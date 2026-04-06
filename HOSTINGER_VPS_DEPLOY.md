# Hostinger VPS Deploy

This repository includes a Hostinger-ready deployment script for Ubuntu VPS.

## Run on the VPS

Open the Hostinger web terminal as `root` and run:

```bash
bash <(curl -fsSL https://raw.githubusercontent.com/jiteshryecsyadav-cmyk/staffnex/main/deploy/hostinger-vps/install.sh)
```

The script will:

- install required packages
- install .NET 8 SDK if needed
- clone or update this GitHub repository
- publish the API to `/var/www/staffnex-api`
- prompt for production connection string and JWT settings
- create a `systemd` service
- configure `nginx`

## What you need before running it

- SQL Server connection string for production
- JWT key with at least 32 characters
- frontend domain or origin if browser clients will call the API

## Verify after deploy

```bash
systemctl status staffnex-api
journalctl -u staffnex-api -n 100 --no-pager
curl http://127.0.0.1:5000/health
curl http://YOUR_SERVER_IP/health
```