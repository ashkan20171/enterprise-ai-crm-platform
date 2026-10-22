/* Stage 28: progressive bilingual localization for legacy MVC views.
   Exact labels only; never translate user-provided content or editable controls. */
(() => {
  'use strict';
  const translations = {
    'AI Operations':'عملیات هوش مصنوعی','Customer Intelligence':'هوشمندی مشتریان','Next Best Action':'اقدام پیشنهادی بعدی',
    'Integration Monitor':'پایش یکپارچه‌سازی','Team Workspace':'فضای کاری تیم','Sales Pipeline':'قیف فروش',
    'Live Operations':'عملیات زنده','Security Operations':'عملیات امنیتی','Data Governance':'حاکمیت داده',
    'Lead Routing':'تخصیص سرنخ','Health Center':'مرکز سلامت سامانه','Workflow Studio':'استودیوی گردش کار',
    'API & Webhooks':'رابط برنامه‌نویسی و وب‌هوک‌ها','Open':'باز','Closed':'بسته','Pending':'در انتظار',
    'Active':'فعال','Inactive':'غیرفعال','Save':'ذخیره','Cancel':'انصراف','Search':'جستجو',
    'Filter':'فیلتر','Export':'خروجی','Create':'ایجاد','Edit':'ویرایش','Delete':'حذف',
    'Details':'جزئیات','Settings':'تنظیمات','Dashboard':'داشبورد','Customers':'مشتریان',
    'Leads':'سرنخ‌ها','Deals':'فرصت‌های فروش','Tasks':'وظایف','Notifications':'اعلان‌ها',
    'Reports':'گزارش‌ها','Loading...':'در حال بارگذاری...','Overview':'نمای کلی',
    'Analytics':'تحلیل‌ها','Activity':'فعالیت','Activities':'فعالیت‌ها','Calendar':'تقویم',
    'Users':'کاربران','Teams':'تیم‌ها','Roles':'نقش‌ها','Permissions':'مجوزها',
    'Security':'امنیت','Automation':'اتوماسیون','Integrations':'یکپارچه‌سازی‌ها',
    'Knowledge Base':'پایگاه دانش','Audit Log':'گزارش ممیزی','Login Audit':'ممیزی ورود',
    'Back':'بازگشت','Next':'بعدی','Previous':'قبلی','Submit':'ثبت',
    'Update':'به‌روزرسانی','Refresh':'تازه‌سازی','Apply':'اعمال','Reset':'بازنشانی',
    'Clear':'پاک‌کردن','Close':'بستن','View':'مشاهده','View all':'مشاهده همه',
    'Add':'افزودن','Add New':'افزودن مورد جدید','New Customer':'مشتری جدید',
    'New Lead':'سرنخ جدید','New Deal':'فرصت جدید','No data':'داده‌ای موجود نیست',
    'No results found':'نتیجه‌ای پیدا نشد','Success':'موفق','Error':'خطا',
    'Warning':'هشدار','Today':'امروز','This week':'این هفته','This month':'این ماه',
    'All':'همه','My records':'رکوردهای من','My team':'تیم من',
    'Pipeline':'قیف فروش','Forecast':'پیش‌بینی فروش','Revenue':'درآمد',
    'Conversion Rate':'نرخ تبدیل','Customer Health':'سلامت مشتری','Risk Level':'سطح ریسک',
    'High Risk':'ریسک بالا','Low Risk':'ریسک پایین','Medium Risk':'ریسک متوسط',
    'Sign in':'ورود','Sign out':'خروج','Register':'ثبت‌نام','Password':'رمز عبور',
    'Email':'ایمیل','Username':'نام کاربری','Remember me':'مرا به خاطر بسپار',
    'Required':'الزامی','Optional':'اختیاری','Name':'نام','Status':'وضعیت',
    'Owner':'مالک','Priority':'اولویت','Description':'توضیحات','Actions':'عملیات',
    'Created at':'تاریخ ایجاد','Updated at':'آخرین به‌روزرسانی','Due date':'موعد انجام'
  };
  const excluded = 'script,style,code,pre,textarea,input,select,option,[contenteditable],.user-content,[data-no-translate]';
  const textOriginal = new WeakMap();
  const attributeOriginal = new WeakMap();
  const reverse = Object.fromEntries(Object.entries(translations).map(([en,fa]) => [fa,en]));
  const currentLanguage = () => localStorage.getItem('crm-lang') === 'en' ? 'en' : 'fa';
  function translate(value, language) {
    const trimmed = value.trim();
    const target = language === 'fa' ? translations[trimmed] : reverse[trimmed];
    return target ? value.replace(trimmed, target) : value;
  }
  function apply(root = document.body) {
    if (!root) return;
    const language = currentLanguage();
    if (document.documentElement.lang !== language) document.documentElement.lang = language;
    const desiredDir = language === 'fa' ? 'rtl' : 'ltr';
    if (document.documentElement.dir !== desiredDir) document.documentElement.dir = desiredDir;
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let node;
    while ((node = walker.nextNode())) {
      if (node.parentElement?.closest(excluded)) continue;
      if (!textOriginal.has(node)) textOriginal.set(node, node.nodeValue);
      const original = textOriginal.get(node);
      const target = translate(original, language);
      if (node.nodeValue !== target) node.nodeValue = target;
    }
    const elements = [root, ...root.querySelectorAll('[placeholder],[title],[aria-label],[data-bs-original-title]')];
    for (const el of elements) {
      if (!(el instanceof Element) || el.closest('[data-no-translate],.user-content')) continue;
      if (!attributeOriginal.has(el)) attributeOriginal.set(el, {});
      const saved = attributeOriginal.get(el);
      for (const attr of ['placeholder','title','aria-label','data-bs-original-title']) {
        if (!el.hasAttribute(attr)) continue;
        if (!(attr in saved)) saved[attr] = el.getAttribute(attr);
        const value = translate(saved[attr], language);
        if (el.getAttribute(attr) !== value) el.setAttribute(attr, value);
      }
    }
  }
  let pending = false;
  function schedule() {
    if (pending) return;
    pending = true;
    queueMicrotask(() => { pending = false; apply(); });
  }
  document.addEventListener('DOMContentLoaded', () => {
    apply();
    document.addEventListener('click', event => {
      if (event.target.closest('#langBtn,[data-lang-toggle]')) setTimeout(schedule, 0);
    });
    new MutationObserver(records => {
      if (records.some(record => record.addedNodes.length || record.removedNodes.length)) schedule();
    }).observe(document.body, { childList: true, subtree: true });
  });
  window.crmLocalizationRefresh = schedule;
})();
