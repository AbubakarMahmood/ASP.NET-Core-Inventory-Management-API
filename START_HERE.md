# 🚀 START HERE - Running Your Inventory Management API on Windows

## You Have 3 Ways to Run This Application:

### ⭐ OPTION 1: Docker (Easiest - Recommended!)

**No .NET installation needed. Just Docker Desktop.**

1. **Make sure Docker Desktop is running**

2. **Open PowerShell** in this project folder

3. **Run ONE command:**
   ```powershell
   docker-compose up --build
   ```

4. **Wait 2-3 minutes** for first-time build

5. **Done!** Access:
   - **Blazor UI**: http://localhost:3000
   - **API Swagger**: http://localhost:5000/swagger
   - **Login**: admin@inventory.com / Admin123!

**Or even easier:** Just double-click `quick-start.bat` in Windows Explorer!

---

### OPTION 2: Using the PowerShell Setup Wizard

1. **Open PowerShell as Administrator**

2. **Navigate to project folder:**
   ```powershell
   cd C:\path\to\ASP.NET-Core-Inventory-Management-API-
   ```

3. **Run the setup script:**
   ```powershell
   .\setup-windows.ps1
   ```

4. **Follow the wizard** - it will guide you through everything!

---

### OPTION 3: Manual Setup (.NET SDK)

See **[GETTING_STARTED_WINDOWS.md](GETTING_STARTED_WINDOWS.md)** for detailed manual setup instructions.

---

## 📋 What You Get

After starting the application, you'll have access to:

### Blazor Web UI (http://localhost:3000)
- Modern, responsive interface
- Real-time updates with SignalR
- Advanced filtering and sorting
- Excel/PDF export capabilities

### Features Available:
✅ **Products Management**
  - CRUD operations
  - Advanced filtering
  - Filter presets (save your favorite filters)
  - Keyboard shortcuts: Ctrl+S (save preset), Ctrl+K (clear filters)
  - Multi-column sorting
  - Export to Excel/PDF

✅ **Work Orders**
  - Create and manage work orders
  - Approval workflow (Draft → Submitted → Approved → Completed)
  - Status and priority filtering
  - Real-time notifications

✅ **Stock Movements**
  - Record stock transactions
  - Track receipts, issues, adjustments, transfers
  - View movement history

✅ **User Management** (Admin only)
  - Create/edit users
  - Assign roles (Admin, Manager, Operator)
  - Role-based permissions

✅ **Audit Log Viewer**
  - Track all system changes
  - Filter by user, entity, action
  - Complete audit trail

✅ **Reports**
  - Low Stock Report
  - Pending Orders Report

### API Documentation (http://localhost:5000/swagger)
- Interactive API testing
- Complete endpoint documentation
- JWT authentication support
- Try-it-out functionality

---

## 🔐 Test Credentials

Three pre-configured accounts for testing:

| Role | Email | Password | Permissions |
|------|-------|----------|-------------|
| **Admin** | admin@inventory.com | Admin123! | Full access |
| **Manager** | manager@inventory.com | Manager123! | Approve orders, manage inventory |
| **Operator** | operator@inventory.com | Operator123! | Create orders, record movements |

---

## 📊 Sample Data Included

The application comes pre-loaded with:

- **5 Products**: Laptop, Mouse, USB Cable, Monitor, Keyboard
- **3 Users**: Admin, Manager, Operator
- Sample stock levels and configurations

You can start testing immediately - no need to create data!

---

## ✅ Quick Test Checklist

After starting, verify everything works:

1. [ ] Open http://localhost:3000 - UI loads
2. [ ] Login with admin@inventory.com / Admin123!
3. [ ] Click "Products" - see list of 5 products
4. [ ] Click "Create Product" - form appears
5. [ ] Try filtering products by category
6. [ ] Save a filter preset (Ctrl+S or click Save button)
7. [ ] Open http://localhost:5000/swagger - API docs load
8. [ ] Try "Work Orders" page - see work order management
9. [ ] Check "Users" page (Admin only) - see user list
10. [ ] Check "Audit Log" - see system changes

---

## 🔧 Keyboard Shortcuts

Power user features:

- **Ctrl+S**: Save current filters as a preset
- **Ctrl+K**: Clear all filters
- **Ctrl+N**: Create new item (on list pages)
- Filter chips: Click the X to remove individual filters

---

## 🐛 Having Issues?

### Docker Issues

**Problem:** Port already in use
```powershell
# Stop all containers
docker-compose down

# Or change ports in docker-compose.yml
```

**Problem:** Docker won't start
1. Restart Docker Desktop
2. In Docker settings, enable WSL 2 integration
3. Try again

### .NET SDK Issues

**Problem:** "dotnet command not found"
- Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- Restart PowerShell after installation

### Database Issues

**Problem:** "Cannot connect to database"
- Make sure PostgreSQL container is running: `docker ps`
- Check connection string in `src/InventoryAPI.Api/appsettings.Development.json`

### More Help

See **[IMPORTANT_NOTES.md](IMPORTANT_NOTES.md)** for:
- Detailed troubleshooting
- Common error messages and fixes
- Development tips
- Database connection help

---

## 📚 Documentation Files

| File | Purpose |
|------|---------|
| **START_HERE.md** | This file - quick start guide |
| **GETTING_STARTED_WINDOWS.md** | Complete setup guide with all options |
| **IMPORTANT_NOTES.md** | Troubleshooting and important info |
| **CLAUDE.md** | Architecture deep-dive and best practices |
| **README.md** | Project overview and features |
| **quick-start.bat** | Double-click to start with Docker |
| **setup-windows.ps1** | Interactive setup wizard |

---

## 🎯 Recommended Path for First-Time Users

1. **Start with Docker** (Option 1 above)
2. **Open the Blazor UI** at http://localhost:3000
3. **Login as Admin** (admin@inventory.com / Admin123!)
4. **Explore the Products page** first
5. **Try creating a filter preset** (select some filters, click Save)
6. **Test keyboard shortcuts** (Ctrl+S, Ctrl+K)
7. **Create a Work Order** on the Work Orders page
8. **View the API docs** at http://localhost:5000/swagger
9. **Check the code** - see how Clean Architecture works
10. **Read CLAUDE.md** for architecture details

---

## 🚀 Ready to Start?

### Fastest way (Docker):
```powershell
docker-compose up --build
```

### Or double-click:
**`quick-start.bat`** in Windows Explorer

### Or use the wizard:
```powershell
.\setup-windows.ps1
```

---

## 🎉 That's It!

You should now have a fully functional inventory management system running locally.

**Need more help?** Check out:
- GETTING_STARTED_WINDOWS.md (detailed instructions)
- IMPORTANT_NOTES.md (troubleshooting)
- CLAUDE.md (architecture documentation)

**Questions?** Open an issue on GitHub.

---

**Happy coding! 🚀**

Built with ASP.NET Core 8, Clean Architecture, CQRS, and ❤️
