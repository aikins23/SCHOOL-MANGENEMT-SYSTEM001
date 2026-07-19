# Nyansapo ERP - Web Platform Deployment Guide

**Version:** 1.1
**Target Environment:** Windows Server / Linux (Ubuntu 22.04) / Azure App Service

---

## 1. System Requirements

### Hardware (Server)
- **Processor:** 2+ Cores
- **RAM:** 4 GB minimum (8 GB recommended for heavy reporting)
- **Storage:** 20 GB SSD

### Software
- **Operating System:** Windows Server 2019+ with IIS OR Linux (Ubuntu) with Nginx/Apache
- **Runtime:** .NET 10.0 ASP.NET Core Runtime
- **Database:** Microsoft SQL Server (Express, Web, Standard, or Enterprise)
- **Transport:** HTTPS certificate for the public portal domain

---

## 2. SQL Server Database Setup

The Web Platform requires a central SQL Server database that all Desktop instances will synchronize with.

1. Install SQL Server and SQL Server Management Studio (SSMS).
2. Create a new database named `Nyansapo_Cloud_Db` (or similar).
3. Ensure SQL Server Authentication is enabled. Create a dedicated user (e.g., `nyansapo_web`) and grant it only the rights required by the deployed app.
4. Restore or create the desktop-owned school schema before the web app starts. The web app only creates web-owned support tables.
5. Keep the connection string handy for Step 3.

Production must use a reachable SQL Server host. Do not use LocalDB, `.`, `(local)`, or `localhost` in production.

---

## 3. Configuring the Web Application

Before publishing the Blazor Server application, configure production settings through environment variables, host secrets, Azure App Service configuration, or IIS environment variables. Do not commit real secrets into `appsettings.json`.

The app validates production configuration at startup and refuses unsafe settings.

### Required production settings

- `ConnectionStrings:Default`
- `AllowedHosts`
- `Paystack:PublicKey`
- `Paystack:SecretKey`
- `Sync:ProvisioningKey`
- `Sync:AllowSharedApiKeyFallback=false`
- `Security:AllowLegacyPlainTextPasswords=false`
- `Security:SyncUploadMaxBytes`
- `ForwardedHeaders:KnownProxies` when running behind a trusted reverse proxy

