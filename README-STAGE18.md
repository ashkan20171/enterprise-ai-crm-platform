# AshkanCRM Stage 18 — Productivity, Search & SQL Forecast

## Added
- Persistent SQL Server tasks and reminders
- Task create/complete/soft-delete with ownership enforcement
- SQL-backed CRM calendar
- Permission-aware global search across Customers, Leads and Deals
- SQL-backed revenue forecast and weighted pipeline
- Audit events for task lifecycle
- Existing Stage 17 lead/deal lifecycle and SQL notifications preserved
- Persian RTL default and English LTR coverage extended

## Upgrade behavior
`CrmDataBootstrapper` creates `CrmTasks` if missing, so existing Identity/CRM data does not need to be deleted.
