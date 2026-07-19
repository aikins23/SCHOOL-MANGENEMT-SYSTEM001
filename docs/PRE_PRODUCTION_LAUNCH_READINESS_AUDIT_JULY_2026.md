# Pre-Production Launch Readiness Audit - July 2026

Audit date: 2026-07-18
Project: Nyansapo ERP School Management System
Scope: Desktop WinForms, Blazor web, SQL Server, synchronization, payments, PWA, accessibility, SEO, deployment, and operations

> Status update (2026-07-19): this remains the evidence snapshot from the audit
> date. The exposed SQL credential was rotated, removed from reachable Git
> history, and protected by automated secret scanning. SQL forced encryption
> and certificate validation are active on the current workstation. The other
> launch blockers remain open unless the living
> `OUTSTANDING_FEATURES_TRACKER.md` records newer verification.

## 1. Executive Summary

### Overall assessment

**Commercial production readiness: 4.8/10 - NOT READY FOR UNCONTROLLED COMMERCIAL LAUNCH**

**Controlled single-school pilot readiness: 6.5/10**, provided the P0 security, schema, backup, and workflow gates below are closed first.

This rating is intentionally different from the codebase or feature rating of approximately 7.5/10. The system is broad, ambitious, and substantially implemented, but production readiness measures whether failures, security boundaries, payments, recovery, and operations are proven under real deployment conditions.

### Verified strengths

- Desktop Release build passed with 0 errors and 0 warnings.
- Web build passed with 0 errors and 0 warnings.
- Web automated tests passed: 179 passed, 0 failed.
- Desktop automated tests completed with 51 passed, 0 failed, and 24 SQL integration tests skipped.
- NuGet vulnerability scans reported no known vulnerable packages.
- Authentication uses PBKDF2-HMAC-SHA256 with a strong current iteration count.
- Web cookie settings use HttpOnly, Secure, and SameSite Strict.
- Role and policy authorization is present across the sensitive Razor page routes reviewed.
- The desktop backup service performs real SQL Server backup and restore operations.
- The synchronization code has device credentials, fixed-time key comparison, and a table allowlist.
- CI exists and builds both applications.

### Top launch blockers

1. **A live SQL credential is committed in configuration.** It must be rotated, removed from source and history, and replaced with deployment secrets.
2. **Security middleware, deployment validation, sync endpoints, and payment webhooks exist but are not activated in the web startup pipeline.** Controls that are not registered do not protect production.
3. **Payment provider secrets are stored in plaintext in the database, and webhook posting does not yet enforce a strict existing-intent transaction boundary.**
4. **Multi-tenant isolation is not enforced by database keys, mandatory school scope, or EF global query filters.** This blocks a hosted multi-school launch.
5. **Application code performs runtime CREATE/ALTER/INDEX work.** Schema changes need a versioned deployment process and a least-privileged application login.
6. **Twenty-four desktop SQL integration tests are skipped, and the real desktop workflow smoke pass is not complete.** Build success is not enough to prove login, finance, grading, report card, promotion, and timetable behavior.
7. **Backup recovery, centralized monitoring, and health checks are not production-proven.** A backup is useful only after a restore drill proves the recovery process.
8. **Private portal data can be indexed or cached too broadly.** The authenticated portal currently uses public SEO directives, broad service-worker caching, and shared localStorage profile keys.

### Quick wins

- Rotate the exposed database password and remove it from tracked configuration.
- Call the existing deployment validator and security middleware from `Program.cs`.
- Map the existing sync and Paystack webhook endpoints deliberately, with tests.
- Set authenticated portal pages to `noindex, nofollow` and remove private routes from the sitemap.
- Replace crash-log overwrite behavior with rolling structured logging.
- Make the release CI job fail when SQL integration tests are skipped.
- Remove unused source maps, duplicate framework assets, and oversized noncritical images from web publishing.

## 2. Detailed Findings

## Architecture, Security, and Performance

### `LAUNCH-SEC-01` - Committed database credential and disabled transport protection

- **Severity:** Critical - launch blocker
- **Found:** `App.config` contains a plaintext SQL login credential and uses `Encrypt=False;TrustServerCertificate=True`. The web configuration also disables SQL encryption for its local connection.
- **Why it matters:** Anyone with repository or package access may obtain database credentials. Unencrypted database traffic can expose credentials and school data in transit.
- **Actions:** Rotate the credential immediately; remove it from tracked files and repository history; inject secrets from environment variables, DPAPI, or an approved secret store; issue and trust a SQL Server certificate; require `Encrypt=True`; use separate least-privileged desktop, web, migration, backup, and test identities.