### Example production configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "portal.yourschooldomain.com",
  "ConnectionStrings": {
    "Default": "Server=tcp:sql.yourschooldomain.com,1433;Initial Catalog=Nyansapo_Cloud_Db;User ID=nyansapo_web;Password=YourSecurePassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Paystack": {
    "PublicKey": "pk_live_your_public_key",
    "SecretKey": "sk_live_your_secret_key"
  },
  "Sync": {
    "ApiKey": "",
    "AllowSharedApiKeyFallback": false,
    "ProvisioningKey": "use-a-server-only-secret-with-at-least-32-characters"
  },
  "Security": {
    "AllowLegacyPlainTextPasswords": false,
    "SyncUploadMaxBytes": 2097152
  },
  "ForwardedHeaders": {
    "KnownProxies": ["10.0.0.10"]
  }
}
```

`Security:SyncUploadMaxBytes` must be between `262144` and `10485760` bytes in production. The default `2097152` bytes is suitable for normal sync batches.

If the reverse proxy is on the same host and no trusted proxy IP is configured, only configure forwarded headers after confirming the actual proxy IP. The app uses trusted forwarded headers for client IP based login and sync rate limiting.

---

## 4. Publishing the Blazor Server App

1. Open a terminal/command prompt in the `web/KingdomPrep.Web/` directory.
2. Run the dotnet publish command:
   ```bash
   dotnet publish -c Release -o ./publish
   ```
3. The compiled files will be located in the `web/KingdomPrep.Web/publish/` folder.

Before publishing, run:

```bash
dotnet build web/KingdomPrep.Web/KingdomPrep.Web.csproj
dotnet test web/KingdomPrep.Web.Tests/KingdomPrep.Web.Tests.csproj
```

---

## 5. Hosting on IIS (Windows Server)

1. **Install IIS and ASP.NET Core Hosting Bundle**:
   - Ensure IIS is enabled via Server Manager.
   - Download and install the [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0).
   - Restart IIS: `net stop was /y` followed by `net start w3svc`.

2. **Create the IIS Site**:
   - Open IIS Manager.
   - Right-click "Sites" -> "Add Website".
   - Site name: `NyansapoWeb`
   - Physical path: Point this to the copied `publish` folder (e.g., `C:\inetpub\wwwroot\nyansapo`).
   - Binding: Set the hostname (e.g., `portal.nyansapoerp.com`).

3. **Configure Application Pool**:
   - Go to "Application Pools" in IIS.
   - Find the pool created for `NyansapoWeb`.
   - Double-click and set **.NET CLR version** to **No Managed Code**.

4. **Permissions**:
   - Ensure the `IIS_IUSRS` group has Read/Execute permissions on the `publish` folder.

---

## 6. Hosting on Linux (Nginx/Ubuntu)

If hosting on a Linux server, follow these steps:

1. **Install .NET 10 Runtime**:
   ```bash
   sudo apt-get update
   sudo apt-get install -y aspnetcore-runtime-10.0
   ```

2. **Copy Published Files**:
   Copy the contents of the `publish` folder to `/var/www/nyansapo/`.

3. **Create a Systemd Service**:
   Create `/etc/systemd/system/nyansapo.service`:
   ```ini
   [Unit]
   Description=Nyansapo ERP Web Portal

   [Service]
   WorkingDirectory=/var/www/nyansapo
   ExecStart=/usr/bin/dotnet /var/www/nyansapo/KingdomPrep.Web.dll
   Restart=always
   RestartSec=10
   SyslogIdentifier=nyansapo-web
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://localhost:5000

   [Install]
   WantedBy=multi-user.target
   ```
   Start the service: `sudo systemctl enable nyansapo.service && sudo systemctl start nyansapo.service`

4. **Configure Nginx Reverse Proxy**:
   Create `/etc/nginx/sites-available/nyansapo`:
   ```nginx
   map $http_upgrade $connection_upgrade {
       default upgrade;
       '' close;
   }

   server {
       listen 80;
       server_name portal.nyansapoerp.com;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection $connection_upgrade;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```
   Enable the site: `sudo ln -s /etc/nginx/sites-available/nyansapo /etc/nginx/sites-enabled/`
   Restart Nginx: `sudo systemctl restart nginx`

---

## 7. Verifying the Deployment

1. Navigate to `https://portal.nyansapoerp.com` (or your configured domain).
2. The login page should load successfully.
3. Attempt to log in with an Administrator account.
4. Verify that unsafe production settings are not present:
   - `AllowedHosts` is not `*`.
   - `ConnectionStrings:Default` does not use LocalDB or localhost.
   - Paystack keys are live keys and not blank.
   - `Sync:ProvisioningKey` is at least 32 characters.
   - `Sync:AllowSharedApiKeyFallback` is `false`.
   - `Security:AllowLegacyPlainTextPasswords` is `false`.
5. Verify the sync status endpoint:
   - Open `https://portal.nyansapoerp.com/api/sync/status` to confirm the route is reachable.
   - Test again from the desktop Sync Settings screen with `X-Sync-Key`, `X-School-Id`, and `X-Device-Id`.
6. Confirm security headers on the live site:
   - `Content-Security-Policy`
   - `X-Frame-Options: DENY`
   - `X-Content-Type-Options: nosniff`
7. Confirm rate limits are active:
   - Login POST requests are limited per client IP.
   - `/api/sync/*` requests are limited per client IP.

---

## 8. Sync Device Go-Live

Each desktop installation must be registered before cloud sync is enabled.

1. Complete desktop first-time setup and confirm the permanent `SchoolId`.
2. Open desktop Sync Settings and copy the `SchoolId` and `DeviceId`.
3. Register the device through the web registration endpoint:

   ```http
   POST /api/sync/register-device
   X-Provisioning-Key: <server-provisioning-key>
   Content-Type: application/json
   ```

   ```json
   {
     "schoolId": "00000000-0000-0000-0000-000000000000",
     "deviceId": "00000000-0000-0000-0000-000000000000",
     "deviceName": "Accounts Office Desktop",
     "syncApiKey": "use-a-long-unique-random-key",
     "isActive": true,
     "licenseStatus": "Active",
     "expiresAtUtc": null
   }
   ```

4. Enter the issued sync API key into the desktop Sync Settings screen.
5. Run the desktop Sync Status check.
6. Confirm the desktop can upload queued changes and pull web changes.

The web server stores only a hash of the sync API key. Keep the original key in the school's deployment record because it cannot be recovered from the database.

---

## 9. Backup, Rollback, And Recovery

Before go-live:

- Take a SQL Server backup of the production database.
- Keep the previous web publish folder or deployment artifact.
- Keep the current desktop installer/build available.
- Confirm a test restore works before handing the portal to users.

Rollback plan:

1. Stop the web app.
2. Restore the previous publish artifact.
3. Restore the SQL Server backup only if schema/data changes caused the issue.
4. Re-run login and sync verification before reopening access.

Desktop offline operation depends on the local desktop database and sync outbox. If the network goes down, the desktop continues recording local changes and syncs when the connection returns.
