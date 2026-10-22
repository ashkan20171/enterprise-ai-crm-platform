/* Stage 101: explicit localization for stable coded select values. */
(() => {
  'use strict';
  function currentLanguage() {
    return document.documentElement.lang.toLowerCase().startsWith('en') ? 'en' : 'fa';
  }
  function apply() {
    const lang = currentLanguage();
    document.querySelectorAll('[data-stage101-en][data-stage101-fa]').forEach(option => {
      const label = option.getAttribute('data-stage101-' + lang);
      if (label !== null && option.textContent !== label) option.textContent = label;
    });
  }
  document.addEventListener('DOMContentLoaded', apply);
  document.addEventListener('crm:language-changed', () => queueMicrotask(apply));
  document.addEventListener('click', event => {
    if (event.target.closest('#langBtn,[data-lang-toggle]')) setTimeout(apply, 120);
  });
  window.addEventListener('pageshow', apply);
})();