### `LAUNCH-SEC-02` - Existing security controls are not wired into startup

- **Severity:** Critical - launch blocker
- **Found:** `SecurityMiddlewareExtensions`, `DeploymentConfigurationValidator`, sync endpoint mapping, Paystack webhook mapping, and sync request-size controls exist, but `web/KingdomPrep.Web/Program.cs` does not activate them.
- **Why it matters:** Dead security code creates false confidence. Production can start with unsafe settings while required API endpoints remain absent or unprotected.
- **Actions:** Call deployment validation before serving requests; register security headers in the correct middleware order; configure request limits; explicitly map and authorize sync and webhook routes; add startup tests that fail if required endpoints or headers are missing; protect logout with antiforgery instead of disabling it.

### `LAUNCH-SEC-03` - Legacy plaintext password mode remains enabled

- **Severity:** High - launch blocker
- **Found:** `Security:AllowLegacyPlainTextPasswords` is enabled in web configuration. A validator exists to reject this for production but is not currently called.
- **Why it matters:** Legacy plaintext comparison weakens credential protection and can silently survive into deployment.
- **Actions:** Run a forced migration of legacy accounts to the current PBKDF2 format; disable plaintext mode in every non-development environment; activate startup validation; log and alert on any remaining legacy hash upgrade.

### `LAUNCH-PAY-01` - Payment secrets are stored as plaintext database fields

- **Severity:** Critical - launch blocker for online payments
- **Found:** `PaymentGatewaySettingsService` persists Paystack and MTN MoMo secret material directly in `SchoolPaymentSettings`.
- **Why it matters:** A read-only database compromise becomes a payment-provider compromise. Broad retrieval of school secret candidates increases the blast radius.
- **Actions:** Encrypt secrets using ASP.NET Core Data Protection with a durable protected key ring or use an external vault; restrict decryption to the payment service; add key rotation and audit events; never return secrets to UI models after saving; apply database least privilege.

### `LAUNCH-PAY-02` - Webhook verification and ledger posting are not fully atomic

- **Severity:** Critical - launch blocker for online payments
- **Found:** HMAC validation uses fixed-time comparison, which is good. However, the webhook can continue without an existing payment intent, does not fully validate status/currency/metadata, and posts payment separately from intent completion.
- **Why it matters:** A replay, forged payload path, duplicate delivery, or partial failure can produce an incorrect or duplicated financial ledger entry.
- **Actions:** Require an existing pending and unexpired intent; compare school, student, gateway, reference, amount, currency, and customer metadata exactly; require a successful provider status; perform ledger post and intent completion in one transaction; enforce a unique provider-reference constraint; return idempotent success for already completed valid events.

### `LAUNCH-TENANT-01` - Tenant isolation is convention-based rather than enforced

- **Severity:** Critical - launch blocker for multi-school SaaS
- **Found:** `SchoolId` is nullable in important paths, repositories can accept a null school scope, `AppDbContext` has no global tenant filters, and several primary or unique keys are global rather than school-scoped.
- **Why it matters:** A missed filter can expose or modify another school's students, users, payments, or reports.
- **Actions:** Make school scope mandatory for tenant-owned records; introduce a request tenant context; add global query filters or a repository guard; use school-scoped composite unique keys; remove null-to-all fallbacks; add cross-school read/write/delete isolation tests; use database row-level security if the hosting model warrants it.

### `LAUNCH-DB-01` - Runtime schema mutation is spread across application services

- **Severity:** High - launch blocker
- **Found:** Desktop repositories and web services execute `CREATE TABLE`, `ALTER TABLE`, and index creation during normal application operation.
- **Why it matters:** Concurrent startups can race, partial DDL can leave inconsistent schemas, and the application login needs excessive database permission.
- **Actions:** Move all DDL into ordered, idempotent, versioned deployment scripts or migrations; maintain a schema version table; execute migrations with a separate privileged identity; make application startup fail clearly when the schema version is unsupported; remove DDL rights from normal runtime accounts.

### `LAUNCH-DB-02` - Database relationships and integrity rules are incomplete

