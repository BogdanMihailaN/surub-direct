# Șurub Direct — working agreements

- Keep all backend source in `backend/src` and tests in `backend/tests`; frontend source in `frontend/src` and tests in `frontend/tests`. Services, gateway and backend shared libraries belong under `backend/src`. Angular apps belong under `frontend/src/apps`; do not add redundant per-service/per-app src layers.

- Use independently runnable services behind YARP: Catalog (including Pricing), Inventory, Identity, Purchasing (suppliers and SKU purchasing terms), and the Ordering host scaffold. The user's microservices decision supersedes the former modular-monolith design.
- Keep Clean Architecture inside each business service. Domain must not reference EF Core, ASP.NET Core or Infrastructure. Application must not reference Infrastructure.
- A service must never reference another service's projects or access another service's database. Consumer-owned ports and HTTP DTOs define integration boundaries. Contracts projects are local to their owning service.
- Shared libraries contain only technical primitives and authentication/HTTP plumbing, never shared business entities or persistence.
- The gateway contains routing and infrastructure policies, not database access or business operations. It must not expose `/internal` routes.
- All administrative mutations require authentication, Administrator role and antiforgery validation, including direct calls to service APIs. Identity owns the session cookie and CSRF validation; service clients disable automatic cookie handling and validate every administrative request.
- Never check in `.env`, credentials, runtime data, backups or `.tools`. Give every service its own database credential. Preserve the legacy database until its owner explicitly approves removal.
- Inventory owns stock. Receipts are immutable and idempotent; retries must not increment a balance twice. An unavailable dependency must not be represented as zero stock or a missing SKU.
- All demo merchandise, prices and images are illustrative. Do not enable checkout or remove noindex until commercial data and launch requirements are addressed.
- Run `python3 scripts/dev.py test` for backend changes, Angular production builds for frontend changes, and affected Playwright flows for changes to the catalog/admin journey. Docker is needed for integration tests.
- Keep one primary C# type per authored file, one handler per use case, one endpoint per file, separate EF configurations and small composition-only Program.cs files.
- Angular components use colocated TypeScript, HTML and SCSS. Feature pages compose components; API access and state live in data-access/state. Keep shared code restricted to actual reuse.
- Keep strict TypeScript/template checking enabled. Organize tests into Unit, Integration, Architecture and browser scenarios.
- See `docs/file-organization.md`, `docs/architecture.md` and `docs/implementation-status.md`. Archived documents describe past decisions, not the current runtime.
