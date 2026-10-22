from pathlib import Path
root = Path(__file__).parent
layout = (root / 'Views/Shared/_Layout.cshtml').read_text(encoding='utf-8')
js = (root / 'wwwroot/js/stage99-global-localization.js').read_text(encoding='utf-8')
assert 'stage99-global-localization.js' in layout
assert 'data-no-translate' in js
assert '.AspNetCore.Culture' in js
assert "document.documentElement.dir = direction" in js
assert 'MutationObserver' in js
print('Stage 99 static checks passed')