- **Severity:** High
- **Found:** The EF model configures entities and some keys but few explicit relationships or foreign keys. Tenant scope, payment idempotency, money precision, and concurrency are not uniformly database-enforced.
- **Why it matters:** Application validation alone cannot prevent orphan records, cross-school references, duplicate postings, or lost updates.
- **Actions:** Add foreign keys and delete rules; add check constraints for valid marks and nonnegative payment values where appropriate; standardize money as `decimal(18,2)`; add unique transaction references; add row-version or equivalent concurrency columns; validate every `StudentID` relationship in a schema audit.

### `LAUNCH-PERF-01` - Desktop UI freezing risks remain

- **Severity:** High - launch blocker until smoke-tested
- **Found:** Shared helpers still use sync-over-async (`GetAwaiter().GetResult()`), fire-and-forget tasks remain, and large forms mix UI, SQL, PDF, SMS, and business logic. Known large files include fee payment, timetable, dashboard, payment history, and authentication code.
- **Why it matters:** A production desktop app that appears frozen causes repeated clicks, duplicate transactions, interrupted work, and loss of staff confidence.
- **Actions:** Use true asynchronous ADO.NET calls; keep `async void` only at event boundaries; await every important operation; use cancellation, command timeouts, progress, and double-click guards; move PDF/SMS/sync work to observed background services; paginate grids; split large forms into presenters/services; complete the real UI workflow matrix.

### `LAUNCH-PERF-02` - Web scaling and query capacity are not proven

- **Severity:** High for hosted launch
- **Found:** Blazor Interactive Server holds circuits on the server. No load-test evidence, distributed login throttle, SignalR scale-out plan, response compression, or query profiling evidence was found.
- **Why it matters:** Concurrent schools, parents, and teachers can exhaust server memory, database connections, or circuit capacity.
- **Actions:** Define expected concurrent users; run load tests for login, dashboard, parent fees, grading, and SignalR reconnect; add pagination and query indexes from measured plans; enable safe response compression and static caching; use sticky sessions or an appropriate SignalR backplane for multiple instances; replace in-memory throttling with distributed IP-plus-username protection.

### `LAUNCH-OBS-01` - Monitoring, diagnostics, and health checks are insufficient

- **Severity:** High - launch blocker
- **Found:** Desktop crash handlers overwrite `crash.log`; logging is primarily local; no production health endpoints, centralized telemetry, metrics, alerting, or correlation strategy is activated.
- **Why it matters:** Failures can remain invisible until a school reports them, and support cannot reconstruct a transaction or request safely.
- **Actions:** Add structured rolling logs with redaction and correlation IDs; centralize web logs and critical desktop diagnostics; add `/health/live` and `/health/ready` checks for database, schema version, and required providers; add payment, sync, login-failure, SMS-outbox, and backup alerts; define retention and incident ownership.

### `LAUNCH-DR-01` - Backup exists but disaster recovery is not proven

- **Severity:** Critical - launch blocker
- **Found:** SQL backup/restore is implemented, with managed-folder checks. Encryption, off-machine copies, checksum/verification, independent scheduling, retention, and a recorded restore drill are not proven.
- **Why it matters:** A local backup can be lost with the machine, silently corrupt, or fail because the SQL Server service cannot access the selected path.
- **Actions:** Run backups with `CHECKSUM`; run `RESTORE VERIFYONLY`; copy encrypted backups off-machine; schedule independently of the desktop app; define retention, RPO, and RTO; perform and document a full restore drill to an isolated server; restrict `WITH REPLACE` restore to an audited privileged role.

### `LAUNCH-QA-01` - Release verification permits skipped integration tests

- **Severity:** High - launch blocker
- **Found:** Web tests pass, but 24 desktop SQL integration tests are skipped because the test master connection is unavailable. CI can pass without proving them.
- **Why it matters:** Database-dependent admissions, fees, grading, reports, promotion, and timetable behavior may regress while the pipeline remains green.
- **Actions:** Provision an isolated disposable SQL test database in CI; require 0 skipped integration tests on release branches; publish test and coverage artifacts; add schema migration validation; complete the desktop UI smoke checklist; add payment and cross-tenant security tests.

## User Interface, Accessibility, and Workflow

### `LAUNCH-UX-01` - Desktop visual responsiveness is not consistently proven

