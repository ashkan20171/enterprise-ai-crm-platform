from pathlib import Path
root=Path(__file__).resolve().parents[1]
for name in ('Index','Details'):
 s=(root/'Views'/'Support'/f'{name}.cshtml').read_text(encoding='utf-8')
 assert 'var isEnglish' in s and 'LocalStatus(' in s
 assert '@functions' in s and '@(isEnglish ?' in s
 assert s.count('@(isEnglish ?') >= 10
print('Stage 110 support localization source checks passed.')
