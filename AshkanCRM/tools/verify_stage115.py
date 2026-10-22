from pathlib import Path
root=Path(__file__).resolve().parents[1]
for name in ('IntegrationMonitor','Sessions'):
    s=(root/'Views'/'CRM'/f'{name}.cshtml').read_text(encoding='utf-8')
    assert 'CultureInfo.CurrentUICulture' in s
    assert 'Model != null && Model.Any()' in s
    assert 'en ?' in s
assert 'Revoke</button>' not in (root/'Views/CRM/Sessions.cshtml').read_text()
print('Stage 115 structural checks passed')
