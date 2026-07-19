# School Activation And Sync Registration

Nyansapo ERP must treat every school as a separate tenant. The permanent `SchoolId`
is the tenant boundary, and every desktop installation that syncs with the web app
must be registered against that same `SchoolId`.

## Core Rules

- A school can change its name, phone number, address, email, logo, and portal URL.
- A school must not change its permanent `SchoolId` after setup.
- Desktop records must always carry the active `SchoolId`.
- Web records must always be filtered by the logged-in user's `SchoolId`.
- Sync requests must include the school's `SchoolId`, the desktop `DeviceId`, and
  the device's sync API key.

## First-Time Setup Flow

1. The desktop app creates or loads a permanent `SchoolId`.
2. The administrator completes the school profile.
3. The administrator creates the first admin account.
4. All future desktop records are written under that permanent `SchoolId`.
5. School profile edits update branding and contact details only; they do not
   generate a new `SchoolId`.

## Desktop Sync Identity

Each desktop device has a stable `DeviceId` stored through the local sync outbox
device registration. The Sync Settings screen displays:

- Permanent School ID
- Registered Desktop Device ID
- Parent/Web Portal URL
- Sync API Key

The school should copy the School ID and Device ID from Sync Settings when the
device is being activated on the web server.

## Web Device Registration

The web app now supports a protected registration endpoint:

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

The web app stores only a SHA-256 hash of the sync API key and the key's last four
characters for support identification.

## Required Server Settings

Set these as environment variables, user secrets, or deployment secrets. Do not
commit real secrets into `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "Default": "Server=tcp:sql.yourschooldomain.com,1433;Initial Catalog=Nyansapo_Cloud_Db;User ID=nyansapo_web;Password=YourSecurePassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "AllowedHosts": "portal.yourschooldomain.com",
  "Sync": {
    "ApiKey": "",
    "AllowSharedApiKeyFallback": false,
    "ProvisioningKey": "server-only-registration-secret"
  },
  "Security": {
    "AllowLegacyPlainTextPasswords": false,
    "SyncUploadMaxBytes": 2097152
  }
}
```

`AllowSharedApiKeyFallback` should stay `false` for sellable multi-school
deployments. Use registered devices instead.

Production also rejects LocalDB or localhost connection strings, wildcard
`AllowedHosts`, blank Paystack keys, short provisioning keys, plaintext password
fallback, and sync upload limits outside `262144` to `10485760` bytes.

## Sync Request Protection

The desktop app sends:

- `X-Sync-Key`
- `X-School-Id`
- `X-Device-Id`

The web app checks that:

- The school exists in the registered device table.
- The device belongs to that school.
- The supplied sync key matches the stored hash.
- The device is active.
- The license status is `Active`.
- The device registration has not expired.

## Operational Guidance

For every new school installation:

1. Run first-time setup on the desktop.
2. Open Sync Settings and copy the School ID and Device ID.
3. Register that device on the web server.
4. Put the issued sync API key into the desktop Sync Settings.
5. Test the connection from Sync Settings before enabling scheduled uploads.
6. Open Sync Status and run the full check. The desktop should confirm:
   - local sync infrastructure exists;
   - endpoint URL is configured;
   - sync API key is configured;
   - pending outbox count can be read;
   - the web endpoint is reachable.

For every school profile update:

1. Update school name, address, logo, phone, or email as needed.
2. Confirm the `SchoolId` did not change.
3. Confirm dashboard data still appears on desktop.
4. Confirm the web portal still filters by the same `SchoolId`.

## Why This Matters

This prevents one school from accidentally uploading into another school's tenant,
and it blocks an unregistered desktop copy from pushing or pulling school records.
It also gives support staff a clean way to deactivate a stolen, expired, or replaced
computer without touching the school's permanent identity.

## Offline Desktop Rule

The desktop app must remain usable when internet access is unavailable. Local
write actions are recorded in the desktop database and sync outbox. When the
network returns, the sync service uploads queued changes and pulls web-side
changes without depending on the date or time the outage occurred.

Do not point the desktop directly at the production web database as its only
runtime database. The desktop needs its local database for offline operation;
the web database is the central cloud copy used for synchronization.
