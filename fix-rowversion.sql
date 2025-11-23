-- SQL Script to add missing RowVersion columns to all tables
-- This fixes the error: "column u.RowVersion does not exist"

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

-- Verify the columns were added
SELECT
    table_name,
    column_name,
    data_type
FROM
    information_schema.columns
WHERE
    table_name IN ('Users', 'Products', 'WorkOrders', 'WorkOrderItems', 'StockMovements', 'FilterPresets')
    AND column_name = 'RowVersion'
ORDER BY
    table_name;
