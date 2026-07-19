# SP-2 & SP-3: Desktop Outbound Sync Engine Specification

**Date:** 2026-06-21
**Author:** Buabeng Emmanuel Aikins (with Claude)
**Status:** Draft / Ready for Implementation

> Historical design specification. The sync engine has since been partially
> implemented. Use `../../OUTSTANDING_FEATURES_TRACKER.md` for current status,
> remaining conflict/tenant-safety work, and completion criteria.

## 1. Overview
As part of the Offline-First Cloud Sync Architecture, the legacy desktop application (WinForms) must be augmented with a background service capable of detecting internet connectivity (SP-2) and synchronizing local data to the central web platform (SP-3).

This document outlines the technical specification for building the C# Outbound Sync Engine that will run on the school's primary LAN node.

---

## 2. SP-2: Connectivity & Scheduler

### 2.1 Configuration Storage
The engine requires a secure local configuration file (e.g., `syncsettings.json` or encrypted registry keys) storing:
- `CloudBaseUrl`: The URL of the web platform (e.g., `https://kingdomprep.azurewebsites.net`).
- `SyncApiKey`: The shared secret key matching the server's `Sync:ApiKey`.
- `SchoolId`: The GUID identifying the local school tenant.
- `DeviceId`: A unique GUID generated once per primary installation.

### 2.2 Connectivity Service
A background thread (`System.Threading.Timer` or `BackgroundWorker`) will run periodically (e.g., every 5 minutes).
- **Ping Check**: It will send a `GET` request to `[CloudBaseUrl]/api/sync/status`.
- **Validation**: If it receives a HTTP 200 OK with `Enabled = true`, the engine knows it is online and authorized.
- **Failures**: If offline or unauthorized, it backs off (exponential backoff up to 30 minutes) and tries again later.

---

## 3. SP-3: Sync Engine (Push/Pull)

The core sync process executes when the Connectivity Service detects an active connection.

### 3.1 Push Operation (Outbound)
The engine queries the local SQL database for records modified offline.

**Steps:**
1. For each synced table (e.g., `Students`, `Employees`, `StudentFeeLedger`), query for rows where `SyncState = 'Pending'`.
2. Map the results to a uniform JSON payload (`SyncUploadRequest`).
   - Format: `{ "schoolId": "...", "deviceId": "...", "tables": [ { "tableName": "Students", "rows": [ { ... } ] } ] }`
3. Send a `POST` request to `[CloudBaseUrl]/api/sync/upload` with the `X-Sync-Key` and `X-School-Id` headers.
4. On success (HTTP 200), update the local records in the database to `SyncState = 'Synced'`.

### 3.2 Pull Operation (Inbound)
The engine pulls updates made on the web platform (e.g., by parents or admins).

**Steps:**
1. The engine maintains a local table `SyncWatermarks (TableName, LastUpdatedAt)`.
2. For each allowed table, it sends a `POST` to `[CloudBaseUrl]/api/sync/pull` providing the `TableName` and `Since` (watermark).
3. The server responds with an array of modified rows.
4. The desktop app merges these into the local database using a **Last-Write-Wins (LWW)** strategy:
   - Match by `SyncId`.
   - If local doesn't exist, `INSERT`.
   - If local exists, compare `UpdatedAt`. If remote `UpdatedAt >` local `UpdatedAt`, then `UPDATE`.
5. Update the local `SyncWatermarks` table with the highest `UpdatedAt` received.

### 3.3 SMS Outbox Flushing (SP-0 Integration)
As part of the push loop, the engine will query the local `SmsOutbox` where `Status = 'Pending'`.
- It will batch these SMS messages and either send them directly via the local SMS API integration, OR push them to the cloud to be processed by a cloud SMS worker.
- Once sent/pushed, the local status is updated to `'Sent'`.

---

## 4. Error Handling & Edge Cases
- **Idempotency**: If the internet drops during a push, the local `SyncState` remains `'Pending'`. The next run will push the same records again. The web endpoint (`SyncInboxService`) safely handles duplicate `SyncId`s.
- **Data Serialization**: The payload rows are represented as `Dictionary<string, object>` or JSON elements to accommodate varying schemas without hardcoding C# models in the sync engine core.
- **Concurrency**: The sync loop is locked to prevent overlapping executions if a push/pull cycle takes longer than the timer interval.

## 5. Next Implementation Steps
1. Create a class library (e.g., `KingdomPrep.Desktop.Sync`) to isolate this logic from the legacy WinForms UI.
2. Implement `ISyncConfigProvider`, `ConnectivityService`, and `SyncEngine` classes.
3. Hook the `SyncEngine.StartAsync()` into the WinForms `Program.cs` startup pipeline on the Primary LAN node.
