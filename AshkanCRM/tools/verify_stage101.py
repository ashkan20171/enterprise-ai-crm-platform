from pathlib import Path
root = Path(__file__).resolve().parents[1]
customers = (root/'Views/CRM/Customers.cshtml').read_text()
deals = (root/'Views/CRM/Deals.cshtml').read_text()
layout = (root/'Views/Shared/_Layout.cshtml').read_text()
assert 'data-stage101-en="Active"' in customers
assert 'value="Enterprise"' in customers
assert 'string.IsNullOrEmpty(x.Name)' in customers
assert 'stage == "Qualification"' in deals
assert 'st == "Qualification"' in deals
assert 'stage101-i18n.js' in layout
print('Stage 101 structural checks passed')
