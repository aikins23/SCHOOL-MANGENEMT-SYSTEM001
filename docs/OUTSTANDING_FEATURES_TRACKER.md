# Outstanding Features Tracker

Last updated: 2026-07-19
Project: Nyansapo ERP School Management System
Scope: Desktop WinForms, Blazor web, SQL Server, synchronization, integrations, deployment

## Purpose

This is the living source of truth for features that were requested but are not implemented, are only partially implemented, or have not been proven end to end.

Historical documents remain useful snapshots, but this tracker must be updated after each task. A feature is not marked `DONE` merely because a screen or service exists. It must satisfy its completion criteria and include verification evidence.

## Status Definitions

| Status | Meaning |
|---|---|
| `NOT STARTED` | The required user workflow does not exist. |
| `PARTIAL` | Some code or UI exists, but the complete workflow is missing. |
| `VERIFY` | Implementation appears complete, but live database, UI, device, or external-service verification is outstanding. |
| `BLOCKED` | Work cannot be completed until a named external dependency is available. |
| `DEFERRED` | Intentionally postponed by product decision. |
| `DONE` | Completion criteria have passed and evidence is recorded. |

## Latest Verification Snapshot

Snapshot date: 2026-07-18

- Desktop solution build: passed with 0 errors and 0 warnings.
- Web solution build: passed with 0 errors and 0 warnings.
- Web automated tests: 179 passed, 0 failed.
- Desktop unit tests: 54 passed, 0 failed.
- Desktop SQL integration tests: 24 remained skipped in the latest full-suite run because the separate test-master login is not yet configured for the runner.
- Reason for desktop skips: SQL integration test login/master connection is not currently available to the test runner.
- Live production database connection: working through SQL Server and `SqlClient`.
- Latest observed live records: 178 students, 566 payment records, and 352 additional-fee student charges.

These numbers are a dated snapshot and must be refreshed after database promotion, restoration, or test-environment changes.

## Active Backlog

### Pre-Production Launch Readiness

Detailed evidence, severity, and technical recommendations are in [PRE_PRODUCTION_LAUNCH_READINESS_AUDIT_JULY_2026.md](PRE_PRODUCTION_LAUNCH_READINESS_AUDIT_JULY_2026.md).

| ID | Launch gate | Status | Remaining work | Completion criteria |
|---|---|---|---|---|
| `LAUNCH-SEC-01` | Secret rotation and SQL transport security | `PARTIAL` | Live credentials are rotated, retired values were removed from reachable GitHub history, automated full-history scanning is active, forced SQL encryption is enabled, and this workstation validates the bound `localhost` certificate. Use a CA-issued DNS certificate, or securely distribute the private CA trust anchor, before multi-machine deployment. | Secret scan is clean, old credentials fail, deployment secrets are injected securely, and every deployed SQL client validates an appropriately trusted certificate. |
| `LAUNCH-SEC-02` | Activate web security and API startup wiring | `PARTIAL` | Register deployment validation, security headers, request limits, sync endpoints, payment webhook endpoints, authorization, and antiforgery in `Program.cs`. | Startup tests prove required endpoints and headers are active, unsafe production settings stop startup, and direct unauthorized requests fail. |
| `LAUNCH-PAY-01` | Payment secret and webhook hardening | `PARTIAL` | Encrypt provider secrets and make verified intent validation, ledger posting, and intent completion atomic and idempotent. | Unknown, altered, duplicate, expired, cross-school, and failed callbacks cannot post money; valid callbacks post once. |
| `LAUNCH-TENANT-01` | Enforced school isolation | `PARTIAL` | Make school scope mandatory, add tenant query guards/filters and school-scoped keys, and remove null-to-all fallbacks. | Cross-school read, write, update, delete, payment, report, and sync tests all fail closed. |
| `LAUNCH-DB-01` | Versioned schema deployment and least privilege | `PARTIAL` | Remove runtime DDL from normal repositories/services and deploy schema changes with a separate migration identity. | Application runtime accounts have no DDL rights, schema upgrades are versioned and repeatable, and incompatible startup fails clearly. |
| `LAUNCH-OBS-01` | Production monitoring and health | `NOT STARTED` | Add structured centralized logs, correlation, redaction, readiness/liveness probes, metrics, and alerts. | Operators can detect and diagnose database, payment, sync, SMS, backup, and application failures within the agreed response time. |
| `LAUNCH-DR-01` | Proven disaster recovery | `PARTIAL` | Add checksum/verification, encryption, off-machine retention, independent scheduling, and a documented restore drill. | A fresh isolated environment is restored successfully within the approved RPO/RTO using the production runbook. |
| `LAUNCH-QA-01` | Zero-skip release verification | `BLOCKED` | Provision the SQL integration environment, run all desktop tests, and complete desktop/web role workflow smoke matrices. | Release pipeline passes with 0 failed and 0 skipped required tests, with smoke evidence attached. |
| `LAUNCH-PRIVACY-01` | Private portal indexing and browser storage safety | `PARTIAL` | Noindex private routes, narrow service-worker caching, remove private routes from sitemap, and clear/namespace profile localStorage. | Crawlers cannot index authenticated routes and shared-device logout leaves no previous user's private profile/cache data. |

