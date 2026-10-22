#!/usr/bin/env python3
"""Dependency-free structural smoke checks; does not replace compilation/integration tests."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
views = [root / 'Views/CRM/SalesPerformance.cshtml', root / 'Views/CRM/TeamSalesComparison.cshtml']
for path in views:
    content = path.read_text(encoding='utf-8')
    for marker in ('data-report-controls', 'data-report-search', 'data-report-sort', 'data-report-chart', 'data-report-table', 'data-report-count', 'data-report-clear', 'data-report-empty'):
        assert marker in content, f'{path.name}: missing {marker}'
    for marker in ('Export', 'asp-route-months'):
        assert marker in content, f'{path.name}: missing {marker}'
script = (root / 'wwwroot/js/crm77-report-interactions.js').read_text(encoding='utf-8')
for marker in ('createDocumentFragment', "search.addEventListener('input'", "sort.addEventListener('change'", "clear.addEventListener('click'", 'Number.isFinite'):
    assert marker in script, f'Interaction script missing {marker}'
print('Stage 78 structural smoke checks passed (2 views, 1 script).')
