#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [ ! -f .env ]; then
  cp .env.example .env
  echo 'Se creó .env. Cambia SQL_PASSWORD antes de continuar.'
  exit 0
fi
docker compose up --build -d
echo 'Interfaz: http://localhost:8080 | API: http://localhost:5000'
