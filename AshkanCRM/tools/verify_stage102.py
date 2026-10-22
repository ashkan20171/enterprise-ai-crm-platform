from pathlib import Path
root = Path(__file__).resolve().parents[1]
users = (root / "Views/CRM/Users.cshtml").read_text(encoding="utf-8")
settings = (root / "Views/CRM/Settings.cshtml").read_text(encoding="utf-8")
layout = (root / "Views/Shared/_Layout.cshtml").read_text(encoding="utf-8")
assert 'Func<string, string> roleLabel' in users
assert '>@roleLabel(role)</option>' in users
assert 'string.Join("، ", user.Roles.Select(roleLabel))' in users
assert '@(fa ? "مدیریت سازمان" : "ADMINISTRATION")' in settings
assert 'stage101-i18n.js' in layout
print("Stage 102 localization structural checks passed")
