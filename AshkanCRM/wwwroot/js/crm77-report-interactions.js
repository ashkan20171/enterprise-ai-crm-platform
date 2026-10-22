/* Stage 78: dependency-free progressive enhancement for server-rendered reports. */
(() => {
  'use strict';
  const controls = document.querySelector('[data-report-controls]');
  const chart = document.querySelector('[data-report-chart]');
  const table = document.querySelector('[data-report-table]');
  if (!controls || !chart || !table || !table.tBodies.length) return;
  const search = controls.querySelector('[data-report-search]');
  const sort = controls.querySelector('[data-report-sort]');
  const count = controls.querySelector('[data-report-count]');
  const empty = controls.querySelector('[data-report-empty]');
  const clear = controls.querySelector('[data-report-clear]');
  if (!search || !sort || !count) return;
  const chartRows = Array.from(chart.querySelectorAll('[data-report-item]'));
  const tableRows = Array.from(table.tBodies[0].querySelectorAll('[data-report-item]'));
  const collator = new Intl.Collator(document.documentElement.lang || undefined, { sensitivity: 'base', numeric: true });
  const number = (row, field) => {
    const value = Number(row.dataset[field]);
    return Number.isFinite(value) ? value : 0;
  };
  const compare = (a, b) => {
    const byName = () => collator.compare(a.dataset.name || '', b.dataset.name || '');
    switch (sort.value) {
      case 'name-asc': return byName();
      case 'target-desc': return number(b, 'target') - number(a, 'target') || byName();
      default: return number(b, 'actual') - number(a, 'actual') || byName();
    }
  };
  const update = () => {
    const needle = search.value.trim().toLocaleLowerCase();
    for (const [parent, rows] of [[chart, chartRows], [table.tBodies[0], tableRows]]) {
      rows.sort(compare);
      // Use a fragment to avoid repeated reflows for large reports.
      const fragment = document.createDocumentFragment();
      for (const row of rows) {
        row.hidden = !!needle && !(row.dataset.name || '').toLocaleLowerCase().includes(needle);
        fragment.appendChild(row);
      }
      parent.appendChild(fragment);
    }
    const shown = tableRows.filter(row => !row.hidden).length;
    count.textContent = document.documentElement.lang?.toLowerCase().startsWith('fa')
      ? `${shown} مورد از ${tableRows.length}` : `${shown} of ${tableRows.length} shown`;
    if (empty) empty.hidden = shown !== 0;
    if (clear) clear.hidden = needle.length === 0;
  };
  search.addEventListener('input', update);
  sort.addEventListener('change', update);
  if (clear) clear.addEventListener('click', () => { search.value = ''; update(); search.focus(); });
  update();
})();