### Offline, Sync, and Multi-Tenancy

| ID | Feature | Status | Remaining work | Completion criteria |
|---|---|---|---|---|
| `SYNC-01` | Full desktop offline operation | `PARTIAL` | Provide durable local storage for supported desktop writes while SQL Server/API is unavailable. Queue changes without losing user work. | Create records offline, restart the app, reconnect later, sync once, and confirm no data loss or duplicate rows. |
| `SYNC-02` | Conflict-safe synchronization | `PARTIAL` | Replace the placeholder desktop pull merge, apply real row merges/tombstones, and finish conflict ownership rules for all synchronized tables. | Automated and manual push/pull tests cover duplicates, deletion, simultaneous edits, retry, and interrupted sync. |
| `SYNC-03` | Complete tenant-safe synchronization | `PARTIAL` | Prove every payload, checkpoint, conflict, and local queue is isolated by school. | Cross-school isolation tests pass and no school can read or modify another school's sync data. |
| `SAAS-01` | School-specific links/subdomains | `NOT STARTED` | Add hosted school URL provisioning and domain resolution. | A registered school receives a working isolated URL and branding. |
| `SAAS-02` | Subscription and central Nyansapo administration | `NOT STARTED` | Add school registration, plans, subscription state, central support/admin, and school switching. | Central administrators can onboard, suspend, reactivate, and inspect schools without accessing unauthorized school data. |

### Desktop Stability and Core Workflows

| ID | Feature | Status | Remaining work | Completion criteria |
|---|---|---|---|---|
| `TEST-01` | Restore complete SQL integration test execution | `BLOCKED` | Provide the protected SQL test master connection in the fresh clone and run the SQL integration category. The current verified desktop gate covers 54 non-SQL tests. | Desktop suite completes with no excluded or skipped required SQL integration tests and no failures. |
| `FLOW-01` | Critical desktop workflow pass | `VERIFY` | Run login, add student, admission approval, fee payment, notice/SMS queue, grading, report card, promotion, and timetable through the real UI and test database. | Every workflow completes without freezing, silent failure, duplicate writes, or incorrect balances. |
| `UI-01` | Complete form freeze audit | `PARTIAL` | Inspect all forms for synchronous DB, PDF, SMS, network, and large data-loading work in UI event handlers. | Form-by-form smoke checklist passes; every long operation has busy state, cancellation/timeout where appropriate, and visible errors. |
| `UI-02` | Responsive desktop layout audit | `PARTIAL` | Finish dashboard, finance insight, quick actions, tables, split containers, scroll areas, and text wrapping across supported resolutions/scaling. | Visual smoke passes at 100%, 125%, and 150% scaling without clipping, overlap, or broken scrolling. |
| `LEDGER-01` | Payment Ledger filtering | `PARTIAL` | Connect search, date range, transaction type, class, term, and learner filters to paged database queries. | Filter combinations return correct totals and export exactly the filtered rows. |

### Finance, Payments, and Messaging

