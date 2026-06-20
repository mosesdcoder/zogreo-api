#!/usr/bin/env bash
# =============================================================================
# deploy.sh — Run on the Droplet for every release
# Usage: bash /opt/zogreo/repo/deploy/deploy.sh
# =============================================================================
set -euo pipefail

APP_DIR="/opt/zogreo"
REPO_DIR="$APP_DIR/repo"
PUBLISH_DIR="$APP_DIR/app"
SERVICE="zogreo-api"
DOTNET_PROJECT="src/Zogreo.Api/Zogreo.Api.csproj"
MIGRATIONS_PROJECT="src/Zogreo.Infrastructure/Zogreo.Infrastructure.csproj"

echo "==> [1/6] Pulling latest code"
cd "$REPO_DIR"
git fetch --all
git reset --hard origin/main      # change 'main' to your branch if different

echo "==> [2/6] Restoring NuGet packages"
dotnet restore src/Zogreo.sln

echo "==> [3/6] Building & publishing (Release)"
dotnet publish "$DOTNET_PROJECT" \
  --configuration Release \
  --output "$PUBLISH_DIR" \
  --no-restore \
  --self-contained false

echo "==> [4/6] Running EF Core migrations"
# Load env so the connection string is available
set -o allexport
# shellcheck disable=SC1091
source "$APP_DIR/.env"
set +o allexport

dotnet ef database update \
  --project "$MIGRATIONS_PROJECT" \
  --startup-project "$DOTNET_PROJECT" \
  --no-build \
  --configuration Release

echo "==> [5/6] Setting file ownership"
chown -R zogreo:zogreo "$PUBLISH_DIR"
chown -R zogreo:zogreo "$APP_DIR/uploads"
chown -R zogreo:zogreo "$APP_DIR/logs"

echo "==> [6/6] Restarting service"
systemctl daemon-reload
systemctl restart "$SERVICE"
systemctl is-active --quiet "$SERVICE" \
  && echo "  ✓ $SERVICE is running" \
  || { echo "  ✗ $SERVICE failed to start — check: journalctl -u $SERVICE -n 50"; exit 1; }

echo ""
echo "  Deploy complete. Health check:"
sleep 2
curl -sf http://127.0.0.1:5000/health && echo " ✓ /health OK" || echo " ✗ /health not responding yet (give it a few seconds)"
