(() => {
  'use strict';
  const lang = () => localStorage.getItem('crm-lang') === 'en' ? 'en' : 'fa';
  function apply() {
    const l = lang();
    if(document.documentElement.lang !== l) document.documentElement.lang = l;
    const d = l === 'fa' ? 'rtl' : 'ltr';
    if(document.documentElement.dir !== d) document.documentElement.dir = d;
    document.querySelectorAll('[data-fa][data-en]').forEach(el => {
      const value = el.getAttribute('data-' + l);
      if(value !== null && el.textContent !== value) el.textContent = value;
    });
  }
  document.addEventListener('DOMContentLoaded', () => {
    apply();
    document.getElementById('langBtn')?.addEventListener('click', () => queueMicrotask(apply));
  });
  window.addEventListener('storage', event => { if(event.key === 'crm-lang') apply(); });
})();
