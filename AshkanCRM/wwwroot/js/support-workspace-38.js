(() => {
  'use strict';
  const input = document.getElementById('agent-search');
  if (!input) return;
  const rows = [...document.querySelectorAll('[data-agent-ticket]')];
  const empty = document.getElementById('agent-no-results');
  input.addEventListener('input', () => {
    const query = input.value.trim().toLocaleLowerCase();
    let visible = 0;
    for (const row of rows) {
      const match = row.textContent.toLocaleLowerCase().includes(query);
      row.hidden = !match;
      if (match) visible++;
    }
    if (empty) empty.hidden = visible > 0;
  });
  const localize = () => {
    const fa = document.documentElement.lang.toLowerCase().startsWith('fa');
    for (const node of document.querySelectorAll('#agent-workspace [data-fa], #agent-workspace [data-en]')) {
      const value = node.getAttribute(fa ? 'data-fa' : 'data-en');
      if (value) node.textContent = value;
    }
    input.placeholder = input.getAttribute(fa ? 'data-fa-placeholder' : 'data-en-placeholder') || '';
  };
  localize();
  new MutationObserver(localize).observe(document.documentElement, {attributes:true,attributeFilter:['lang']});
})();
