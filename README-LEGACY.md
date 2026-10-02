# AshkanCRM Stage 16 — Sales Execution & Razor Fix

- Fixed Customers.cshtml Razor parser errors caused by nested code block inside foreach.
- Pagination moved outside edit-dialog loop.
- SQL-backed Lead creation with server-side role/ownership enforcement.
- SQL-backed Deal creation with server-side role/ownership enforcement.
- Sales users only query their own Leads and Deals; Admin/SalesManager can see all.
- Anti-forgery tokens added to new persistent create/edit forms.
- Lead and Deal pages refreshed for bilingual/i18n-ready UI.
- Existing Stage 15 customer lifecycle, recycle bin, timeline, audit, Identity, admin, AI, governance, RTL/LTR features retained.
