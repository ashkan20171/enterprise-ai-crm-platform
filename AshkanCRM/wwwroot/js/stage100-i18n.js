/* Stage 100: explicit bilingual markup and safe attribute localization.
 * User-entered text and data rows are never machine-translated.
 */
(() => {
  'use strict';
  const getLanguage = () => localStorage.getItem('crm-lang') === 'en' ? 'en' : 'fa';
  const selector = '[data-fa][data-en]';
  const protectedElements = 'input,textarea,select,option,[contenteditable],script,style,code,pre,[data-no-translate],.user-content';
  function update(root = document) {
    const language = getLanguage();
    document.documentElement.lang = language === 'en' ? 'en-US' : 'fa-IR';
    document.documentElement.dir = language === 'en' ? 'ltr' : 'rtl';
    const elements = [];
    if (root.nodeType === 1 && root.matches(selector)) elements.push(root);
    root.querySelectorAll?.(selector).forEach(el => elements.push(el));
    for (const el of elements) {
      if (el.matches(protectedElements) || el.closest('[data-no-translate],.user-content')) continue;
      const value = el.getAttribute(`data-${language}`);
      if (value === null) continue;
      // Only replace simple text labels; never destroy nested icons, links, or inputs.
      if (el.childElementCount === 0) { if (el.textContent !== value) el.textContent = value; }
      else {
        const textNodes = Array.from(el.childNodes).filter(n => n.nodeType === Node.TEXT_NODE && n.nodeValue.trim());
        if (textNodes.length === 1) textNodes[0].nodeValue = value;
      }
    }
    root.querySelectorAll?.('[data-fa-placeholder][data-en-placeholder],[data-fa-title][data-en-title],[data-fa-aria-label][data-en-aria-label]').forEach(el => {
      for (const [suffix, attr] of [['placeholder','placeholder'],['title','title'],['aria-label','aria-label']]) {
        const value = el.getAttribute(`data-${language}-${suffix}`);
        if (value !== null && el.getAttribute(attr) !== value) el.setAttribute(attr, value);
      }
    });
  }
  let scheduled = false;
  function schedule() {
    if (scheduled) return;
    scheduled = true;
    queueMicrotask(() => { scheduled = false; update(); });
  }
  document.addEventListener('DOMContentLoaded', () => {
    update();
    new MutationObserver(records => {
      if (records.some(r => Array.from(r.addedNodes).some(n => n.nodeType === 1))) schedule();
    }).observe(document.body, { childList: true, subtree: true });
  });
  document.addEventListener('click', e => {
    if (e.target.closest('#langBtn,[data-lang-toggle]')) setTimeout(schedule, 40);
  });
  window.addEventListener('storage', e => { if (e.key === 'crm-lang') schedule(); });
  window.addEventListener('crm:language-changed', schedule);
  window.crmStage100I18n = { update };
})();
