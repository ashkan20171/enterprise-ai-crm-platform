/* Stage 90: synchronize existing user appearance preferences with the actual CRM theme. */
(() => {
  'use strict';
  const root = document.documentElement;
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  const resolve = () => {
    const choice = root.dataset.crmAppearance || 'system';
    return choice === 'system' ? (media.matches ? 'dark' : 'light') : choice;
  };
  const refresh = () => {
    const effective = resolve();
    root.dataset.theme = effective;
    root.style.colorScheme = effective;
    const button = document.getElementById('themeBtn');
    if (button) {
      const en = root.lang === 'en';
      button.setAttribute('aria-label', en ? `Theme: ${effective}. Click to toggle` : `پوسته: ${effective === 'dark' ? 'تیره' : 'روشن'}؛ برای تغییر کلیک کنید`);
      button.setAttribute('aria-pressed', String(effective === 'dark'));
    }
  };
  const original = window.crm89ApplyAppearance;
  if (typeof original === 'function') {
    window.crm89ApplyAppearance = data => { original(data); refresh(); };
  }
  const observer = new MutationObserver(records => {
    if (records.some(r => r.attributeName === 'data-crm-appearance')) refresh();
  });
  observer.observe(root, { attributes: true, attributeFilter: ['data-crm-appearance'] });
  const changed = () => { refresh(); };
  if (media.addEventListener) media.addEventListener('change', changed);
  else if (media.addListener) media.addListener(changed);
  // Capture before legacy crm.js handler so the header button uses the same persisted preference.
  document.addEventListener('click', event => {
    if (!event.target.closest('#themeBtn')) return;
    event.stopImmediatePropagation();
    const next = resolve() === 'dark' ? 'light' : 'dark';
    const state = typeof window.crm89AppearanceState === 'function' ? window.crm89AppearanceState() : {};
    if (typeof window.crm89ApplyAppearance === 'function') {
      window.crm89ApplyAppearance({ ...state, appearance: next });
      window.dispatchEvent(new Event('crm-sidebar-preferences-changed'));
    } else {
      root.dataset.crmAppearance = next;
      refresh();
    }
  }, true);
  refresh();
})();
