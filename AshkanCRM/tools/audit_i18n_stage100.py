"""Report potential missing bilingual labels in Razor pages; does not modify content."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[1] / 'Views'
counts = []
for page in sorted(root.rglob('*.cshtml')):
    text = page.read_text(encoding='utf-8-sig')
    fa = len(re.findall(r'[\u0600-\u06ff]', text))
    en = len(re.findall(r'\b[A-Za-z]{4,}\b', text))
    paired = len(re.findall(r'data-fa\s*=', text))
    if fa and en:
        counts.append((str(page.relative_to(root)), fa, en, paired))
print('Razor pages containing Persian and English source text (manual review candidates):')
for page, fa, en, paired in counts:
    print(f'{page}\tfa_chars={fa}\ten_words={en}\tpaired_labels={paired}')
print(f'Total candidates: {len(counts)}')
print('NOTE: source-text counts are heuristic, not proof of untranslated UI. Review runtime output.')
