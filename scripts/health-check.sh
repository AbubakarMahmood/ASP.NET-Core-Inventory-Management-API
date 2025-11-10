#!/bin/bash

###############################################################################
# Health Check Script
# Verifies the application and database are healthy
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_URL="${API_URL:-http://localhost:5000}"
MAX_RETRIES=3
RETRY_DELAY=2

echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Health Check Script${NC}"
echo -e "${GREEN}================================${NC}"
echo ""
echo "API URL: $API_URL"
echo "Max Retries: $MAX_RETRIES"
echo ""

# Check if curl or wget is available
if command -v curl &> /dev/null; then
    HTTP_CLIENT="curl"
elif command -v wget &> /dev/null; then
    HTTP_CLIENT="wget"
else
    echo -e "${RED}Error: Neither curl nor wget found${NC}"
    echo "Please install curl or wget"
    exit 1
fi

# Function to make HTTP request
make_request() {
    local url=$1
    if [ "$HTTP_CLIENT" == "curl" ]; then
        curl -s -w "\n%{http_code}" "$url"
    else
        wget -q -O - "$url"
    fi
}

# Function to check endpoint
check_endpoint() {
    local endpoint=$1
    local description=$2

    echo -e "${BLUE}Checking $description...${NC}"

    for i in $(seq 1 $MAX_RETRIES); do
        RESPONSE=$(make_request "$API_URL$endpoint" 2>/dev/null || echo "ERROR")

        if [ "$RESPONSE" != "ERROR" ]; then
            HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
            BODY=$(echo "$RESPONSE" | head -n-1)

            if [ "$HTTP_CODE" == "200" ]; then
                echo -e "${GREEN}✓ $description - Healthy${NC}"
                if command -v jq &> /dev/null; then
                    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
                else
                    echo "$BODY"
                fi
                echo ""
                return 0
            elif [ "$HTTP_CODE" == "503" ]; then
                echo -e "${YELLOW}⚠ $description - Degraded (503)${NC}"
                if command -v jq &> /dev/null; then
                    echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"
                else
                    echo "$BODY"
                fi
                echo ""
                return 1
            fi
        fi

        if [ $i -lt $MAX_RETRIES ]; then
            echo -e "${YELLOW}  Attempt $i/$MAX_RETRIES failed. Retrying in ${RETRY_DELAY}s...${NC}"
            sleep $RETRY_DELAY
        fi
    done

    echo -e "${RED}✗ $description - Unhealthy (failed after $MAX_RETRIES attempts)${NC}"
    echo ""
    return 1
}

# Check basic health
HEALTH_OK=0
check_endpoint "/api/v1/health" "Basic Health Check" || HEALTH_OK=1

# Check database status
DB_OK=0
check_endpoint "/api/v1/database/status" "Database Status" || DB_OK=1

# Summary
echo -e "${GREEN}================================${NC}"
echo -e "${GREEN}Summary${NC}"
echo -e "${GREEN}================================${NC}"

if [ $HEALTH_OK -eq 0 ] && [ $DB_OK -eq 0 ]; then
    echo -e "${GREEN}✓ All checks passed - System is healthy${NC}"
    exit 0
elif [ $HEALTH_OK -eq 0 ] || [ $DB_OK -eq 0 ]; then
    echo -e "${YELLOW}⚠ Some checks failed - System is degraded${NC}"
    exit 1
else
    echo -e "${RED}✗ All checks failed - System is unhealthy${NC}"
    exit 2
fi
