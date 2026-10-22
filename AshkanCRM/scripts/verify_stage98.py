from pathlib import Path
root = Path(__file__).resolve().parents[1]
bootstrap = (root / "Data" / "CrmDataBootstrapper.cs").read_text(encoding="utf-8")
controller = (root / "Controllers" / "CRMController.cs").read_text(encoding="utf-8")
view = (root / "Views" / "CRM" / "Dashboard.cshtml").read_text(encoding="utf-8")
assert "DEFAULT(NCHAR(123)+NCHAR(125))" in bootstrap
assert "DEFAULT(N'{}')" not in bootstrap
assert "OverdueScheduledDeals" in controller and "OverdueScheduledDeals" in view
assert "planned close date has passed" in view
print("Stage 98 static checks passed")