- **Severity:** High
- **Found:** Previous defects include clipped dashboard panels, split-container failures, broken scroll areas, wrapped table content, and controls that move or disappear at different sizes.
- **Why it matters:** Staff can miss financial data or actions, especially on smaller screens and Windows display scaling.
- **Actions:** Define supported minimum size; test every primary form at 100%, 125%, and 150% DPI; use stable layout panels instead of absolute coordinates; set minimum sizes in valid order; add wrapping and tooltips only where necessary; capture a screenshot checklist for each role.

### `LAUNCH-UX-02` - Web mobile and cross-browser verification is incomplete

- **Severity:** High for public parent access
- **Found:** Responsive CSS exists, but prior screenshots show sidebar and card clipping. No complete browser/device evidence covers Android Chrome, iPhone Safari, Edge, and common viewport sizes.
- **Why it matters:** Parent access is primarily mobile, and a clipped navigation or payment action makes the service unusable.
- **Actions:** Test at 360, 390, 768, 1024, and 1440 pixel widths; test Android Chrome, iOS Safari, and desktop Edge/Chrome; verify sidebar, dialogs, tables, install flow, orientation, keyboard opening, and long names/amounts; add Playwright visual regression tests.

### `LAUNCH-A11Y-01` - WCAG compliance is not demonstrated

- **Severity:** High
- **Found:** Some interactive UI uses clickable `div` or `label` elements rather than semantic buttons, icon-only controls rely on `title`, and form/table labeling is inconsistent. No axe, keyboard, screen-reader, or contrast report is recorded.
- **Why it matters:** Keyboard, screen-reader, low-vision, and motor-impaired users may be unable to complete essential tasks.
- **Actions:** Target WCAG 2.2 AA; replace clickable containers with buttons/links; add accessible names and focus states; connect labels with inputs; add table captions and header scope; announce loading and validation status; test keyboard-only flows; run axe and manual NVDA/VoiceOver checks; verify contrast at normal, hover, focus, disabled, and error states.

### `LAUNCH-UX-03` - Error and loading behavior is inconsistent

- **Severity:** High
- **Found:** Busy states exist in several improved workflows, but raw SQL errors, modal blocking, generic refresh failures, and unobserved background tasks have appeared across the desktop system.
- **Why it matters:** Raw errors expose implementation details and do not tell staff whether an operation completed. Repeated clicks can duplicate work.
- **Actions:** Standardize error envelopes and user messages; log technical details with a support code; disable repeated submission; use inline progress for long operations; distinguish retryable network errors from validation failures; preserve entered data after failure; audit every primary button handler.

### `LAUNCH-UX-04` - PWA offline behavior is easy to overstate

- **Severity:** Medium to High
- **Found:** The service worker provides an installable shell and fallback, but Blazor Server workflows still require a live circuit. The worker can cache broad same-origin GET responses after first use.
- **Why it matters:** Users may believe fee, grading, or parent workflows work offline when only the shell does. Broad caching can retain private content on shared devices.
- **Actions:** Describe the current capability as installable with offline shell only; allowlist immutable public assets; exclude APIs, authenticated HTML, profile images, downloads, and private data; clear user-specific caches on logout; use explicit cache versions; add real client-side storage and synchronization only for workflows intentionally designed for offline operation.

### `LAUNCH-PRIVACY-01` - Shared browser storage can retain user profile data

- **Severity:** High
- **Found:** Username, email, phone, and avatar data are stored in global localStorage keys rather than a tenant-and-user namespace and are not clearly cleared during logout.
- **Why it matters:** On a shared device, the next user can inherit another person's profile data.
- **Actions:** Keep profile data server-side where possible; otherwise namespace it by school and user, minimize stored fields, clear it on logout/account switch, and document retention; never store authentication secrets or payment data in localStorage.

## SEO, Web Visibility, and Front-End Delivery

### `LAUNCH-SEO-01` - Authenticated portal pages are configured for indexing

- **Severity:** High privacy risk
- **Found:** Global metadata uses `index, follow`, `robots.txt` allows most portal routes, and the sitemap includes login/portal locations. The Open Graph URL is hardcoded and no consistent canonical strategy exists.
- **Why it matters:** Search engines should not index private school operational pages or expose route patterns and stale login results.
- **Actions:** Apply `noindex, nofollow, noarchive` to authenticated and login pages; disallow private route families in `robots.txt`; remove private routes from the sitemap; return proper 401/403 behavior; if marketing visibility is required, create a separate public anonymous site with its own metadata, canonical URLs, sitemap, and structured data.

### `LAUNCH-SEO-02` - Public marketing SEO is incomplete

