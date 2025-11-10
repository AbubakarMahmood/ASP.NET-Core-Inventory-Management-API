#!/bin/bash

###############################################################################
# Database Migration Script
# Applies EF Core migrations to the database
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INFRASTRUCTURE_PROJECT="$PROJECT_ROOT/src/InventoryAPI.Infrastructure"
API_PROJECT="$PROJECT_ROOT/src/InventoryAPI.Api"
BACKUP_DIR="$PROJECT_ROOT/backups"

# Ensure backup directory exists
mkdir -p "$BACKUP_DIR"

echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Database Migration Script${NC}"
echo -e "${GREEN}================================${NC}"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: dotnet CLI not found${NC}"
    echo "Please install .NET SDK 8.0 or later"
    exit 1
fi

# Check if dotnet-ef is installed
if ! dotnet ef &> /dev/null; then
    echo -e "${YELLOW}dotnet-ef tool not found. Installing...${NC}"
    dotnet tool install --global dotnet-ef
fi

# Parse arguments
ENVIRONMENT="${1:-Production}"
CONNECTION_STRING="${2:-}"

echo "Environment: $ENVIRONMENT"
echo "Project Root: $PROJECT_ROOT"
echo ""

# List current migrations
echo -e "${YELLOW}=== Current Migration Status ===${NC}"
dotnet ef migrations list \
    --project "$INFRASTRUCTURE_PROJECT" \
    --startup-project "$API_PROJECT" \
    --no-build

echo ""

# Check for pending migrations
echo -e "${YELLOW}=== Checking for pending migrations ===${NC}"
PENDING_COUNT=$(dotnet ef migrations list \
    --project "$INFRASTRUCTURE_PROJECT" \
    --startup-project "$API_PROJECT" \
    --no-build 2>/dev/null | grep -c "(Pending)" || echo "0")

if [ "$PENDING_COUNT" -eq 0 ]; then
    echo -e "${GREEN}✓ No pending migrations. Database is up to date.${NC}"
    exit 0
fi

echo -e "${YELLOW}Found $PENDING_COUNT pending migration(s)${NC}"
echo ""

# Confirm before proceeding in production
if [ "$ENVIRONMENT" == "Production" ]; then
    echo -e "${RED}=== PRODUCTION MIGRATION ===${NC}"
    echo -e "${YELLOW}This will apply migrations to the PRODUCTION database!${NC}"
    echo ""
    read -p "Are you sure you want to continue? (yes/no): " CONFIRM

    if [ "$CONFIRM" != "yes" ]; then
        echo -e "${YELLOW}Migration cancelled.${NC}"
        exit 0
    fi
fi

# Apply migrations
echo ""
echo -e "${YELLOW}=== Applying Migrations ===${NC}"

if [ -n "$CONNECTION_STRING" ]; then
    echo "Using provided connection string"
    dotnet ef database update \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT" \
        --connection "$CONNECTION_STRING"
else
    echo "Using connection string from appsettings.json"
    dotnet ef database update \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT"
fi

# Check result
if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✓ Migrations applied successfully!${NC}"
    echo ""

    # List final migration status
    echo -e "${GREEN}=== Final Migration Status ===${NC}"
    dotnet ef migrations list \
        --project "$INFRASTRUCTURE_PROJECT" \
        --startup-project "$API_PROJECT" \
        --no-build

    exit 0
else
    echo ""
    echo -e "${RED}✗ Migration failed!${NC}"
    echo -e "${YELLOW}Check the error messages above for details.${NC}"
    exit 1
fi
