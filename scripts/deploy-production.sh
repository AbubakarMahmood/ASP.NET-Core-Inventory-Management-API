#!/bin/bash

###############################################################################
# Production Deployment Script
# Complete workflow for deploying to production
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT_DIR="$PROJECT_ROOT/scripts"

echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Production Deployment${NC}"
echo -e "${GREEN}================================${NC}"
echo ""

# Step 1: Confirmation
echo -e "${RED}=== PRODUCTION DEPLOYMENT ===${NC}"
echo -e "${YELLOW}This will deploy to PRODUCTION!${NC}"
echo ""
echo "This script will:"
echo "  1. Create database backup"
echo "  2. Apply pending migrations"
echo "  3. Build application"
echo "  4. Deploy with Docker Compose"
echo "  5. Run health checks"
echo ""
read -p "Are you sure you want to continue? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo -e "${YELLOW}Deployment cancelled.${NC}"
    exit 0
fi

# Step 2: Backup Database
echo ""
echo -e "${BLUE}=== Step 1: Creating Database Backup ===${NC}"
if bash "$SCRIPT_DIR/backup-database.sh" "pre_deployment_$(date +%Y%m%d_%H%M%S)"; then
    echo -e "${GREEN}✓ Backup completed${NC}"
else
    echo -e "${RED}✗ Backup failed${NC}"
    read -p "Continue anyway? (yes/no): " CONTINUE
    if [ "$CONTINUE" != "yes" ]; then
        echo "Deployment aborted"
        exit 1
    fi
fi

# Step 3: Apply Migrations
echo ""
echo -e "${BLUE}=== Step 2: Applying Migrations ===${NC}"
if bash "$SCRIPT_DIR/migrate-database.sh" Production; then
    echo -e "${GREEN}✓ Migrations completed${NC}"
else
    echo -e "${RED}✗ Migrations failed${NC}"
    echo "Deployment aborted"
    exit 1
fi

# Step 4: Build Application
echo ""
echo -e "${BLUE}=== Step 3: Building Application ===${NC}"
cd "$PROJECT_ROOT"

dotnet build --configuration Release

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Build completed${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    echo "Deployment aborted"
    exit 1
fi

# Step 5: Deploy with Docker Compose
echo ""
echo -e "${BLUE}=== Step 4: Deploying with Docker Compose ===${NC}"

# Set production environment
export ASPNETCORE_ENVIRONMENT=Production

# Build and start containers
docker-compose down
docker-compose build --no-cache api
docker-compose up -d

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Docker containers started${NC}"
else
    echo -e "${RED}✗ Docker deployment failed${NC}"
    echo "Deployment aborted"
    exit 1
fi

# Step 6: Wait for startup
echo ""
echo -e "${BLUE}=== Step 5: Waiting for Application Startup ===${NC}"
echo "Waiting 30 seconds for application to start..."
sleep 30

# Step 7: Health Checks
echo ""
echo -e "${BLUE}=== Step 6: Running Health Checks ===${NC}"

export API_URL="http://localhost:5000"

if bash "$SCRIPT_DIR/health-check.sh"; then
    echo -e "${GREEN}✓ Health checks passed${NC}"
else
    echo -e "${RED}✗ Health checks failed${NC}"
    echo ""
    echo -e "${YELLOW}Showing recent logs:${NC}"
    docker-compose logs --tail=50 api
    echo ""
    read -p "Rollback deployment? (yes/no): " ROLLBACK
    if [ "$ROLLBACK" == "yes" ]; then
        echo "Rolling back..."
        # Implement rollback logic here
        docker-compose down
        # Restore previous version
        echo -e "${YELLOW}Manual rollback required${NC}"
    fi
    exit 1
fi

# Step 8: Success
echo ""
echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}✓ Deployment Successful!${NC}"
echo -e "${GREEN}================================${NC}"
echo ""
echo "Next steps:"
echo "  1. Monitor logs: docker-compose logs -f api"
echo "  2. Check metrics and monitoring dashboards"
echo "  3. Verify critical functionality"
echo "  4. Update deployment documentation"
echo ""

exit 0
