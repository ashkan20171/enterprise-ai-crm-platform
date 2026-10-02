# Architecture

## Core layers
- **Presentation:** ASP.NET Core MVC, Razor views, responsive bilingual shell.
- **Application:** controller workflows and CRM services.
- **Identity & Authorization:** ASP.NET Core Identity, roles and server-side data scopes.
- **Persistence:** EF Core + SQL Server persistent CRM entities.
- **Compatibility bootstrap:** additive schema initialization for existing databases.

## Data access model
Authorization answers *what a user may do*. Data scope answers *which records the user may operate on* (Own / Team / All). The two concerns are intentionally separated.

## Main domains
Customer management, lead lifecycle, opportunity pipeline, tasks/reminders, activities, notifications, forecasting, targets, analytics, security operations, governance and integrations.

## AI-ready design
Intelligence features are deterministic/explainable by default and designed so an external model provider can later be introduced without moving authorization decisions into the model.
