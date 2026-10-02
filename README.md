# Enterprise AI-Ready CRM Platform

A portfolio-grade CRM built with ASP.NET Core, Entity Framework Core and SQL Server, focused on secure sales operations, team-scoped data access, automation, explainable intelligence and enterprise UX.

## Highlights
- ASP.NET Core MVC + EF Core + SQL Server
- ASP.NET Core Identity, RBAC and server-side authorization
- Own / Team / All data scopes and team ownership
- Customer, Lead, Deal and Task lifecycle management
- Drag-and-drop sales pipeline, forecast, targets and Win/Loss analytics
- SQL-backed notifications, audit trail and activity timelines
- Recurring tasks, reminders, duplicate detection and global search
- Explainable AI-ready sales intelligence, customer health/risk and next-best-action signals
- Lead routing with auditable rules
- Integration Hub, API-key concepts and webhook monitoring
- Login audit, rate limiting, security headers, health endpoint and data governance
- Persian RTL / English LTR UI with responsive layouts

## Architecture
The solution separates authentication/authorization, persistent CRM entities, SQL bootstrap/schema compatibility, application services and MVC presentation. Data-scope checks are enforced server-side rather than relying on UI hiding.

See [ARCHITECTURE.md](ARCHITECTURE.md) and [SECURITY.md](SECURITY.md).

## Engineering Decisions
- Existing databases are upgraded defensively instead of being dropped.
- Sensitive actions use role and scope checks on the server.
- AI features are explainable and provider-ready; no external API secret is hard-coded.
- Auditability is treated as a first-class requirement for routing, lifecycle and security operations.
- The UI supports RTL/LTR and responsive enterprise workflows.

## Getting Started
1. Install a compatible .NET SDK and SQL Server.
2. Update the connection string in `AshkanCRM/appsettings.json` for your environment.
3. Restore NuGet packages.
4. Build and run the `AshkanCRM` project.
5. Use development/demo credentials only in a local environment and replace them before deployment.

## Portfolio Review Path
For a quick technical review, start with:
- `AshkanCRM/Program.cs`
- `AshkanCRM/Data/CrmDbContext.cs`
- `AshkanCRM/Data/CrmDataBootstrapper.cs`
- `AshkanCRM/Controllers/CRMController.cs`
- `AshkanCRM/Models/PersistentCrmEntities.cs`
- `AshkanCRM/Views/Shared/_Layout.cshtml`
- `AshkanCRM/wwwroot/css/crm.css`

## Security Note
This repository is intended as a portfolio/reference implementation. Review secrets, database credentials, demo accounts, CORS, proxy headers, rate limits, logging and deployment configuration before production use.

## Roadmap
- Provider-backed LLM integration with tool calling
- Vector/embedding-based knowledge retrieval
- SignalR real-time event stream
- Automated integration and security tests
- Docker/CI pipeline and deployment profiles
- OpenTelemetry-based observability

## License
Choose and add an appropriate license before public distribution.
"# enterprise-ai-crm-platform" 
