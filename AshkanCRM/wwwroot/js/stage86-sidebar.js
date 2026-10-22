/* Stage 86: accessible drag handles and local sidebar density preferences. */
(() => {
  'use strict';
  const sidebar = document.querySelector('.sidebar');
  const nav = sidebar?.querySelector(':scope > nav');
  if (!nav) return;
  const groups = [...nav.querySelectorAll(':scope > .sidebar-group')];
  const orderKey = 'ashkancrm.sidebar.group-order.v1';
  const densityKey = 'ashkancrm.sidebar.density.v1';
  const id = group => group.querySelector('.sidebar-group-source')?.dataset.i18n || group.querySelector('.sidebar-group-source')?.dataset.en || '';
  const english = () => document.documentElement.lang?.toLowerCase().startsWith('en');
  const refreshButtons = () => {
    const ordered = [...nav.querySelectorAll(':scope > .sidebar-group')];
    ordered.forEach((group, index) => {
      const up = group.querySelector('.sidebar-order-up');
      const down = group.querySelector('.sidebar-order-down');
      if (up) up.disabled = index === 0;
      if (down) down.disabled = index === ordered.length - 1;
    });
  };
  const saveOrder = () => {
    try { localStorage.setItem(orderKey, JSON.stringify([...nav.querySelectorAll(':scope > .sidebar-group')].map(id))); }
    catch (_) { /* optional browser persistence */ }
    refreshButtons();
  };
  let dragging = null;
  for (const group of groups) {
    const controls = group.querySelector('.sidebar-order-controls');
    if (!controls) continue;
    const handle = document.createElement('span');
    handle.className = 'sidebar-drag-handle';
    handle.textContent = '⠿';
    handle.draggable = true;
    handle.tabIndex = 0;
    handle.setAttribute('role', 'img');
    const translate = () => {
      handle.title = english() ? 'Drag to reorder section; use arrow buttons with keyboard' : 'برای تغییر ترتیب بکشید؛ با صفحه‌کلید از دکمه‌های جهت‌دار استفاده کنید';
      handle.setAttribute('aria-label', handle.title);
    };
    translate();
    document.getElementById('langBtn')?.addEventListener('click', () => queueMicrotask(translate));
    controls.prepend(handle);
    handle.addEventListener('dragstart', event => {
      if (document.getElementById('sidebar-menu-search')?.value.trim()) { event.preventDefault(); return; }
      dragging = group;
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', id(group));
      group.classList.add('sidebar-dragging');
    });
    handle.addEventListener('dragend', () => {
      group.classList.remove('sidebar-dragging');
      groups.forEach(item => item.classList.remove('sidebar-drop-target'));
      dragging = null;
    });
    group.addEventListener('dragover', event => {
      if (!dragging || dragging === group) return;
      event.preventDefault();
      event.dataTransfer.dropEffect = 'move';
      group.classList.add('sidebar-drop-target');
    });
    group.addEventListener('dragleave', event => {
      if (!group.contains(event.relatedTarget)) group.classList.remove('sidebar-drop-target');
    });
    group.addEventListener('drop', event => {
      if (!dragging || dragging === group) return;
      event.preventDefault();
      const before = event.clientY < group.getBoundingClientRect().top + group.getBoundingClientRect().height / 2;
      nav.insertBefore(dragging, before ? group : group.nextSibling);
      groups.forEach(item => item.classList.remove('sidebar-drop-target'));
      saveOrder();
      dragging.querySelector('.sidebar-drag-handle')?.focus();
    });
  }
  const toolbar = nav.querySelector('.sidebar-order-toolbar');
  if (!toolbar) return;
  const select = document.createElement('select');
  select.className = 'sidebar-density-select';
  select.setAttribute('aria-label', 'Sidebar size / اندازه سایدبار');
  const choices = [['normal', 'عادی / Normal'], ['compact', 'فشرده / Compact']];
  choices.forEach(([value, label]) => { const option = document.createElement('option'); option.value = value; option.textContent = label; select.append(option); });
  let density = 'normal';
  try { if (localStorage.getItem(densityKey) === 'compact') density = 'compact'; } catch (_) { /* optional */ }
  const applyDensity = value => { sidebar.classList.toggle('sidebar-compact', value === 'compact'); select.value = value; };
  applyDensity(density);
  select.addEventListener('change', () => {
    applyDensity(select.value);
    try { localStorage.setItem(densityKey, select.value); } catch (_) { /* optional */ }
  });
  toolbar.append(select);
})();
