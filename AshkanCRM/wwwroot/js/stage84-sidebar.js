/* Stage 84: collapsible sidebar sections, preserving Stage 82 search and Stage 83 favorites. */
(() => {
  'use strict';
  const nav = document.querySelector('.sidebar > nav');
  const search = document.getElementById('sidebar-menu-search');
  if (!nav) return;
  const labels = [...nav.querySelectorAll(':scope > .nav-label')];
  if (!labels.length) return;
  const key = 'ashkancrm.sidebar.groups.v1';
  let preferences = {};
  try { const data = JSON.parse(localStorage.getItem(key) || '{}'); if (data && typeof data === 'object' && !Array.isArray(data)) preferences = data; } catch (_) { /* private browsing */ }
  const english = () => document.documentElement.lang?.toLowerCase().startsWith('en');
  const groups = [];
  window.addEventListener("crm-sidebar-preferences-remote", () => {
    let remote = {}; try { remote = JSON.parse(localStorage.getItem(key) || "{}"); } catch (_) {}
    for (const group of nav.querySelectorAll(":scope > .sidebar-group")) {
      const source = group.querySelector(".sidebar-group-source");
      const id = source?.dataset.i18n || source?.dataset.en;
      const expanded = remote[id];
      if (typeof expanded !== "boolean") continue;
      const content = group.querySelector(".sidebar-group-content");
      const toggle = group.querySelector(".sidebar-group-toggle");
      if (content) content.hidden = !expanded;
      if (toggle) toggle.setAttribute("aria-expanded", String(expanded));
      group.classList.toggle("is-collapsed", !expanded);
    }
  });
  for (let i = 0; i < labels.length; i++) {
    const label = labels[i];
    const section = document.createElement('section');
    section.className = 'sidebar-group';
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'sidebar-group-toggle';
    const name = document.createElement('span');
    name.className = 'sidebar-group-name';
    const chevron = document.createElement('span');
    chevron.className = 'sidebar-group-chevron';
    chevron.setAttribute('aria-hidden', 'true');
    chevron.textContent = '⌄';
    button.append(name, chevron);
    const content = document.createElement('div');
    content.className = 'sidebar-group-content';
    content.id = `crm-sidebar-group-${i}`;
    button.setAttribute('aria-controls', content.id);
    label.parentNode.insertBefore(section, label);
    section.append(button, content);
    let cursor = label.nextSibling;
    while (cursor && !(cursor.nodeType === 1 && cursor.classList.contains('nav-label'))) {
      const next = cursor.nextSibling;
      content.append(cursor);
      cursor = next;
    }
    // The original label remains the translation source, outside the rendered group.
    label.hidden = true;
    label.classList.add('sidebar-group-source');
    section.append(label);
    const groupId = label.dataset.i18n || label.dataset.en || `group-${i}`;
    const defaultExpanded = true;
    const preferred = typeof preferences[groupId] === 'boolean' ? preferences[groupId] : defaultExpanded;
    function setExpanded(expanded) {
      content.hidden = !expanded;
      button.setAttribute('aria-expanded', String(expanded));
      section.classList.toggle('is-collapsed', !expanded);
    }
    function translate() {
      name.textContent = english() ? (label.dataset.en || label.textContent.trim()) : (label.dataset.fa || label.textContent.trim());
      button.setAttribute('aria-label', (english() ? 'Toggle ' : 'باز و بسته کردن ') + name.textContent);
    }
    translate(); setExpanded(preferred);
    button.addEventListener('click', () => {
      if (search?.value.trim()) return;
      const next = button.getAttribute('aria-expanded') !== 'true';
      preferences[groupId] = next;
      try { localStorage.setItem(key, JSON.stringify(preferences)); window.dispatchEvent(new Event("crm-sidebar-preferences-changed")); } catch (_) { /* optional persistence */ }
      setExpanded(next);
    });
    groups.push({section, content, button, setExpanded, translate, groupId});
  }
  function syncSearch() {
    const searching = !!search?.value.trim();
    for (const group of groups) {
      const links = [...group.content.querySelectorAll('a[href]')];
      const visible = links.filter(link => !link.hidden);
      group.section.hidden = searching && !visible.length;
      group.button.disabled = searching;
      group.setExpanded(searching ? true : (typeof preferences[group.groupId] === 'boolean' ? preferences[group.groupId] : true));
    }
  }
  // Stage 82 registered its input handler earlier; this listener runs after it.
  search?.addEventListener('input', syncSearch);
  document.getElementById('sidebar-menu-clear')?.addEventListener('click', () => queueMicrotask(syncSearch));
  search?.addEventListener('keydown', event => { if (event.key === 'Escape') queueMicrotask(syncSearch); });
  document.getElementById('langBtn')?.addEventListener('click', () => queueMicrotask(() => groups.forEach(g => g.translate())));
  syncSearch();
})();
