# Deployment Scripts

This directory contains automated scripts for managing database and application deployment.

## Available Scripts

### 🔄 `migrate-database.sh`

Applies EF Core migrations to the database.

**Usage:**
```bash
# Development (with confirmation prompt)
./scripts/migrate-database.sh Development

# Production (with confirmation and safety checks)
./scripts/migrate-database.sh Production

# With custom connection string
./scripts/migrate-database.sh Production "Host=db.example.com;Port=5432;Database=inventorydb;Username=user;Password=pass"
```

**What it does:**
- Lists current migration status
- Checks for pending migrations
- Prompts for confirmation (in Production)
- Applies all pending migrations
- Shows final migration status

---

### 💾 `backup-database.sh`

Creates a compressed backup of the PostgreSQL database.

**Usage:**
```bash
# Create backup with automatic timestamp
./scripts/backup-database.sh

# Create backup with custom name
./scripts/backup-database.sh "before_major_update"

# With environment variables
DB_HOST=prod-db.example.com \
DB_PORT=5432 \
DB_NAME=inventorydb \
DB_USER=inventoryuser \
./scripts/backup-database.sh
```

**What it does:**
- Creates PostgreSQL dump
- Compresses with gzip
- Stores in `backups/` directory
- Automatically cleans up old backups (keeps last 10)

---

### ✅ `health-check.sh`

Verifies application and database health.

**Usage:**
```bash
# Check local instance
./scripts/health-check.sh

# Check specific API URL
API_URL=https://api.example.com ./scripts/health-check.sh

# With custom retry settings
API_URL=https://api.example.com \
MAX_RETRIES=5 \
RETRY_DELAY=3 \
./scripts/health-check.sh
```

**What it does:**
- Checks `/api/v1/health` endpoint
- Checks `/api/v1/database/status` endpoint
- Retries on failure (configurable)
- Displays JSON responses (pretty-printed if jq available)
- Returns exit codes: 0 (healthy), 1 (degraded), 2 (unhealthy)

**Exit Codes:**
- `0` - All checks passed (healthy)
- `1` - Some checks passed (degraded)
- `2` - All checks failed (unhealthy)

---

### 🚀 `deploy-production.sh`

Complete production deployment workflow.

**Usage:**
```bash
# Full production deployment
./scripts/deploy-production.sh
```

**What it does:**
1. Prompts for confirmation
2. Creates database backup
3. Applies pending migrations
4. Builds application (Release mode)
5. Deploys with Docker Compose
6. Waits for startup
7. Runs health checks
8. Offers rollback on failure

⚠️ **Warning:** This script is destructive. Review before running!

---

## Environment Variables

### Database Connection

```bash
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME=inventorydb
export DB_USER=inventoryuser
export PGPASSWORD=your_password  # For backup script
```

### Application

```bash
export API_URL=http://localhost:5000
export ASPNETCORE_ENVIRONMENT=Production
```

### Connection String (for migrations)

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=inventorydb;Username=inventoryuser;Password=YourPassword"
```

---

## Examples

### Pre-Deployment Checklist

```bash
# 1. Backup database
./scripts/backup-database.sh "pre_deployment_v1.2.0"

# 2. Review pending migrations
dotnet ef migrations list \
  --project src/InventoryAPI.Infrastructure \
  --startup-project src/InventoryAPI.Api

# 3. Apply migrations
./scripts/migrate-database.sh Production

# 4. Deploy application
docker-compose up -d --build api

# 5. Verify health
./scripts/health-check.sh
```

### Emergency Rollback

```bash
# 1. Stop application
docker-compose down

# 2. Restore database from backup
gunzip backups/pre_deployment_v1.2.0.sql.gz
psql -U inventoryuser -d inventorydb -f backups/pre_deployment_v1.2.0.sql

# 3. Deploy previous version
git checkout v1.1.0
docker-compose up -d --build api

# 4. Verify
./scripts/health-check.sh
```

### Continuous Integration

```yaml
# .github/workflows/deploy.yml
jobs:
  deploy:
    steps:
      - name: Backup Database
        run: ./scripts/backup-database.sh

      - name: Migrate Database
        run: ./scripts/migrate-database.sh Production

      - name: Deploy
        run: docker-compose up -d --build api

      - name: Health Check
        run: ./scripts/health-check.sh
```

---

## Requirements

### System Dependencies

- **bash** - Shell interpreter
- **curl** or **wget** - HTTP client (health-check)
- **dotnet** - .NET SDK 8.0+ (migration scripts)
- **dotnet-ef** - EF Core CLI tools (migration scripts)
- **pg_dump** / **psql** - PostgreSQL client tools (backup/restore)
- **docker** and **docker-compose** - Container management (deploy script)
- **gzip** - Compression (backup script)
- **jq** (optional) - JSON formatting (health-check)

### Install Requirements

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y curl postgresql-client gzip jq

# macOS
brew install curl postgresql gzip jq

# .NET tools
dotnet tool install --global dotnet-ef
```

---

## Troubleshooting

### "Permission denied"

```bash
# Make scripts executable
chmod +x scripts/*.sh
```

### "dotnet: command not found"

```bash
# Install .NET SDK
# https://dotnet.microsoft.com/download

# Add to PATH
export PATH="$PATH:$HOME/.dotnet/tools"
```

### "pg_dump: command not found"

```bash
# Install PostgreSQL client tools
sudo apt-get install postgresql-client  # Ubuntu
brew install postgresql                  # macOS
```

### Health check fails with "connection refused"

```bash
# Check if application is running
docker-compose ps

# Check logs
docker-compose logs api

# Verify API URL
echo $API_URL

# Test connection manually
curl http://localhost:5000/api/v1/health
```

---

## Best Practices

1. **Always backup before migrations**
   ```bash
   ./scripts/backup-database.sh
   ./scripts/migrate-database.sh Production
   ```

2. **Test scripts in staging first**
   ```bash
   ASPNETCORE_ENVIRONMENT=Staging ./scripts/deploy-production.sh
   ```

3. **Use version-specific backup names**
   ```bash
   ./scripts/backup-database.sh "v1.2.0_$(date +%Y%m%d)"
   ```

4. **Monitor health checks after deployment**
   ```bash
   watch -n 5 './scripts/health-check.sh'
   ```

5. **Keep backups for rollback**
   - Don't delete backups immediately after deployment
   - Keep at least 30 days of backups
   - Store backups off-server for disaster recovery

---

## Security Considerations

### Sensitive Data

- **Never commit passwords** to version control
- Use environment variables for credentials
- Consider using `.pgpass` file for PostgreSQL authentication
- Use secret management tools in production (Vault, AWS Secrets Manager, Azure Key Vault)

### File Permissions

```bash
# Secure backup directory
chmod 700 backups/
chmod 600 backups/*.sql.gz

# Secure scripts (executable by owner only)
chmod 700 scripts/*.sh
```

### Audit Logging

```bash
# Log all script executions
./scripts/migrate-database.sh 2>&1 | tee -a deployment_$(date +%Y%m%d_%H%M%S).log
```

---

## Support

For issues or questions:
- See main documentation in `/docs`
- Check troubleshooting guides
- Review script source code (well-commented)
- Open GitHub issue

---

**Last Updated**: 2025-01-10
**Version**: 1.0.0
