/* Stage 105: one authoritative language switch for server-rendered and client UI. */
(() => {
  'use strict';
  const culture = language => language === 'en' ? 'en-US' : 'fa-IR';
  const selected = () => document.documentElement.lang.toLowerCase().startsWith('en') ? 'en' : 'fa';
  const cookieFor = language => `.AspNetCore.Culture=${encodeURIComponent(`c=${culture(language)}|uic=${culture(language)}`)}`;
  function writeCulture(language) {
    document.cookie = `${cookieFor(language)}; Path=/; SameSite=Lax; Max-Age=31536000`;
  }
  function switchLanguage(event) {
    const button = event.target.closest?.('#langBtn, [data-lang-toggle]');
    if (!button) return;
    event.preventDefault();
    event.stopImmediatePropagation();
    const next = selected() === 'en' ? 'fa' : 'en';
    try { localStorage.setItem('crm-lang', next); } catch (_) { /* Cookie still works */ }
    writeCulture(next);
    window.location.reload();
  }
  // Capture before legacy bubble listeners to prevent double toggles and stale Razor strings.
  document.addEventListener('click', switchLanguage, true);
  document.addEventListener('DOMContentLoaded', () => {
    const serverLanguage = selected();
    try { localStorage.setItem('crm-lang', serverLanguage); } catch (_) { /* Private mode */ }
    const button = document.querySelector('#langBtn, [data-lang-toggle]');
    if (button) button.textContent = serverLanguage === 'en' ? 'FA' : 'EN';
  });
  window.addEventListener('storage', event => {
    if (event.key !== 'crm-lang' || (event.newValue !== 'fa' && event.newValue !== 'en')) return;
    if (event.newValue === selected()) return;
    writeCulture(event.newValue);
    window.location.reload();
  });
})();
