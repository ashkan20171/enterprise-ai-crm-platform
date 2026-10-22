from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'Views/CRM/Dashboard.cshtml').read_text(encoding='utf-8')
assert 'string LocalizedStage(string? code)' in s
assert '@LocalizedStage(deal.Stage)' in s
assert 'data-stage58-code="@deal.Stage"' in s
assert 'aria-label="@(isEnglish ? "Scheduled open pipeline by month"' in s
assert '<td>@deal.Stage</td>' not in s
print('Stage 109 localization structural checks passed')