- **Severity:** Low for a private portal, Medium if public acquisition is required
- **Found:** No verified JSON-LD, page-specific canonical metadata, social preview image, or public information architecture was found. Some metadata contains encoding artifacts.
- **Why it matters:** A future public product site would have weak search snippets and inconsistent sharing previews.
- **Actions:** Separate marketing from the authenticated ERP; add unique titles/descriptions, canonical links, Open Graph/Twitter metadata, Organization/SoftwareApplication structured data, heading hierarchy, alt text, and valid encoded text only on public pages.

### `LAUNCH-WEBPERF-01` - Static payload and cache policy need production optimization

- **Severity:** Medium
- **Found:** Large images, source maps, duplicate Bootstrap variants, and other noncritical assets are present in `wwwroot`. The service worker caches broadly, while response compression and measured Core Web Vitals evidence are absent.
- **Why it matters:** Mobile users on slower Ghanaian networks can experience delayed first load, high data use, and stale assets.
- **Actions:** Optimize raster images; publish only used minified assets; remove source maps and unused RTL/unminified framework files from production; self-host required libraries; use hashed filenames and long immutable cache headers; enable response compression; measure LCP, INP, and CLS against the deployed HTTPS build.

### `LAUNCH-CSP-01` - Content Security Policy is both inactive and too permissive

- **Severity:** High
- **Found:** Security-header middleware is not active. Its current CSP allows `unsafe-inline` and `unsafe-eval`, while payment/CDN dependencies require an explicit tested policy.
- **Why it matters:** Without CSP, script injection has fewer barriers. Activating an untested policy later can break Blazor, charts, or checkout.
- **Actions:** Activate CSP in staging; self-host static libraries where possible; use nonces or hashes for inline code; remove `unsafe-eval`; allow only exact Paystack and SignalR endpoints; add CSP reporting; run payment, chart, PWA, and reconnect tests before production enforcement.

### Core Web Vitals status

- **Status:** NOT TESTED
- **Reason:** No deployed production-equivalent URL and Lighthouse/WebPageTest evidence were part of this audit.
- **Required evidence:** Mobile and desktop Lighthouse traces on the final HTTPS deployment, with authenticated dashboard and parent-fee journey timing captured separately from any public landing page.

## 3. Prioritized Launch Checklist

| Area | Status | Rating | Launch requirement |
|---|---|---:|---|
| Desktop and web compile | PASS | 10/10 | Preserve 0 errors and 0 warnings. |
| Web automated tests | PASS | 9/10 | Keep 179 passing and add security/browser coverage. |
| Desktop automated tests | FAIL | 6/10 | Run all SQL tests with 0 skipped and 0 failed. |
| Dependency vulnerability scan | PASS | 8/10 | Automate scan and review transitive/native dependencies. |
| Secret management | FAIL | 2/10 | Rotate exposed SQL credential and remove all production secrets from source. |
| SQL transport security | FAIL | 2/10 | Require encrypted, certificate-validated connections. |
| Authentication | PARTIAL | 7/10 | Disable legacy plaintext mode and distribute throttling. |
| Route authorization | PASS/PARTIAL | 8/10 | Keep role policies; add direct API and cross-role negative tests. |
| Multi-tenant isolation | FAIL | 3/10 | Enforce mandatory school scope in code and database. |
| Payment security | FAIL | 4/10 | Encrypt secrets and prove atomic verified webhook posting. |
| Schema deployment | FAIL | 3/10 | Remove runtime DDL and use versioned deployment migrations. |
| Data integrity | PARTIAL | 5/10 | Add FK, scope, idempotency, precision, and concurrency constraints. |
| Desktop responsiveness | PARTIAL | 5/10 | Complete freeze and DPI smoke matrices. |
| Web responsiveness | PARTIAL | 6/10 | Complete device/browser and visual regression checks. |
| Accessibility | FAIL/NOT PROVEN | 3/10 | Pass WCAG 2.2 AA automated and manual checks. |
| PWA installation | PARTIAL | 6/10 | Verify real devices and narrow private caching. |
| Backup and restore | PARTIAL | 5/10 | Complete encrypted offsite backup and restore drill. |
| Monitoring and health | FAIL | 3/10 | Add centralized observability, readiness checks, and alerts. |
| CI release gate | PARTIAL | 6/10 | Fail releases on skipped integration/security checks. |
| Private portal SEO/privacy | FAIL | 3/10 | Noindex private pages and remove them from sitemap. |
| Public SEO | NOT APPLICABLE/PARTIAL | 4/10 | Build separately only if a public marketing site is required. |
| Core Web Vitals | NOT TESTED | 0/10 | Measure final deployed HTTPS build. |
| Deployment/runbook | PARTIAL | 5/10 | Add environment, migration, rollback, incident, and recovery runbooks. |

