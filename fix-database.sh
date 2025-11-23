#!/bin/bash

###############################################################################
# Emergency Database Fix Script
# Fixes the missing RowVersion column issue
###############################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}============================================${NC}"
echo -e "${BLUE}Emergency Database Fix Script${NC}"
echo -e "${BLUE}Fixing missing RowVersion columns${NC}"
echo -e "${BLUE}============================================${NC}"
echo ""

# Database connection details from docker-compose.yml
DB_HOST="localhost"
DB_PORT="5433"
DB_NAME="inventorydb"
DB_USER="inventoryuser"
DB_PASS="InventoryPass123!"

# Check if docker is available
if command -v docker &> /dev/null; then
    echo -e "${YELLOW}Option 1: Applying fix via Docker...${NC}"

    # Execute SQL directly in the PostgreSQL container
    docker exec -i inventory-postgres psql -U "$DB_USER" -d "$DB_NAME" <<'EOSQL'
-- Add RowVersion to Users table
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS "RowVersion" bytea NOT NULL DEFAULT E'\\x';

-- Add RowVersion to Products table
ALTER TABLE "Products"
ADD COLUMN IF NOT EXISTS "RowVersion" bytea NOT NULL DEFAULT E'\\x';

-- Add RowVersion to WorkOrders table
ALTER TABLE "WorkOrders"
ADD COLUMN IF NOT EXISTS "RowVersion" bytea NOT NULL DEFAULT E'\\x';

-- Add RowVersion to WorkOrderItems table
ALTER TABLE "WorkOrderItems"
ADD COLUMN IF NOT EXISTS "RowVersion" bytea NOT NULL DEFAULT E'\\x';

-- Add RowVersion to StockMovements table
ALTER TABLE "StockMovements"
ADD COLUMN IF NOT EXISTS "RowVersion" bytea NOT NULL DEFAULT E'\\x';

-- Add RowVersion to FilterPresets table
ALTER TABLE "FilterPresets"
ADD COLUMN IF NOT EXISTS "RowVersion" bytea NOT NULL DEFAULT E'\\x';

-- Add migration history entry
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251123000000_AddRowVersionToAllEntities', '8.0.0')
ON CONFLICT DO NOTHING;

-- Verify
SELECT table_name, column_name FROM information_schema.columns
WHERE table_name IN ('Users', 'Products', 'WorkOrders', 'WorkOrderItems', 'StockMovements', 'FilterPresets')
AND column_name = 'RowVersion'
ORDER BY table_name;
EOSQL

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Database fix applied successfully via Docker!${NC}"
    else
        echo -e "${RED}✗ Failed to apply fix via Docker${NC}"
        exit 1
    fi

# Check if psql is available
elif command -v psql &> /dev/null; then
    echo -e "${YELLOW}Option 2: Applying fix via psql...${NC}"

    PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f fix-rowversion.sql

    # Also add migration history entry
    PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" <<EOSQL
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251123000000_AddRowVersionToAllEntities', '8.0.0')
ON CONFLICT DO NOTHING;
EOSQL

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Database fix applied successfully via psql!${NC}"
    else
        echo -e "${RED}✗ Failed to apply fix via psql${NC}"
        exit 1
    fi

else
    echo -e "${RED}✗ Neither docker nor psql is available${NC}"
    echo -e "${YELLOW}Please install docker or postgresql-client to apply the fix${NC}"
    echo -e "${YELLOW}Or run the SQL script manually: fix-rowversion.sql${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}Database Fix Complete!${NC}"
echo -e "${GREEN}============================================${NC}"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "1. Restart the API container: ${YELLOW}docker-compose restart api${NC}"
echo "2. Check the API logs: ${YELLOW}docker-compose logs -f api${NC}"
echo "3. Test the login endpoint"
echo ""
