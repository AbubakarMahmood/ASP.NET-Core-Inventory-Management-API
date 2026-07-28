using InventoryAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Infrastructure.Data.Migrations;

/// <summary>
/// Converts the original mutable stock field into an evidence-backed cached
/// balance, adds retry-safe operation identities and historical snapshots,
/// revokes plaintext refresh tokens, and installs PostgreSQL integrity guards.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260726090000_EnforceLedgerInvariants")]
public partial class EnforceLedgerInvariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Duplicate product lines would make later issue semantics ambiguous.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "WorkOrderItems"
                    GROUP BY "WorkOrderId", "ProductId"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Duplicate products exist on a work order; reconcile them before applying migration 20260726090000.';
                END IF;
            END
            $$;
            """);

        migrationBuilder.DropColumn(
            name: "CostingMethod",
            table: "Products");

        migrationBuilder.RenameColumn(
            name: "RefreshToken",
            table: "Users",
            newName: "RefreshTokenHash");

        // Existing values are live bearer credentials, not hashes. Revoke them
        // instead of relabelling plaintext as protected state.
        migrationBuilder.Sql(
            "UPDATE \"Users\" SET \"RefreshTokenHash\" = NULL, \"RefreshTokenExpiryTime\" = NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "RefreshTokenHash",
            table: "Users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OperationId",
            table: "StockMovements",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "BalanceBefore",
            table: "StockMovements",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "BalanceAfter",
            table: "StockMovements",
            type: "integer",
            nullable: true);

        // Reconcile cached balances against the legacy ledger. Positive drift is
        // placed before existing history so it represents an opening balance;
        // negative drift is placed after history as a closing correction.
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                actor_id uuid;
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "Products" p
                    LEFT JOIN (
                        SELECT "ProductId",
                               COALESCE(SUM(CASE
                                   WHEN "Type" IN (1, 5) THEN "Quantity"
                                   WHEN "Type" = 2 THEN -"Quantity"
                                   WHEN "Type" = 3 THEN "Quantity"
                                   ELSE 0
                               END), 0) AS ledger_balance
                        FROM "StockMovements"
                        GROUP BY "ProductId"
                    ) ledger ON ledger."ProductId" = p."Id"
                    WHERE p."CurrentStock" <> COALESCE(ledger.ledger_balance, 0)
                ) THEN
                    SELECT "Id" INTO actor_id
                    FROM "Users"
                    ORDER BY CASE WHEN "Role" = 3 THEN 0 ELSE 1 END, "CreatedAt", "Id"
                    LIMIT 1;

                    IF actor_id IS NULL THEN
                        RAISE EXCEPTION 'Stock balance drift exists but no user is available to attribute reconciliation movements.';
                    END IF;

                    INSERT INTO "StockMovements" (
                        "Id", "OperationId", "ProductId", "Type", "Quantity",
                        "BalanceBefore", "BalanceAfter", "SourceLocation",
                        "DestinationLocation", "Reason", "Reference", "WorkOrderId",
                        "PerformedById", "Timestamp", "UnitCostAtTransaction")
                    SELECT
                        md5('reconciliation-row:' || p."Id"::text)::uuid,
                        md5('reconciliation-operation:' || p."Id"::text)::uuid,
                        p."Id",
                        3,
                        p."CurrentStock" - COALESCE(ledger.ledger_balance, 0),
                        NULL,
                        NULL,
                        p."Location",
                        p."Location",
                        'Legacy balance reconciliation created by migration 20260726090000',
                        'MIGRATION:20260726090000',
                        NULL,
                        actor_id,
                        CASE
                            WHEN p."CurrentStock" - COALESCE(ledger.ledger_balance, 0) > 0
                                THEN COALESCE(history.first_timestamp, NOW()) - INTERVAL '1 microsecond'
                            ELSE COALESCE(history.last_timestamp, NOW()) + INTERVAL '1 microsecond'
                        END,
                        p."UnitCost"
                    FROM "Products" p
                    LEFT JOIN (
                        SELECT "ProductId",
                               COALESCE(SUM(CASE
                                   WHEN "Type" IN (1, 5) THEN "Quantity"
                                   WHEN "Type" = 2 THEN -"Quantity"
                                   WHEN "Type" = 3 THEN "Quantity"
                                   ELSE 0
                               END), 0) AS ledger_balance
                        FROM "StockMovements"
                        GROUP BY "ProductId"
                    ) ledger ON ledger."ProductId" = p."Id"
                    LEFT JOIN (
                        SELECT "ProductId", MIN("Timestamp") AS first_timestamp, MAX("Timestamp") AS last_timestamp
                        FROM "StockMovements"
                        GROUP BY "ProductId"
                    ) history ON history."ProductId" = p."Id"
                    WHERE p."CurrentStock" <> COALESCE(ledger.ledger_balance, 0);
                END IF;
            END
            $$;
            """);

        // Historical entries predate caller-generated operation ids. Synthetic
        // values are deterministic and deliberately distinguish migrated data.
        migrationBuilder.Sql(
            "UPDATE \"StockMovements\" SET \"OperationId\" = md5('legacy-operation:' || \"Id\"::text)::uuid WHERE \"OperationId\" IS NULL;");

        // Derive immutable before/after snapshots from chronological deltas.
        migrationBuilder.Sql(
            """
            WITH deltas AS (
                SELECT sm."Id",
                       sm."ProductId",
                       sm."Timestamp",
                       CASE
                           WHEN sm."Type" IN (1, 5, 6) THEN sm."Quantity"
                           WHEN sm."Type" = 2 THEN -sm."Quantity"
                           WHEN sm."Type" = 3 THEN sm."Quantity"
                           ELSE 0
                       END AS delta
                FROM "StockMovements" sm
            ), running AS (
                SELECT d."Id",
                       d.delta,
                       COALESCE(
                           SUM(d.delta) OVER (
                               PARTITION BY d."ProductId"
                               ORDER BY d."Timestamp", d."Id"
                               ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING),
                           0) AS balance_before
                FROM deltas d
            )
            UPDATE "StockMovements" sm
               SET "BalanceBefore" = running.balance_before,
                   "BalanceAfter" = running.balance_before + running.delta
              FROM running
             WHERE running."Id" = sm."Id";
            """);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM "StockMovements"
                    WHERE "BalanceBefore" < 0 OR "BalanceAfter" < 0
                ) THEN
                    RAISE EXCEPTION 'Legacy stock history produces a negative intermediate balance; reconcile movement ordering before applying migration 20260726090000.';
                END IF;
            END
            $$;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "OperationId",
            table: "StockMovements",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "BalanceBefore",
            table: "StockMovements",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "BalanceAfter",
            table: "StockMovements",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Products_CurrentStock_NonNegative",
            table: "Products",
            sql: "\"CurrentStock\" >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Products_ReorderPoint_NonNegative",
            table: "Products",
            sql: "\"ReorderPoint\" >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Products_ReorderQuantity_Positive",
            table: "Products",
            sql: "\"ReorderQuantity\" > 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Products_UnitCost_NonNegative",
            table: "Products",
            sql: "\"UnitCost\" >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Quantity_NonZero",
            table: "StockMovements",
            sql: "\"Quantity\" <> 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Type_Range",
            table: "StockMovements",
            sql: "\"Type\" BETWEEN 1 AND 6");
        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Balances_NonNegative",
            table: "StockMovements",
            sql: "\"BalanceBefore\" >= 0 AND \"BalanceAfter\" >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_StockMovements_Balance_Delta",
            table: "StockMovements",
            sql: "((\"Type\" IN (1, 5, 6) AND \"Quantity\" > 0 AND \"BalanceAfter\" = \"BalanceBefore\" + \"Quantity\") " +
                 "OR (\"Type\" = 2 AND \"Quantity\" > 0 AND \"BalanceAfter\" = \"BalanceBefore\" - \"Quantity\") " +
                 "OR (\"Type\" = 3 AND \"BalanceAfter\" = \"BalanceBefore\" + \"Quantity\") " +
                 "OR (\"Type\" = 4 AND \"BalanceAfter\" = \"BalanceBefore\"))");

        migrationBuilder.AddCheckConstraint(
            name: "CK_WorkOrderItems_QuantityRequested_Positive",
            table: "WorkOrderItems",
            sql: "\"QuantityRequested\" > 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_WorkOrderItems_QuantityIssued_Range",
            table: "WorkOrderItems",
            sql: "\"QuantityIssued\" >= 0 AND \"QuantityIssued\" <= \"QuantityRequested\"");

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_OperationId_ProductId",
            table: "StockMovements",
            columns: new[] { "OperationId", "ProductId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_ProductId_Timestamp",
            table: "StockMovements",
            columns: new[] { "ProductId", "Timestamp" });
        migrationBuilder.CreateIndex(
            name: "IX_Users_RefreshTokenHash",
            table: "Users",
            column: "RefreshTokenHash",
            unique: true,
            filter: "\"RefreshTokenHash\" IS NOT NULL");

        migrationBuilder.DropIndex(
            name: "IX_WorkOrderItems_WorkOrderId",
            table: "WorkOrderItems");
        migrationBuilder.CreateIndex(
            name: "IX_WorkOrderItems_WorkOrderId_ProductId",
            table: "WorkOrderItems",
            columns: new[] { "WorkOrderId", "ProductId" },
            unique: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION stockverity_reject_ledger_mutation()
            RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'StockMovements is append-only; % is not permitted', TG_OP
                    USING ERRCODE = '55000';
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER stockverity_stock_movements_append_only
            BEFORE UPDATE OR DELETE ON "StockMovements"
            FOR EACH ROW EXECUTE FUNCTION stockverity_reject_ledger_mutation();
            """);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION stockverity_verify_product_balance()
            RETURNS trigger AS $$
            DECLARE
                target_product_id uuid;
                cached_balance bigint;
                ledger_balance bigint;
            BEGIN
                IF TG_TABLE_NAME = 'Products' THEN
                    target_product_id := NEW."Id";
                ELSE
                    target_product_id := NEW."ProductId";
                END IF;

                SELECT "CurrentStock" INTO cached_balance
                FROM "Products"
                WHERE "Id" = target_product_id;

                IF cached_balance IS NULL THEN
                    RETURN NULL;
                END IF;

                SELECT COALESCE(SUM(CASE
                    WHEN "Type" IN (1, 5, 6) THEN "Quantity"
                    WHEN "Type" = 2 THEN -"Quantity"
                    WHEN "Type" = 3 THEN "Quantity"
                    ELSE 0
                END), 0)
                INTO ledger_balance
                FROM "StockMovements"
                WHERE "ProductId" = target_product_id;

                IF cached_balance <> ledger_balance THEN
                    RAISE EXCEPTION 'Product % cached balance % does not match ledger balance %',
                        target_product_id, cached_balance, ledger_balance
                        USING ERRCODE = '23514';
                END IF;

                RETURN NULL;
            END;
            $$ LANGUAGE plpgsql;

            CREATE CONSTRAINT TRIGGER stockverity_products_balance_matches_ledger
            AFTER INSERT OR UPDATE OF "CurrentStock" ON "Products"
            DEFERRABLE INITIALLY DEFERRED
            FOR EACH ROW EXECUTE FUNCTION stockverity_verify_product_balance();

            CREATE CONSTRAINT TRIGGER stockverity_movements_balance_matches_product
            AFTER INSERT ON "StockMovements"
            DEFERRABLE INITIALLY DEFERRED
            FOR EACH ROW EXECUTE FUNCTION stockverity_verify_product_balance();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS stockverity_movements_balance_matches_product ON \"StockMovements\";");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS stockverity_products_balance_matches_ledger ON \"Products\";");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS stockverity_verify_product_balance();");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS stockverity_stock_movements_append_only ON \"StockMovements\";");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS stockverity_reject_ledger_mutation();");

        migrationBuilder.DropIndex(name: "IX_WorkOrderItems_WorkOrderId_ProductId", table: "WorkOrderItems");
        migrationBuilder.CreateIndex(name: "IX_WorkOrderItems_WorkOrderId", table: "WorkOrderItems", column: "WorkOrderId");
        migrationBuilder.DropIndex(name: "IX_Users_RefreshTokenHash", table: "Users");
        migrationBuilder.DropIndex(name: "IX_StockMovements_ProductId_Timestamp", table: "StockMovements");
        migrationBuilder.DropIndex(name: "IX_StockMovements_OperationId_ProductId", table: "StockMovements");

        migrationBuilder.DropCheckConstraint(name: "CK_WorkOrderItems_QuantityIssued_Range", table: "WorkOrderItems");
        migrationBuilder.DropCheckConstraint(name: "CK_WorkOrderItems_QuantityRequested_Positive", table: "WorkOrderItems");
        migrationBuilder.DropCheckConstraint(name: "CK_StockMovements_Balance_Delta", table: "StockMovements");
        migrationBuilder.DropCheckConstraint(name: "CK_StockMovements_Balances_NonNegative", table: "StockMovements");
        migrationBuilder.DropCheckConstraint(name: "CK_StockMovements_Type_Range", table: "StockMovements");
        migrationBuilder.DropCheckConstraint(name: "CK_StockMovements_Quantity_NonZero", table: "StockMovements");
        migrationBuilder.DropCheckConstraint(name: "CK_Products_UnitCost_NonNegative", table: "Products");
        migrationBuilder.DropCheckConstraint(name: "CK_Products_ReorderQuantity_Positive", table: "Products");
        migrationBuilder.DropCheckConstraint(name: "CK_Products_ReorderPoint_NonNegative", table: "Products");
        migrationBuilder.DropCheckConstraint(name: "CK_Products_CurrentStock_NonNegative", table: "Products");

        migrationBuilder.DropColumn(name: "BalanceAfter", table: "StockMovements");
        migrationBuilder.DropColumn(name: "BalanceBefore", table: "StockMovements");
        migrationBuilder.DropColumn(name: "OperationId", table: "StockMovements");

        migrationBuilder.AddColumn<int>(
            name: "CostingMethod",
            table: "Products",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "RefreshTokenHash",
            table: "Users",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64,
            oldNullable: true);
        migrationBuilder.RenameColumn(
            name: "RefreshTokenHash",
            table: "Users",
            newName: "RefreshToken");
    }
}
