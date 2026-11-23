@echo off
setlocal
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Development
dotnet restore || exit /b 1
dotnet build --no-restore || exit /b 1
dotnet run --no-build --launch-profile http
endlocal