| ID | Feature | Status | Remaining work | Completion criteria |
|---|---|---|---|---|
| `FEE-01` | Additional fees end-to-end | `VERIFY` | Live-test flat, class, and department fees; direct submission; approval/rejection; idempotent posting; optional parent SMS; and parent balance breakdown. | Approved charges post once to the correct students and rejected/draft requests never affect balances. |
| `PAY-01` | Paystack live payment | `BLOCKED` | Test checkout, callback, webhook, reference, school, student, amount, currency, and duplicate protection with real keys. | A live or approved production-like transaction is verified server-side and posted exactly once. |
| `PAY-02` | Direct MTN MoMo live payment | `BLOCKED` | Test request-to-pay, pending state, success/failure polling, amount validation, school isolation, and idempotent posting with real credentials. | Real provider transaction completes and posts exactly once after verified success. |
| `SMS-01` | Production SMS delivery | `BLOCKED` | Verify admission, additional-fee, notice, and other required messages against real provider credentials and Ghanaian phone normalization. | Provider receipt confirms delivery to the intended parent/emergency number and failures remain retryable in the outbox. |
| `NOTICE-01` | Headmaster web broadcast | `NOT STARTED` | Wire the dashboard broadcast form to validated recipients, authorization, persistence, delivery queue, and audit logging. | Authorized broadcast appears in history and reaches only the selected audience. |

### Academics, Reports, and Timetable

| ID | Feature | Status | Remaining work | Completion criteria |
|---|---|---|---|---|
| `REPORT-01` | Report-card live workflow | `VERIFY` | Re-run score entry, grading, details preview, PDF generation, opening/printing, and parent publication without UI freezes. | Report totals never exceed 100, values display at 2 decimal places, and generated PDF data matches the database. |
| `REPORT-02` | Unified database grading scheme | `PARTIAL` | Remove or isolate remaining legacy hardcoded grading fallbacks and avoid synchronous database loading on the UI thread. | Desktop, web, preview, PDF, and parent portal produce identical grade codes and remarks from the active scheme. |
| `REPORT-03` | Print and branding audit | `PARTIAL` | Remove remaining hardcoded school identity from legacy forms/printouts and verify school logo/name/contact plus readable Nyansapo footer everywhere. | Every printout passes the branding checklist using current school profile data without source-code edits. |
| `REPORT-04` | Global two-decimal display audit | `PARTIAL` | Audit grids, cards, receipts, dashboards, reports, exports, and PDFs for numeric formatting. | Monetary and calculated decimal values consistently use 2 decimal places unless the field is intentionally an integer or percentage rule says otherwise. |
| `PERF-01` | Teacher performance publication workflow | `VERIFY` | Live-test exercises/homework entry, teacher submission, headmaster approval/rejection, upload/publication, and parent visibility. | Parents see only approved reports; edits and approval events are audited. |
| `REMARK-01` | Student remarks workflow | `VERIFY` | Live-test teacher remarks, attitude, conduct and interest submission plus headmaster review and report-card use. | Saved remarks reload correctly, follow permissions, and appear on the intended report card. |
| `TERM-01` | Web academic term management | `PARTIAL` | Add or complete web controls for create, activate, close, reopen, and reopening date. | Web and desktop use one active term and produce consistent report-card term dates. |
| `TIME-01` | Department timetable live verification | `VERIFY` | Generate all department classes, validate subject-teacher conflicts, class-teacher mode, breaks, period edits, and department PDF output against live data. | No teacher/class collision exists and the department PDF contains every expected class from silence hour through closing. |

### Web, PWA, Permissions, and Headmaster Experience

| ID | Feature | Status | Remaining work | Completion criteria |
|---|---|---|---|---|
| `HEAD-01` | Complete headmaster dashboard | `PARTIAL` | Replace mock Student/Staff tabs and wire review, approve, deny, navigation, and summary actions. | Every visible dashboard action works and uses live authorized data. |
| `PHOTO-01` | Web student and employee photo management | `NOT STARTED` | Add authorized upload, validation, preview, replacement, removal, and storage handling to administration pages. | Uploaded photos persist, reload, respect size/type rules, and display in relevant profiles. |
| `RBAC-01` | Complete role-visibility audit | `PARTIAL` | Inventory desktop controls, web components, sidebar links, direct routes, APIs, and database actions against centralized permissions. | Unauthorized actions are absent from UI/DOM and remain blocked through direct navigation/API calls. |
| `PWA-01` | Real-device PWA installation | `VERIFY` | Test deployed HTTPS manifest, service worker, icons, install prompt, installed-app listing, updates, and offline fallback on Android and iPhone. | Supported browsers install the app successfully and launch it in standalone mode. |
| `WEB-UI-01` | Responsive web visual audit | `PARTIAL` | Test sidebar widths, wrapping, dashboard cards, actions, tables, modals, and mobile navigation on representative viewports. | No text clipping, overlap, inaccessible action, or horizontal page overflow remains. |
| `STUDENT-PORTAL-01` | Dedicated student portal | `DEFERRED` | Product owner deferred this until after deployment. | Resume only after an explicit product decision. |

