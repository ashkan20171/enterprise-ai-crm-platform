/* Stage 107 compatibility: the server-selected culture is authoritative.
   Stage 105 exclusively manages culture cookies and reloads. */
(() => {
  'use strict';
  function renderLocalizedLabels(root = document) {
    const language = document.documentElement.lang.toLowerCase().startsWith('en') ? 'en' : 'fa';
    root.querySelectorAll('[data-fa][data-en]').forEach(element => {
      const value = element.getAttribute('data-' + language);
      if (value !== null && element.textContent !== value && element.children.length === 0) {
        element.textContent = value;
      }
    });
  }
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => renderLocalizedLabels(), { once: true });
  } else {
    renderLocalizedLabels();
  }
  window.addEventListener('crm:language-changed', () => renderLocalizedLabels());
})();
