#!/bin/bash

###############################################################################
# Database Backup Script
# Creates a PostgreSQL database backup
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKUP_DIR="$PROJECT_ROOT/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Default database connection details
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-inventorydb}"
DB_USER="${DB_USER:-inventoryuser}"

# Parse arguments
BACKUP_NAME="${1:-backup_$TIMESTAMP}"

# Ensure backup directory exists
mkdir -p "$BACKUP_DIR"

BACKUP_FILE="$BACKUP_DIR/${BACKUP_NAME}.sql"

echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Database Backup Script${NC}"
echo -e "${GREEN}================================${NC}"
echo ""
echo "Database: $DB_NAME"
echo "Host: $DB_HOST:$DB_PORT"
echo "User: $DB_USER"
echo "Backup File: $BACKUP_FILE"
echo ""

# Check if pg_dump is installed
if ! command -v pg_dump &> /dev/null; then
    echo -e "${RED}Error: pg_dump not found${NC}"
    echo "Please install PostgreSQL client tools"
    exit 1
fi

# Create backup
echo -e "${YELLOW}Creating backup...${NC}"

if pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" > "$BACKUP_FILE"; then
    BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    echo ""
    echo -e "${GREEN}✓ Backup created successfully!${NC}"
    echo "  File: $BACKUP_FILE"
    echo "  Size: $BACKUP_SIZE"
    echo ""

    # Compress backup
    echo -e "${YELLOW}Compressing backup...${NC}"
    gzip "$BACKUP_FILE"
    COMPRESSED_SIZE=$(du -h "${BACKUP_FILE}.gz" | cut -f1)
    echo -e "${GREEN}✓ Backup compressed${NC}"
    echo "  File: ${BACKUP_FILE}.gz"
    echo "  Size: $COMPRESSED_SIZE"
    echo ""

    # Cleanup old backups (keep last 10)
    echo -e "${YELLOW}Cleaning up old backups (keeping last 10)...${NC}"
    ls -t "$BACKUP_DIR"/*.sql.gz 2>/dev/null | tail -n +11 | xargs rm -f 2>/dev/null || true

    BACKUP_COUNT=$(ls -1 "$BACKUP_DIR"/*.sql.gz 2>/dev/null | wc -l)
    echo -e "${GREEN}✓ Cleanup complete. Total backups: $BACKUP_COUNT${NC}"

    exit 0
else
    echo ""
    echo -e "${RED}✗ Backup failed!${NC}"
    echo -e "${YELLOW}Check your database connection and credentials.${NC}"
    rm -f "$BACKUP_FILE"
    exit 1
fi
