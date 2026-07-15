---
title: Inventory Management API
emoji: 📦
colorFrom: blue
colorTo: indigo
sdk: docker
app_port: 7860
pinned: false
---

# Inventory Management API — live demo

ASP.NET Core 8 REST API for inventory and work order management, running
against a PostgreSQL instance inside this Space. The landing page is the
interactive Swagger documentation.

Log in via `POST /api/v1/auth/login` with one of the demo accounts:

| Role | Email | Password |
|---|---|---|
| Admin | admin@inventory.com | Admin123! |
| Manager | manager@inventory.com | Manager123! |
| Operator | operator@inventory.com | Operator123! |

Storage is ephemeral: data resets to the seeded demo set when the Space
restarts.

Source: https://github.com/AbubakarMahmood/ASP.NET-Core-Inventory-Management-API
