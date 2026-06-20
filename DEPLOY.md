# Zogreo API — DigitalOcean Droplet Deployment Runbook

## Prerequisites

| Item | Minimum |
|---|---|
| Droplet OS | Ubuntu 22.04 or 24.04 LTS |
| Droplet size | 2 vCPU / 2 GB RAM (4 GB recommended) |
| Domain | Pointed at Droplet IP via A-record |
| SSH access | Root or sudo user |
| Git remote | Repo pushed to GitHub (or any remote) |

---

## Step 1 — SSH into the Droplet

```bash
ssh root@YOUR_DROPLET_IP
```

---

## Step 2 — Run the one-time server setup

```bash
# Set your repo URL, then run the script
export REPO_URL=https://github.com/YOUR_ORG/zogreo-api.git
bash /path/to/deploy/server-setup.sh
```

The script installs: **.NET 8 SDK**, **PostgreSQL 16**, **Redis**, **Nginx**, **Certbot**.

It also:
- Creates a `zogreo` system user and `/opt/zogreo/{app,uploads,logs}` directories.
- Clones the repo into `/opt/zogreo/repo`.
- Creates `/opt/zogreo/.env` with placeholder values.
- Prints the auto-generated PostgreSQL password — **save it now**.

---

## Step 3 — Fill in secrets

```bash
nano /opt/zogreo/.env
```

Fill every `FILL_ME` value:

| Key | Where to get it |
|---|---|
| `ConnectionStrings__Postgres` | Use password printed in Step 2 |
| `Jwt__Key` | Generate: `openssl rand -base64 48` |
| `Paystack__SecretKey` | Paystack Dashboard → Settings → API Keys |
| `Paystack__PublicKey` | Same |
| `Paystack__SchoolSubaccountCode` | Paystack Dashboard → Settlements → Subaccounts |
| `AfricasTalking__ApiKey` | Africa's Talking Dashboard |
| `Email__SmtpPass` | Your SMTP provider |
| `FileStorage__BaseUrl` | `https://YOUR_DOMAIN/uploads` |
| `Seed__AdminPassword` | A strong password (min 12 chars) |

Save with `Ctrl+O`, exit with `Ctrl+X`. The file is `chmod 600` so only root/service can read it.

---

## Step 4 — Install the systemd service

```bash
cp /opt/zogreo/repo/deploy/zogreo-api.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable zogreo-api
```

---

## Step 5 — Configure Nginx

```bash
# 1. Replace YOUR_DOMAIN in the config
sed -i 's/YOUR_DOMAIN/api.zogreo.ac.ke/g' /opt/zogreo/repo/deploy/zogreo-api.nginx

# 2. Install the config
cp /opt/zogreo/repo/deploy/zogreo-api.nginx /etc/nginx/sites-available/zogreo-api
ln -s /etc/nginx/sites-available/zogreo-api /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default   # remove default placeholder

# 3. Test config
nginx -t

# 4. Reload
systemctl reload nginx
```

---

## Step 6 — Get a TLS certificate

```bash
certbot --nginx -d api.zogreo.ac.ke
```

Follow the prompts. Certbot auto-renews via a systemd timer — verify:

```bash
systemctl status certbot.timer
```

---

## Step 7 — Run the first deploy

```bash
bash /opt/zogreo/repo/deploy/deploy.sh
```

This will:
1. `git pull` from `origin/main`
2. `dotnet publish` in Release mode
3. Run EF Core migrations
4. Restart the `zogreo-api` service

---

## Step 8 — Verify

```bash
# Service status
systemctl status zogreo-api

# Live logs
journalctl -u zogreo-api -f

# Health endpoint
curl https://api.zogreo.ac.ke/health

# Swagger (if enabled in Production appsettings)
open https://api.zogreo.ac.ke/swagger
```

---

## Subsequent deploys

Every time you push a new version:

```bash
ssh root@YOUR_DROPLET_IP
bash /opt/zogreo/repo/deploy/deploy.sh
```

Or wire it into a GitHub Actions workflow (see below).

---

## Optional: GitHub Actions CI/CD

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Droplet

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: SSH and deploy
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.DROPLET_IP }}
          username: root
          key: ${{ secrets.DROPLET_SSH_KEY }}
          script: bash /opt/zogreo/repo/deploy/deploy.sh
```

Add `DROPLET_IP` and `DROPLET_SSH_KEY` in GitHub → Settings → Secrets.

---

## Useful commands

```bash
# Restart API
systemctl restart zogreo-api

# Tail logs
journalctl -u zogreo-api -n 100 -f

# Run a migration manually
cd /opt/zogreo/repo
source /opt/zogreo/.env
dotnet ef database update \
  --project src/Zogreo.Infrastructure \
  --startup-project src/Zogreo.Api

# PostgreSQL shell
sudo -u postgres psql zogreo

# Redis CLI
redis-cli ping

# Check Nginx errors
tail -f /var/log/nginx/error.log

# Renew TLS manually
certbot renew --dry-run
```

---

## Firewall (UFW) — recommended

```bash
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP (redirects to HTTPS)
ufw allow 443/tcp   # HTTPS
ufw deny 5432       # PostgreSQL — never expose publicly
ufw deny 6379       # Redis — never expose publicly
ufw enable
ufw status
```

---

## File layout on server

```
/opt/zogreo/
  .env              ← secrets (chmod 600, owned root:root)
  app/              ← published .NET binaries (deploy target)
  uploads/          ← applicant document files
  logs/             ← app logs (if written to disk)
  repo/             ← git clone of this repository
    deploy/
      server-setup.sh
      deploy.sh
      zogreo-api.service
      zogreo-api.nginx
```