## Completed Baseline Features

The following items are not active backlog items unless a regression is reported:

- Active production data access uses SQL Server through `SqlClient`; Access/OleDb is not the production path.
- The web app has real admissions, finance, teacher, parent, headmaster, staff, security, and synchronization routes.
- Dynamic examination types, assessment numbers, and end-of-term handling exist.
- Parent fee balance breakdown distinguishes term fees, additional fees, prior debt, and payments where data is available.
- CI configuration builds desktop and web and runs automated tests.
- Per-school Paystack and MTN MoMo settings screens and server-side service structures exist.
- Desktop academic-session management supports term activation, closing, and reopening date.
- Department timetable generation and department PDF export exist in code.

## Update Procedure

After every task:

1. Update the relevant task row and `Last updated` date.
2. Do not change a task to `DONE` until its completion criteria pass.
3. Add an entry to the change log with files changed, tests run, and unresolved risks.
4. Refresh the verification snapshot when builds, tests, database records, or external integrations change.
5. Add newly discovered work with a stable task ID instead of burying it in the change log.
6. If a completed feature regresses, change it back to `PARTIAL` or `VERIFY` and record why.

## Per-Task Update Template

```text
Date:
Task ID:
Status before:
Status after:
Work completed:
Files changed:
Verification performed:
Result:
Remaining risk/blocker:
Next recommended task:
```

## Change Log

### 2026-07-19 - Reviewed Logical Migration Commits

- Split the accepted working snapshot into reviewed commits for repository/CI,
  SQL security operations, database recovery, plaintext-wrapper removal,
  desktop workflows, and web portal/PWA workflows.
- Current commit sequence: `e18ef9e`, `cf5a4d8`, `b78eb91`, `45a9a3b`,
  `224b45b`, and `5ccad17`.
- Rebuilt the desktop and web Release solutions with 0 warnings and 0 errors.
- Passed 54 desktop tests and 173 web non-SQL tests with 0 failures and 0 skips
  inside those selected gates.
- Passed the working-tree and full-history secret scan after the final web
  migration commit.
- The SQL integration category was not executed in the final fresh-clone gate
  because no protected test-master connection was supplied to that process.
- Deleted the sensitive pre-rewrite recovery bundle after accepting the fresh
  clone. The original checkout remains local recovery material and must not be
  pushed.

### 2026-07-19 - Fresh Clone and Reviewed Working-Tree Migration

- Created a fresh clone from the rewritten `fees-payment-ui-modernise` GitHub branch at commit `b0cc1f23513a`.
- Replayed four sanitized local commits from the clean history mirror; no pre-rewrite Git objects or recovery bundles were imported into the new clone.
- Migrated the remaining working-tree state as reviewed file snapshots: 178 tracked modifications, 45 tracked deletions, and 330 intentional new source, configuration, documentation, and asset files.
- Excluded 428 generated or local-only artifacts, including build outputs, temporary test projects, package caches, executables, local databases and backups, schema dumps, recovery data, and scratch conversion scripts.
- Preserved complete local inclusion and exclusion manifests under the fresh clone's ignored `.migration-review` directory.
- Passed the full-history and working-tree secret scan and `git fsck --full --strict` in the fresh clone.
- Built the desktop and web solutions with 0 warnings and 0 errors.
- Passed all 54 desktop tests and all 179 web tests with 0 failures and 0 skipped.
- Removed trailing whitespace and excess end-of-file blank lines from 277 migrated text files; `git diff --check` now passes without patch errors.
- Repeated both builds, both test suites, and the secret scan after cleanup with the same passing results.
- The original checkout remains a read-only recovery source and must not be pushed. Acceptance of the fresh clone permits removal of the sensitive pre-rewrite recovery bundle.

