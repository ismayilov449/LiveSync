#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "Starting Docker (SQL Server + Redis)..."
docker compose up -d

echo "Building React client..."
cd "$ROOT/LiveSync.API/client"
if [ ! -d node_modules ]; then
  npm ci
fi
npm run build
cd "$ROOT"

export ASPNETCORE_ENVIRONMENT=Development

echo "Starting API (http://localhost:5252)..."
dotnet run --project LiveSync.API &
API_PID=$!

sleep 2

echo "Starting Worker (metrics http://localhost:5260)..."
dotnet run --project LiveSync.Worker &
WORKER_PID=$!

echo ""
echo "LiveSync dev stack started (API PID $API_PID, Worker PID $WORKER_PID)."
echo "  App:     http://localhost:5252"
echo "  Scalar:  http://localhost:5252/scalar/v1"
echo "Troubleshooting: docs/troubleshooting.md"
echo "Press Ctrl+C to stop foreground shell (child processes may keep running)."

wait
