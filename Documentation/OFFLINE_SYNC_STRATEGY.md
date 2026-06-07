# Offline-First Synchronization Strategy
# Nyansapo School ERP

**Date:** June 6, 2026
**Architecture Style:** Local Cache with Background Synchronization (Hybrid)

---

## 1. The Challenge
In many regions, internet connectivity can be unstable. A school cannot stop operations (registering students, taking fees) just because the internet is down. The system must remain fully functional 100% of the time.

## 2. Technical Architecture

### 2.1 "Local-First" Data Flow
The application will **not** connect directly to the internet for daily operations. Instead:
1.  **Local Store:** Every installation uses its local `(localdb)\MSSQLLocalDB` (the `Neat_Academy.mdf` file) as its primary, high-speed database.
2.  **Zero Latency:** Because the database is on the hard drive, the UI is instant and never "waits" for the cloud.
3.  **Operation Continuity:** Admissions, Fees, Exams, and Attendance work exactly the same way whether the internet is ON or OFF.

### 2.2 The Sync Queue (Change Tracking)
To keep the cloud database updated, we introduce a **Sync Queue** table in the local database.

**Table: `SyncQueue`**
*   `QueueId` (PK)
*   `TableName` (e.g., "Students", "Fees")
*   `RecordId` (The ID of the specific row changed)
*   `Operation` (INSERT, UPDATE, DELETE)
*   `ChangeData` (JSON snapshot of the record)
*   `SyncStatus` (PENDING, SYNCED, FAILED)

### 2.3 Background Sync Service
The application will run a lightweight background thread (or a Windows Service) that:
1.  **Detects Connection:** Periodically pings a central server (e.g., Google.com or your Cloud API).
2.  **Push Data:** If online, it reads the `PENDING` items from the `SyncQueue` and pushes them to the Cloud Database.
3.  **Conflict Resolution:** If the same record was changed on two different computers, the system uses the `Timestamp` to keep the newest version.

---

## 3. Database Schema Requirements

To make this work safely, we must update all tables with two specific columns:
*   **`RowGuid` (uniqueidentifier):** A global unique ID that is the same on the local PC and the Cloud. Unlike an auto-incrementing `Id` (1, 2, 3), a GUID (e.g., `550e8400-e29b...`) is guaranteed to be unique across all school computers.
*   **`LastModified` (datetime):** Used to determine which record is the "latest" during a sync.

---

## 4. Implementation Roadmap

### Phase 1: Local Preparation
*   Add `RowGuid` and `LastModified` columns to all primary tables (Students, Employees, Fees).
*   Create the `SyncQueue` table locally.
*   Update Repositories (e.g., `StudentRepository`) to add an entry to the `SyncQueue` whenever a `Save` or `Update` happens.

### Phase 2: The Cloud Bridge
*   Build a lightweight Web API (ASP.NET Core) hosted on Azure or AWS.
*   The API receives "Sync Packets" from the school computer and saves them to a central SQL Server.

### Phase 3: Background Worker
*   Implement a `SyncManager` class in the C# project.
*   Use a `System.Timers.Timer` to attempt a sync every 5 or 10 minutes.
*   Add a "Sync Status" icon to the Dashboard (Green = Synced, Yellow = Pending, Red = Offline).

---

## 5. Security Note
All data synced to the cloud must be **encrypted in transit** using SSL/TLS. Since this involves sensitive student and financial data, the Cloud API will require an **API Key** unique to each school installation.
