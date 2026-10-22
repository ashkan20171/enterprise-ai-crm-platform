/* Stage 87: authenticated per-user sidebar preference sync; localStorage remains offline fallback. */
(() => {
  'use strict';
  const nav = document.querySelector('.sidebar > nav');
  const sidebar = document.querySelector('.sidebar');
  if (!nav || !sidebar) return;
  const orderKey = 'ashkancrm.sidebar.group-order.v1';
  const favoritesKey = 'ashkancrm.sidebar.favorites.v1';
  const expandedKey = 'ashkancrm.sidebar.groups.v1';
  const readJson = (key, fallback) => { try { return JSON.parse(localStorage.getItem(key) || JSON.stringify(fallback)); } catch (_) { return fallback; } };
  const densityKey = 'ashkancrm.sidebar.density.v1';
  const groupId = group => group.querySelector('.sidebar-group-source')?.dataset.i18n || group.querySelector('.sidebar-group-source')?.dataset.en || '';
  const groups = () => [...nav.querySelectorAll(':scope > .sidebar-group')];
  const readState = () => ({ groupOrder: groups().map(groupId), density: sidebar.classList.contains('sidebar-compact') ? 'compact' : 'normal', favorites: readJson(favoritesKey, []), groupExpanded: readJson(expandedKey, {}), ...window.crm89AppearanceState?.() });
  const token = document.querySelector('#sidebar-sync-token input[name="__RequestVerificationToken"]')?.value;
  let initialized = false;
  let applyingRemote = false;
  let timer;
  let lastSaved = '';
  const persist = async () => {
    if (!initialized || applyingRemote || !token || !navigator.onLine) return;
    const state = readState();
    const serialized = JSON.stringify(state);
    if (serialized === lastSaved) return;
    try {
      const response = await fetch('/api/sidebar-preferences', {
        method: 'POST', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: serialized
      });
      if (response.ok) lastSaved = serialized;
    } catch (_) { /* offline: the existing local preference remains usable */ }
  };
  const schedule = () => { if (!initialized || applyingRemote) return; clearTimeout(timer); timer = setTimeout(persist, 700); };
  const applyRemote = data => {
    applyingRemote = true;
    if (Array.isArray(data.favorites)) {
      const allowed = new Set([...nav.querySelectorAll('a[href]')].map(a => new URL(a.href, location.href)).filter(u => u.origin === location.origin).map(u => u.pathname.replace(/\/+$/, "").toLowerCase()));
      const filtered = [...new Set(data.favorites.filter(x => typeof x === "string" && allowed.has(x.toLowerCase())))].slice(0, 30);
      try { localStorage.setItem(favoritesKey, JSON.stringify(filtered)); } catch (_) {}
    }
    if (data.groupExpanded && typeof data.groupExpanded === "object" && !Array.isArray(data.groupExpanded)) {
      try { localStorage.setItem(expandedKey, JSON.stringify(data.groupExpanded)); } catch (_) {}
    }
    const available = new Map(groups().map(group => [groupId(group), group]));
    if (Array.isArray(data.groupOrder)) {
      const ordered = [...new Set(data.groupOrder.filter(x => typeof x === 'string'))];
      const fragment = document.createDocumentFragment();
      for (const id of ordered) if (available.has(id)) { fragment.append(available.get(id)); available.delete(id); }
      for (const group of available.values()) fragment.append(group);
      nav.append(fragment);
      try { localStorage.setItem(orderKey, JSON.stringify(groups().map(groupId))); } catch (_) { }
    }
    if (data.density === 'normal' || data.density === 'compact') {
      sidebar.classList.toggle('sidebar-compact', data.density === 'compact');
      const select = nav.querySelector('.sidebar-density-select');
      if (select) select.value = data.density;
      try { localStorage.setItem(densityKey, data.density); } catch (_) { }
    }
    nav.querySelectorAll(':scope > .sidebar-group').forEach((group, index, all) => {
      const up = group.querySelector('.sidebar-order-up');
      const down = group.querySelector('.sidebar-order-down');
      if (up) up.disabled = index === 0;
      if (down) down.disabled = index === all.length - 1;
    });
    if (window.crm89ApplyAppearance) window.crm89ApplyAppearance(data);
    window.dispatchEvent(new Event("crm-sidebar-preferences-remote"));
    applyingRemote = false;
  };
  const start = async () => {
    try {
      const response = await fetch('/api/sidebar-preferences', { credentials: 'same-origin', cache: 'no-store' });
      if (response.ok) {
        const data = await response.json();
        if (data.hasSaved) applyRemote(data);
        lastSaved = data.hasSaved ? JSON.stringify(readState()) : '';
      }
    } catch (_) { /* keep local state */ }
    initialized = true;
    if (!lastSaved) schedule(); // first device: seed the server from local preferences
  };
  // Observe only direct group movement, not text/attribute updates (avoids mutation loops).
  const observer = new MutationObserver(records => {
    if (records.some(record => [...record.addedNodes, ...record.removedNodes].some(node => node.nodeType === 1 && node.classList?.contains('sidebar-group')))) schedule();
  });
  observer.observe(nav, { childList: true });
  nav.addEventListener('click', event => {
    if (event.target.closest('.sidebar-order-up, .sidebar-order-down, .sidebar-order-reset')) schedule();
  });
  nav.addEventListener('change', event => { if (event.target.matches('.sidebar-density-select')) schedule(); });
  window.addEventListener('online', schedule);
  window.addEventListener("crm-sidebar-preferences-changed", schedule);
  start();
})();
