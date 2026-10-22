/* Stage 82 sidebar quick filter. No observers or polling. */
(function () {
  'use strict';
  const sidebar = document.querySelector('.sidebar');
  const nav = sidebar?.querySelector('nav');
  const input = document.getElementById('sidebar-menu-search');
  const clear = document.getElementById('sidebar-menu-clear');
  const status = document.getElementById('sidebar-menu-status');
  if (!nav || !input || !clear || !status) return;
  const links = Array.from(nav.querySelectorAll('a[href]'));
  const labels = Array.from(nav.querySelectorAll('.nav-label'));
  const locale = () => document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'fa';
  function applyFilter() {
    const query = input.value.trim().toLocaleLowerCase();
    let shown = 0;
    for (const link of links) {
      const translated = Array.from(link.querySelectorAll('[data-fa],[data-en]')).map(el => `${el.dataset.fa || ''} ${el.dataset.en || ''}`).join(' ');
      const haystack = `${link.textContent} ${translated} ${link.getAttribute('href')}`.toLocaleLowerCase();
      const visible = !query || haystack.includes(query);
      link.hidden = !visible;
      if (visible) shown++;
    }
    for (const label of labels) label.hidden = !!query;
    clear.hidden = !query;
    status.hidden = !query;
    if (query) status.textContent = locale() === 'en' ? `${shown} of ${links.length} menu items` : `${shown} مورد از ${links.length} گزینه منو`;
  }
  input.addEventListener('input', applyFilter);
  clear.addEventListener('click', () => { input.value = ''; applyFilter(); input.focus(); });
  input.addEventListener('keydown', event => {
    if (event.key === 'Escape') { input.value = ''; applyFilter(); input.focus(); }
    if (event.key === 'Enter') { const first = links.find(link => !link.hidden); if (first) { event.preventDefault(); first.focus(); } }
  });
  applyFilter();
})();
