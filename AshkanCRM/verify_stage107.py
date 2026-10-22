from pathlib import Path
root = Path(__file__).resolve().parent
legacy = (root/'wwwroot/js/stage57-culture.js').read_text()
authority = (root/'wwwroot/js/stage105-culture-switch.js').read_text()
assert '.AspNetCore.Culture' not in legacy
assert 'localStorage' not in legacy
assert 'document.documentElement.dir =' not in legacy
assert 'document.cookie' not in legacy
assert 'document.cookie' in authority
assert 'stopImmediatePropagation' in authority
assert 'data-fa' in legacy and 'data-en' in legacy
print('Stage 107 source checks passed')
