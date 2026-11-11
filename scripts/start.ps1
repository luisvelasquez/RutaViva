$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)
if (!(Test-Path .env)) {
  Copy-Item .env.example .env
  Write-Host "Se creó .env. Cambia SQL_PASSWORD antes de continuar."
  exit 0
}
docker compose up --build -d
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Interfaz: http://localhost:8080 | API: http://localhost:5000"
