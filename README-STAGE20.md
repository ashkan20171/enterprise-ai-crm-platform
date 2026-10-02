# AshkanCRM — Stage 20

Stage 20 moves the CRM from role-only authorization toward enterprise data scoping and productivity automation.

## Added
- Persistent SQL teams and team memberships.
- Independent `Own / Team / All` data scopes.
- Server-side scope enforcement for Customers, Leads, Deals, Tasks, Global Search and AI-assisted sales views.
- Team-aware ownership fields on core sales entities.
- Admin Team Workspace to create teams and assign user scope.
- Recurring tasks (Daily / Weekly / Monthly) that create the next occurrence when completed.
- Reminder processing that emits persistent SQL notifications once per due reminder.
- Win/Loss Analytics based on closed SQL deals.
- Duplicate Detection for customers and leads.
- Permission-aware CSV export for customers, leads and deals.
- Next Best Action workspace driven by accessible SQL pipeline data.
- SQL-backed Customer 360 route repaired and scope-aware.
- Added a seeded Sales representative account for testing Own vs Team scope.

## Demo accounts
- Admin: admin@ashkancrm.local / Admin123!
- Sales Manager: sales@ashkancrm.local / Sales123!
- Sales Representative: salesrep@ashkancrm.local / SalesRep123!

## Database upgrade
`CrmDataBootstrapper` adds new columns/tables with guarded `COL_LENGTH` / `OBJECT_ID` checks, preserving existing Identity and CRM data.

## Verification
The environment used to package this stage does not contain the .NET SDK, so a real `dotnet build` could not be executed. Static checks were performed for Razor model directives, duplicate controller actions, brace balance, route/view presence, and ZIP integrity.
