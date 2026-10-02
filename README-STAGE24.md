# AshkanCRM Stage 24 — AI Operations, Security & Governance

## Added
- Scope-aware explainable AI Operations assistant (local, no external key required)
- Customer Intelligence: health score, risk, open pipeline, next-best-action
- Auditable deterministic Lead Routing rules
- Login audit telemetry (IP, user-agent, success/failure/result)
- Fixed-window rate limiting on login
- `/health` database health endpoint
- Correlation ID and hardened security headers
- Data Governance dashboard and read-only JSON backup export
- Responsive intelligence cards, mobile tables/forms, reduced-motion accessibility
- SQL-safe bootstrap upgrades for `CrmLoginAudits` and `CrmLeadRoutingRules`

## Important
AI in this stage is an explainable local intelligence layer. It does not claim to call an external LLM. The architecture can later be extended with an external provider using secrets from configuration/user-secrets/environment variables.

## Database upgrade
The existing bootstrapper adds new tables only when missing. It does not drop existing CRM or Identity tables.

## Run
Restore NuGet packages, Clean, then Rebuild in Visual Studio. This environment does not provide the .NET SDK, so a real `dotnet build` could not be executed here.
