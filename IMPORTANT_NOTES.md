# Important Notes Before Running

## 🚨 Database Migrations

**IMPORTANT:** This project does not have database migrations created yet. They will be created automatically when you first run the application.

### What happens on first run:

1. **Using Docker (Option 1):**
   - Migrations will be automatically created and applied when the API container starts
   - Database will be seeded with default users and sample data
   - Everything happens automatically ✅

2. **Using .NET SDK directly (Option 2 or 3):**
   - You need to create migrations manually first
   - Run these commands before starting the API:

   ```powershell
   cd src\InventoryAPI.Infrastructure
   dotnet ef migrations add InitialCreate -s ..\InventoryAPI.Api
   dotnet ef database update -s ..\InventoryAPI.Api
   ```

## 📋 Recommended Approach for Windows

**For first-time users:** Use Docker (it's the easiest!)

1. Make sure Docker Desktop is running
2. Open PowerShell in the project directory
3. Run: `docker-compose up --build`
4. Wait 2-3 minutes for everything to start
5. Open browser to http://localhost:3000

That's it! No manual database setup, no migrations to run, no configuration needed.

## 🔐 Default Credentials

After the application starts, login with:

- **Email:** admin@inventory.com
- **Password:** Admin123!

Other test accounts:
- Manager: manager@inventory.com / Manager123!
- Operator: operator@inventory.com / Operator123!

## 🎯 Quick Test Checklist

After starting the application, verify everything works:

- [ ] Blazor UI loads at http://localhost:3000
- [ ] Can login with admin credentials
- [ ] Can see products in Products page
- [ ] API Swagger works at http://localhost:5000/swagger
- [ ] Can create a new product
- [ ] Can view work orders
- [ ] Filter presets work (save/load filters)
- [ ] Keyboard shortcuts work (Ctrl+S, Ctrl+K)

## 📊 What's Already Configured

The application comes with:

✅ **5 Sample Products**
- Laptop Computer
- Wireless Mouse
- USB Cable
- Monitor 24"
- Keyboard Mechanical

✅ **3 User Accounts** (Admin, Manager, Operator)

✅ **Complete Features**
- Advanced filtering on Products and Work Orders
- Filter presets (save your favorite filters)
- Filter chips (visual display of active filters)
- Keyboard shortcuts (Ctrl+S, Ctrl+K)
- Multi-column sorting
- Excel and PDF export
- Real-time notifications (SignalR)
- User management
- Audit log tracking
- Stock movement tracking
- Work order workflow

## 🐛 Common First-Run Issues

### Issue: "Port 5433 already in use"

**Note:** The docker-compose.yml is pre-configured to use port 5433 on the host (instead of 5432) to avoid conflicts with locally-installed PostgreSQL.

**If you still have a conflict:**
```powershell
# Find what's using port 5433
netstat -ano | findstr :5433

# Kill the process (replace PID)
taskkill /PID <PID> /F

# Or change port in docker-compose.yml
# Edit line 10: "5434:5432" instead of "5433:5432"
```

### Issue: "Docker containers won't start"

**Solution:**
1. Restart Docker Desktop
2. Run: `docker-compose down -v`
3. Run: `docker-compose up --build`

### Issue: "Cannot connect to API from Blazor UI"

**Cause:** API is not running or wrong URL

**Solution:**
1. Make sure API is running (check http://localhost:5000/swagger)
2. Check `src/InventoryAPI.BlazorUI/wwwroot/appsettings.json`
3. ApiBaseUrl should be: "http://localhost:5000"

### Issue: "Migrations not found" error

**Solution:**
```powershell
cd src\InventoryAPI.Infrastructure
dotnet ef migrations add InitialCreate -s ..\InventoryAPI.Api --force
dotnet ef database update -s ..\InventoryAPI.Api
```

## 🔧 Development Tips

### Hot Reload for Development

Instead of `dotnet run`, use:
```powershell
dotnet watch run
```

This automatically reloads when you change code!

### Debugging in Visual Studio

1. Open `InventoryAPI.sln` in Visual Studio 2022
2. Right-click solution → "Set Startup Projects"
3. Select "Multiple startup projects"
4. Set both API and BlazorUI to "Start"
5. Press F5

Now both API and UI will start with debugging!

### Viewing Database Data

**With Docker:**
```powershell
docker exec -it inventory-postgres psql -U inventoryuser -d inventorydb

# Then run SQL:
SELECT * FROM "Products";
SELECT * FROM "Users";
SELECT * FROM "WorkOrders";
```

**With pgAdmin:**
1. Open pgAdmin
2. Connect to localhost:5433 (note: 5433, not 5432!)
3. Database: inventorydb
4. Username: inventoryuser
5. Password: InventoryPass123!

## 📚 Additional Resources

- **Full Documentation:** See `CLAUDE.md` for detailed architecture
- **API Documentation:** http://localhost:5000/swagger when running
- **Getting Started Guide:** See `GETTING_STARTED_WINDOWS.md`

## 🚀 Ready to Start?

Choose your method:

**Fastest (Docker):**
```powershell
docker-compose up --build
```

**Direct (.NET SDK):**
```powershell
# Run setup script
.\setup-windows.ps1
```

**Or double-click:**
- `quick-start.bat` - Starts with Docker

---

**Happy coding! 🎉**
