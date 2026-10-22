/* Stage 83: browser-local sidebar favorites and active page indicator.
   Uses only currently authorized server-rendered links; no new routes or permissions. */
(() => {
  'use strict';
  const sidebar = document.querySelector('.sidebar');
  const nav = sidebar?.querySelector('nav');
  const favorites = document.getElementById('sidebar-favorites');
  if (!nav || !favorites) return;
  const links = [...nav.querySelectorAll('a[href]')];
  const storageKey = 'ashkancrm.sidebar.favorites.v1';
  const normalize = path => (path || '/').replace(/\/+$/, '').toLowerCase() || '/';
  const allowed = new Map();
  for (const link of links) {
    const url = new URL(link.getAttribute('href'), window.location.href);
    if (url.origin !== window.location.origin) continue;
    allowed.set(normalize(url.pathname), link);
  }
  let saved = [];
  try {
    const parsed = JSON.parse(localStorage.getItem(storageKey) || '[]');
    if (Array.isArray(parsed)) saved = parsed.filter(x => typeof x === 'string').slice(0, 30);
  } catch (_) { saved = []; }
  const persist = () => { try { localStorage.setItem(storageKey, JSON.stringify(saved)); window.dispatchEvent(new Event("crm-sidebar-preferences-changed")); } catch (_) { /* storage may be disabled */ } };
  const isEnglish = () => document.documentElement.lang?.toLowerCase().startsWith('en');
  const current = normalize(window.location.pathname);
  const selected = allowed.get(current);
  if (selected) { selected.classList.add('sidebar-current'); selected.setAttribute('aria-current', 'page'); }
  function render() {
    favorites.replaceChildren();
    const available = [...new Set(saved)].filter(path => allowed.has(path));
    if (!available.length) { favorites.hidden = true; return; }
    favorites.hidden = false;
    const heading = document.createElement('div');
    heading.className = 'sidebar-favorites-heading';
    heading.textContent = isEnglish() ? '★ Favorites' : '★ علاقه‌مندی‌ها';
    favorites.append(heading);
    for (const path of available) {
      const original = allowed.get(path);
      const row = document.createElement('div'); row.className = 'sidebar-favorite-row';
      const anchor = original.cloneNode(true);
      anchor.classList.remove('sidebar-current');
      anchor.removeAttribute('aria-current');
      anchor.classList.add('sidebar-favorite-link');
      anchor.querySelectorAll('.sidebar-pin').forEach(x => x.remove());
      if (path === current) { anchor.classList.add('sidebar-current'); anchor.setAttribute('aria-current', 'page'); }
      const remove = document.createElement('button');
      remove.type = 'button'; remove.className = 'sidebar-favorite-remove';
      remove.textContent = '×';
      remove.setAttribute('aria-label', isEnglish() ? 'Remove favorite' : 'حذف از علاقه‌مندی‌ها');
      remove.addEventListener('click', () => { saved = saved.filter(x => x !== path); persist(); updatePins(); render(); });
      row.append(anchor, remove); favorites.append(row);
    }
  }
  function updatePins() {
    for (const [path, link] of allowed) {
      const pin = link.querySelector('.sidebar-pin');
      if (!pin) continue;
      const active = saved.includes(path);
      pin.textContent = active ? '★' : '☆';
      pin.setAttribute('aria-pressed', String(active));
      pin.setAttribute('aria-label', isEnglish() ? (active ? 'Unpin from favorites' : 'Pin to favorites') : (active ? 'حذف از علاقه‌مندی‌ها' : 'افزودن به علاقه‌مندی‌ها'));
      pin.title = pin.getAttribute('aria-label');
    }
  }
  for (const [path, link] of allowed) {
    const pin = document.createElement('button');
    pin.type = 'button'; pin.className = 'sidebar-pin';
    pin.addEventListener('click', event => {
      event.preventDefault(); event.stopPropagation();
      saved = saved.includes(path) ? saved.filter(x => x !== path) : [...saved, path].slice(-30);
      persist(); updatePins(); render();
    });
    // Do not nest buttons inside links: place the pin alongside its link.
    const wrapper = document.createElement('div'); wrapper.className = 'sidebar-nav-row';
    link.parentNode.insertBefore(wrapper, link);
    wrapper.append(link, pin);
  }
  window.addEventListener("crm-sidebar-preferences-remote", () => {
    try { const value = JSON.parse(localStorage.getItem(storageKey) || "[]"); saved = Array.isArray(value) ? value.filter(x => typeof x === "string").slice(0, 30) : []; } catch (_) { saved = []; }
    updatePins(); render();
  });
  updatePins(); render();
  // Re-render labels when the existing language button changes the locale.
  document.getElementById('langBtn')?.addEventListener('click', () => queueMicrotask(() => { updatePins(); render(); }));
})();
