#!/usr/bin/env bash
# =============================================================================
# server-setup.sh — ONE-TIME bootstrap for a fresh Ubuntu 22.04 / 24.04 Droplet
# Run as root (or with sudo): bash server-setup.sh
# =============================================================================
set -euo pipefail

APP_USER="zogreo"
APP_DIR="/opt/zogreo"
REPO_URL="${REPO_URL:-}"   # set this env var before running, or edit below
# REPO_URL="https://github.com/YOUR_ORG/zogreo-api.git"

echo "==> Updating system packages"
apt-get update -y
apt-get upgrade -y

# ---------------------------------------------------------------------------
# 1. .NET 8 SDK
# ---------------------------------------------------------------------------
echo "==> Installing .NET 8"
wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb \
  -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
apt-get update -y
apt-get install -y dotnet-sdk-8.0

# ---------------------------------------------------------------------------
# 2. PostgreSQL 16
# ---------------------------------------------------------------------------
echo "==> Installing PostgreSQL 16"
apt-get install -y postgresql postgresql-contrib

systemctl enable postgresql
systemctl start postgresql

# Create DB user and database
DB_PASS=$(openssl rand -base64 24)
sudo -u postgres psql -c "CREATE USER zogreo WITH PASSWORD '$DB_PASS';" 2>/dev/null || true
sudo -u postgres psql -c "CREATE DATABASE zogreo OWNER zogreo;" 2>/dev/null || true

echo ""
echo "  *** PostgreSQL password for user 'zogreo': $DB_PASS ***"
echo "  Put this in /opt/zogreo/.env as POSTGRES_PASSWORD"
echo ""

# ---------------------------------------------------------------------------
# 3. Redis (for OTP / distributed cache)
# ---------------------------------------------------------------------------
echo "==> Installing Redis"
apt-get install -y redis-server
sed -i 's/^supervised no/supervised systemd/' /etc/redis/redis.conf
systemctl enable redis-server
systemctl start redis-server

# ---------------------------------------------------------------------------
# 4. Nginx
# ---------------------------------------------------------------------------
echo "==> Installing Nginx"
apt-get install -y nginx
systemctl enable nginx
systemctl start nginx

# ---------------------------------------------------------------------------
# 5. Certbot (Let's Encrypt)
# ---------------------------------------------------------------------------
echo "==> Installing Certbot"
apt-get install -y certbot python3-certbot-nginx

# ---------------------------------------------------------------------------
# 6. Application user + directory
# ---------------------------------------------------------------------------
echo "==> Creating app user and directory"
id "$APP_USER" &>/dev/null || useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"
mkdir -p "$APP_DIR"/{app,uploads,logs}
chown -R "$APP_USER":"$APP_USER" "$APP_DIR"

# ---------------------------------------------------------------------------
# 7. Clone repo (if REPO_URL is set)
# ---------------------------------------------------------------------------
if [ -n "$REPO_URL" ]; then
  echo "==> Cloning repository"
  git clone "$REPO_URL" "$APP_DIR/repo"
  chown -R "$APP_USER":"$APP_USER" "$APP_DIR/repo"
else
  echo "  REPO_URL not set — skipping git clone. Clone manually into $APP_DIR/repo"
fi

# ---------------------------------------------------------------------------
# 8. Create .env placeholder
# ---------------------------------------------------------------------------
if [ ! -f "$APP_DIR/.env" ]; then
  cat > "$APP_DIR/.env" <<'EOF'
# Fill every value before running deploy.sh
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000

ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=zogreo;Username=zogreo;Password=FILL_ME
ConnectionStrings__Redis=localhost:6379

Jwt__Key=FILL_ME_AT_LEAST_32_CHARS
Jwt__Issuer=zogreo-api
Jwt__Audience=zogreo-clients
Jwt__Hours=24

Paystack__SecretKey=sk_live_FILL_ME
Paystack__PublicKey=pk_live_FILL_ME
Paystack__BaseUrl=https://api.paystack.co
Paystack__SchoolSubaccountCode=ACCT_FILL_ME

AfricasTalking__ApiKey=FILL_ME
AfricasTalking__Username=FILL_ME
AfricasTalking__SenderId=Zogreo

Email__SmtpHost=smtp.example.com
Email__SmtpPort=587
Email__SmtpUser=noreply@zogreo.ac.ke
Email__SmtpPass=FILL_ME
Email__FromAddress=noreply@zogreo.ac.ke
Email__FromName=Zogreo Institute

DefaultOrganization__Slug=zogreo
DefaultOrganization__Name=Zogreo Bible & Technical Training Institute
DefaultOrganization__AdmissionPrefix=ZBTTI

FileStorage__UploadsPath=/opt/zogreo/uploads
FileStorage__BaseUrl=https://YOUR_DOMAIN/uploads

Seed__AdminEmail=admin@zogreo.ac.ke
Seed__AdminPhone=+254700000000
Seed__AdminPassword=FILL_ME_STRONG_PASSWORD
EOF
  chown "$APP_USER":"$APP_USER" "$APP_DIR/.env"
  chmod 600 "$APP_DIR/.env"
fi

echo ""
echo "============================================================"
echo "  Server bootstrap complete."
echo ""
echo "  Next steps:"
echo "  1. Edit /opt/zogreo/.env and fill in all FILL_ME values"
echo "  2. Copy deploy/zogreo-api.service -> /etc/systemd/system/"
echo "  3. Copy deploy/zogreo-api.nginx -> /etc/nginx/sites-available/zogreo-api"
echo "  4. Run: bash deploy/deploy.sh"
echo "============================================================"