### 2026-07-19 - LAUNCH-SEC-01 Repository History Cleanup and Secret Scanning

- Rewrote the reachable `main` and `fees-payment-ui-modernise` GitHub histories using fingerprint-targeted replacements after the live credentials had been rotated.
- Protected both remote updates with explicit force-with-lease checks against the previously verified branch tips.
- Added a repository scanner for current files and all reachable Git blobs; findings expose only rule metadata and short SHA-256 fingerprints, never secret values.
- Added an all-branch GitHub Actions secret-scan workflow, a deterministic rewrite tool and redacted fingerprint manifest, local secret/certificate ignore rules, and a collaborator recovery runbook.
- Replaced the feature branch's hardcoded demonstration-user seed password with the required `NYANSAPO_SEED_USER_PASSWORD` environment variable.
- Freshly mirrored the rewritten GitHub repository, passed the complete history scan, passed `git fsck --full --strict`, and confirmed the scanner workflow exists on both rewritten branches.
- Rebuilt the active desktop solution with 0 warnings and 0 errors, passed the current-file secret scan, and passed all 54 desktop unit tests with 0 skipped.
- The existing working checkout was deliberately not reset because it contains extensive uncommitted project work and still references the pre-rewrite history. Do not push from it; move that work into a fresh clone using reviewed patches.
- Remaining launch work for this gate: deploy a CA-issued DNS certificate or a managed trust anchor for every client machine.

### 2026-07-18 - LAUNCH-SEC-01 Live Rotation and Trusted SQL Transport

- Bound the existing RSA 2048/SHA-256 server-authentication certificate for `localhost` to `MSSQLSERVER` and granted the SQL service read access to its private key.
- Added the certificate to this workstation's trusted root store, enabled SQL Server forced encryption, and restarted the service successfully.
- Rotated the `nyansapo_app` SQL login with a generated strong password that was never printed, logged, committed, or passed on a command line.
- Proved the retired credential was rejected before replacing the current-user DPAPI connection.
- Updated the DPAPI connection and active desktop/web defaults to `Encrypt=True;TrustServerCertificate=False`.
- Independently reopened `Neat_Academy` through the protected connection with certificate validation and confirmed 178 student rows.
- Added rollback-capable maintenance and read-only verification scripts plus non-secret evidence under `artifacts/maintenance`.
- Remaining launch work: repository-history cleanup and a CA-issued DNS certificate or managed trust-anchor distribution for clients on other machines.

### 2026-07-18 - LAUNCH-SEC-01 Application-Side Hardening

- Removed the live SQL credential from desktop tracked settings and enabled encrypted local defaults.
- Added centralized connection resolution through process environment, current-user DPAPI storage, and a password-free fallback.
- Added production fail-closed validation for SQL encryption and certificate verification.
- Added secure setup and integration-test prompts that avoid password output, transcripts, and command-line password arguments.
- Added automated tests for connection source priority, protected-file secrecy, and production transport rejection.
- Removed constructor-time database access from `StudentService`; scholarship calculation is now lazy and injectable, preventing unrelated unit tests and form construction from opening the live database.
- Migrated the current working desktop connection into current-user DPAPI storage and verified read-only access to `Neat_Academy` with 178 student rows.
- Sanitized six generated test configuration copies; the retired credential marker now has zero matches outside repository history.
- Verified the web build with 0 warnings/errors and all 179 web tests passing.
- Remaining launch gate after the completed local rotation: repository-history cleanup and multi-machine certificate trust/deployment.

### 2026-07-18 - Pre-Production Audit Added

- Added a comprehensive architecture, security, database, payment, performance, UX, accessibility, PWA, SEO, backup, monitoring, and deployment audit.
- Recorded launch readiness separately from feature maturity: commercial launch is not approved; a controlled pilot requires all P0 gates first.
- Added stable `LAUNCH-*` task IDs so each blocker can be updated and verified independently.
- Preserved the verified build/test baseline: desktop and web builds pass, web tests pass, and 24 desktop SQL integration tests remain skipped.

### 2026-07-18 - Tracker Created

- Consolidated outstanding requests from the development conversation.
- Reconciled older requests with the current route, service, sync, payment, report, timetable, permission, and test implementations.
- Recorded the latest known desktop and web verification results.
- Separated incomplete features from features that exist but still require live verification.
