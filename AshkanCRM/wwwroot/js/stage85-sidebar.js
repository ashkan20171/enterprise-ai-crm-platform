/* Stage 85: keyboard-accessible sidebar group ordering and reset. */
(() => {
  'use strict';
  const nav = document.querySelector('.sidebar > nav');
  if (!nav) return;
  const groups = [...nav.querySelectorAll(':scope > .sidebar-group')];
  if (groups.length < 2) return;
  const storageKey = 'ashkancrm.sidebar.group-order.v1';
  const groupId = group => group.querySelector('.sidebar-group-source')?.dataset.i18n ||
    group.querySelector('.sidebar-group-source')?.dataset.en || '';
  const byId = new Map(groups.map(group => [groupId(group), group]));
  const original = groups.map(groupId);
  let stored = [];
  try {
    const parsed = JSON.parse(localStorage.getItem(storageKey) || '[]');
    if (Array.isArray(parsed)) stored = parsed.filter(id => typeof id === 'string');
  } catch (_) { /* localStorage may be unavailable */ }
  const ordered = [...new Set([...stored, ...original])].filter(id => byId.has(id));
  const applyOrder = ids => {
    const fragment = document.createDocumentFragment();
    ids.forEach(id => fragment.append(byId.get(id)));
    nav.append(fragment);
  };
  applyOrder(ordered);
  const english = () => document.documentElement.lang?.toLowerCase().startsWith('en');
  const save = () => {
    try { localStorage.setItem(storageKey, JSON.stringify([...nav.querySelectorAll(':scope > .sidebar-group')].map(groupId))); }
    catch (_) { /* localStorage is optional */ }
  };
  const toolbar = document.createElement('div');
  toolbar.className = 'sidebar-order-toolbar';
  const reset = document.createElement('button');
  reset.type = 'button';
  reset.className = 'sidebar-order-reset';
  toolbar.append(reset);
  nav.insertBefore(toolbar, nav.firstChild);
  const updateLanguage = () => {
    reset.textContent = english() ? '↺ Reset menu order' : '↺ بازنشانی ترتیب منو';
    for (const group of groups) {
      const buttons = group.querySelectorAll('.sidebar-order-up, .sidebar-order-down');
      buttons.forEach(button => {
        const up = button.classList.contains('sidebar-order-up');
        button.setAttribute('aria-label', english() ? (up ? 'Move section up' : 'Move section down') : (up ? 'انتقال گروه به بالا' : 'انتقال گروه به پایین'));
        button.title = button.getAttribute('aria-label');
      });
    }
  };
  const refresh = () => {
    const items = [...nav.querySelectorAll(':scope > .sidebar-group')];
    items.forEach((group, index) => {
      group.querySelector('.sidebar-order-up').disabled = index === 0;
      group.querySelector('.sidebar-order-down').disabled = index === items.length - 1;
    });
  };
  groups.forEach(group => {
    const toggle = group.querySelector('.sidebar-group-toggle');
    if (!toggle) return;
    const controls = document.createElement('div');
    controls.className = 'sidebar-order-controls';
    const makeButton = (direction, glyph) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = `sidebar-order-${direction}`;
      button.textContent = glyph;
      button.addEventListener('click', () => {
        const adjacent = direction === 'up' ? group.previousElementSibling : group.nextElementSibling;
        if (!adjacent?.classList.contains('sidebar-group')) return;
        if (direction === 'up') nav.insertBefore(group, adjacent);
        else nav.insertBefore(adjacent, group);
        save(); refresh(); button.focus();
      });
      return button;
    };
    controls.append(makeButton('up', '↑'), makeButton('down', '↓'));
    toggle.insertAdjacentElement('afterend', controls);
  });
  reset.addEventListener('click', () => {
    applyOrder(original);
    try { localStorage.removeItem(storageKey); } catch (_) { /* optional */ }
    refresh(); reset.focus();
  });
  document.getElementById('langBtn')?.addEventListener('click', () => queueMicrotask(updateLanguage));
  updateLanguage(); refresh();
})();
