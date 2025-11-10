#!/bin/bash

echo "========================================"
echo "Database Reset and Migration Script"
echo "========================================"
echo ""

# Stop containers
echo "1. Stopping Docker containers..."
docker-compose down -v

# Remove migration files if they exist
echo ""
echo "2. Cleaning old migrations..."
rm -rf src/InventoryAPI.Infrastructure/Data/Migrations/*.cs

# Create new migration
echo ""
echo "3. Creating new migration with proper entity configurations..."
dotnet ef migrations add InitialCreate \
    --project src/InventoryAPI.Infrastructure \
    --startup-project src/InventoryAPI.Api \
    --output-dir Data/Migrations

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to create migration"
    exit 1
fi

# Start containers
echo ""
echo "4. Starting Docker containers (this will auto-apply migrations)..."
docker-compose up --build -d

echo ""
echo "5. Waiting for API to be ready..."
sleep 10

# Check API health
echo ""
echo "6. Checking API health..."
curl -f http://localhost:5000/api/health || echo "Warning: API might not be ready yet"

echo ""
echo "========================================"
echo "✅ Database reset complete!"
echo "========================================"
echo ""
echo "API is available at: http://localhost:5000"
echo "Swagger UI: http://localhost:5000/swagger"
echo "Frontend: http://localhost:3000"
echo ""
echo "View API logs: docker-compose logs -f api"
