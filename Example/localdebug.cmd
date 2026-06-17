@echo off
setlocal

set "ROOT=%~dp0"

start "Minded Backend" cmd /k "cd /d "%ROOT%" && dotnet watch run --project "MindedExample.Api\MindedExample.Api.csproj""
start "Minded Frontend" cmd /k "cd /d "%ROOT%Frontend" && npm run dev"

echo Started backend and frontend in debug mode.
echo Backend: dotnet watch run --project MindedExample.Api\MindedExample.Api.csproj
echo Frontend: npm run dev (in Frontend)
