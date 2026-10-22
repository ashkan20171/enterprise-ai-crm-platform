/* Stage 89: safe, allowlisted per-user appearance preferences. */
(() => {
  'use strict';
  const KEY = 'ashkancrm.appearance.v1';
  const defaults = { accent: 'indigo', fontSize: 'normal', appearance: 'system' };
  const allowed = { accent: ['indigo', 'teal', 'amber'], fontSize: ['normal', 'large'], appearance: ['system', 'light', 'dark'] };
  const normalize = source => Object.fromEntries(Object.keys(defaults).map(key => [key, allowed[key].includes(source?.[key]) ? source[key] : defaults[key]]));
  const read = () => { try { return normalize(JSON.parse(localStorage.getItem(KEY) || '{}')); } catch (_) { return { ...defaults }; } };
  let current = read();
  const apply = (source, notify = false) => {
    current = normalize(source);
    const root = document.documentElement;
    root.dataset.crmAccent = current.accent;
    root.dataset.crmFontSize = current.fontSize;
    root.dataset.crmAppearance = current.appearance;
    try { localStorage.setItem(KEY, JSON.stringify(current)); } catch (_) {}
    document.querySelectorAll('[data-crm89-setting]').forEach(select => { select.value = current[select.dataset.crm89Setting]; });
    if (notify) window.dispatchEvent(new Event('crm-sidebar-preferences-changed'));
  };
  window.crm89AppearanceState = () => ({ ...current });
  window.crm89ApplyAppearance = data => apply(data);
  const nav = document.querySelector('.sidebar > nav');
  if (!nav) { apply(current); return; }
  const isEn = document.documentElement.lang === 'en';
  const panel = document.createElement('details');
  panel.className = 'crm89-panel';
  const summary = document.createElement('summary');
  summary.textContent = isEn ? 'Appearance settings' : 'تنظیمات ظاهر';
  panel.append(summary);
  const settings = [
    ['accent', isEn ? 'Accent color' : 'رنگ اصلی', [['indigo', isEn ? 'Indigo' : 'نیلی'], ['teal', isEn ? 'Teal' : 'فیروزه‌ای'], ['amber', isEn ? 'Amber' : 'کهربایی']]],
    ['fontSize', isEn ? 'Text size' : 'اندازه متن', [['normal', isEn ? 'Normal' : 'معمولی'], ['large', isEn ? 'Large' : 'درشت']]],
    ['appearance', isEn ? 'Theme preference' : 'حالت نمایش', [['system', isEn ? 'System' : 'سیستم'], ['light', isEn ? 'Light' : 'روشن'], ['dark', isEn ? 'Dark' : 'تیره']]]
  ];
  settings.forEach(([key, label, options]) => {
    const wrapper = document.createElement('label');
    wrapper.className = 'crm89-field';
    wrapper.append(document.createTextNode(label));
    const select = document.createElement('select');
    select.dataset.crm89Setting = key;
    options.forEach(([value, name]) => { const option = document.createElement('option'); option.value = value; option.textContent = name; select.append(option); });
    select.addEventListener('change', () => apply({ ...current, [key]: select.value }, true));
    wrapper.append(select); panel.append(wrapper);
  });
  const reset = document.createElement('button');
  reset.type = 'button';
  reset.className = 'crm89-reset';
  reset.textContent = isEn ? 'Reset appearance' : 'بازنشانی ظاهر';
  reset.addEventListener('click', () => apply(defaults, true));
  panel.append(reset);
  nav.prepend(panel);
  apply(current);
})();
