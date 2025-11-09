@echo off
REM Quick Start Script for Windows (Docker)
REM Double-click this file to start the application with Docker

echo =====================================
echo Inventory Management API - Quick Start
echo =====================================
echo.

echo Checking Docker...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not installed or not running!
    echo Please install Docker Desktop from: https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

echo Docker found!
echo.
echo Starting application with Docker Compose...
echo This may take a few minutes on first run...
echo.

docker-compose up --build

pause