## 4. Technical Deep-Dive

### Recommended production request boundary

```text
Authenticated request
  -> resolve mandatory School/Tenant context
  -> apply role/policy authorization
  -> validate command/query DTO
  -> application service transaction
  -> tenant-guarded repository
  -> SQL Server constraints and row version
  -> structured audit event
```

No tenant-owned repository should accept a missing school scope. UI visibility is helpful, but authorization must remain enforced at the route/API and service boundaries.

### Recommended database deployment boundary

```text
Deployment pipeline (privileged migration identity)
  -> backup and verify
  -> validate current schema version
  -> apply ordered transaction-safe scripts
  -> record schema version
  -> smoke test
  -> start application with DDL-denied runtime identity
```

Application startup should validate compatibility, not repair production schema opportunistically.

### Recommended payment transaction boundary

```text
Create pending intent
  -> provider checkout/request-to-pay
  -> signed provider callback/webhook
  -> load exact pending intent by school + reference
  -> verify status, amount, currency, student, gateway, and metadata
  -> one SQL transaction:
       insert unique payment ledger entry
       mark intent completed
       append audit event
  -> idempotent provider response
```

Unknown references must never post money. Duplicate valid callbacks should return success without adding another ledger row.

### Required pre-launch test matrix

1. Desktop roles: administrator, accountant, headmaster, teacher, admissions, and restricted user.
2. Desktop workflows: login, student creation, admission approval, fee payment, additional fee approval, notice/SMS queue, grading, report card, promotion, timetable, backup, and restore.
3. Web roles: parent, teacher, headmaster, finance, staff, administrator, and denied user.
4. Security: direct route/API access, cross-school IDs, duplicate payment callbacks, expired intent, invalid signatures, invalid file uploads, CSRF, brute force, and logout.
5. Offline/sync: disconnect, restart, retry, duplicate delivery, simultaneous edits, tombstone/delete, and school isolation.
6. UI: desktop 100/125/150% DPI and web 360/390/768/1024/1440 widths.
7. Browsers/devices: Chrome Android, Safari iPhone, Edge Windows, Chrome Windows, installed PWA, and update flow.
8. Operations: database unavailable, provider timeout, disk full, expired certificate, backup failure, restore drill, and rollback.

### Launch sequence

#### P0 - Before any pilot data is trusted

1. Close `LAUNCH-SEC-01`, `LAUNCH-SEC-02`, and `LAUNCH-SEC-03`.
2. Close payment secret and webhook risks or disable online payments for the pilot.
3. Move runtime DDL to versioned deployment scripts.
4. Complete a verified backup and restore drill.
5. Run all desktop SQL tests without skips.
6. Complete the critical desktop and web role smoke passes.

#### P1 - Before hosted commercial launch

1. Enforce tenant isolation in code and database.
2. Add centralized logs, health checks, and alerts.
3. Complete accessibility, mobile, browser, and DPI verification.
4. Complete payment provider production-like verification.
5. Complete sync conflict and cross-school tests if offline sync is sold as a launch feature.
6. Run load and connection-capacity tests.

#### P2 - Immediately after launch stabilization

1. Refactor the largest desktop forms and remove remaining sync-over-async work.
2. Add SAST, secret scanning, dependency automation, coverage thresholds, and signed artifacts.
3. Optimize static web delivery and measure Core Web Vitals continuously.
4. Separate any public marketing/SEO experience from the authenticated ERP portal.

## Final Verdict

Nyansapo ERP is feature-rich and has a credible technical foundation, but the present evidence does not support an uncontrolled production launch or multi-school SaaS sale. The immediate problem is not a lack of features. It is that credential protection, startup security wiring, payment atomicity, tenant isolation, schema governance, integration coverage, recovery, and observability are not all enforced and proven.

The safest next release is a controlled single-school pilot only after the P0 gates pass, with online payments disabled until payment security and provider verification are complete. A multi-school commercial launch should wait until the P1 tenant, monitoring, accessibility, browser, and capacity gates are complete.
