/* Stage 99: global bilingual consistency layer.
 * Exact-label translations only. Never translate free-form/user-generated content.
 * Server culture is synchronized on language change so Razor-rendered pages,
 * validation messages and date/number formatting follow the selected language.
 */
(() => {
  'use strict';
  const pairs = [
    ['Welcome','خوش آمدید'],['Home Page','صفحه اصلی'],['Privacy Policy','سیاست حفظ حریم خصوصی'],
    ['Use this page to detail your site\'s privacy policy.','از این صفحه برای توضیح سیاست حفظ حریم خصوصی سایت استفاده کنید.'],
    ['An error occurred while processing your request.','هنگام پردازش درخواست شما خطایی رخ داد.'],['Request ID','شناسه درخواست'],
    ['Development Mode','حالت توسعه'],['Error.','خطا.'],['Customer Intelligence Platform','پلتفرم هوشمندی مشتریان'],
    ['Enterprise CRM','سی‌آرام سازمانی'],['Customer Experience','تجربه مشتری'],['Workspace','فضای کاری'],
    ['Command Center','مرکز فرمان'],['Admin Dashboard','داشبورد مدیریت'],['Administration','مدیریت'],
    ['Manage Users','مدیریت کاربران'],['Users & Permissions','کاربران و مجوزها'],['Teams & Departments','تیم‌ها و واحدها'],
    ['User Sessions','نشست‌های کاربران'],['Feature Flags','پرچم‌های قابلیت'],['Access Studio','استودیوی دسترسی'],
    ['Access Health','سلامت دسترسی'],['Permission Matrix','ماتریس مجوزها'],['Security Center','مرکز امنیت'],
    ['Security Operations','عملیات امنیتی'],['Lead Routing','تخصیص سرنخ'],['Data Governance','حاکمیت داده'],
    ['Data Management','مدیریت داده'],['Enterprise Data Core','هسته داده سازمانی'],['Recycle Bin','سطل بازیافت'],
    ['Data Quality','کیفیت داده'],['Duplicate Detection','شناسایی موارد تکراری'],['Integration Hub','مرکز یکپارچه‌سازی'],
    ['Integration Monitor','پایش یکپارچه‌سازی'],['Audit Log','گزارش ممیزی'],['Reports & Analytics','گزارش‌ها و تحلیل'],
    ['Executive Cockpit','داشبورد مدیریتی اجرایی'],['Approvals','تأییدیه‌ها'],['Sales Forecast','پیش‌بینی فروش'],
    ['Revenue Intelligence','هوش درآمد'],['Win/Loss Analytics','تحلیل برد و باخت'],['Operational Playbooks','راهنماهای عملیاتی'],
    ['Support Tickets','تیکت‌های پشتیبانی'],['Support Workspace','فضای کاری پشتیبانی'],['Ticket assignment queue','صف تخصیص تیکت'],
    ['SLA configuration','تنظیمات SLA'],['SLA Board','تابلوی SLA'],['Agent Performance','عملکرد کارشناسان'],
    ['Assignment Queue','صف تخصیص'],['Escalation Report','گزارش ارجاع'],['Knowledge Base','پایگاه دانش'],
    ['Customer 360','نمای ۳۶۰ درجه مشتری'],['Customer Intelligence','هوشمندی مشتریان'],['AI Operations','عملیات هوش مصنوعی'],
    ['AI Insights','بینش‌های هوش مصنوعی'],['AI Agent Studio','استودیوی عامل‌های هوش مصنوعی'],['CRM AI Copilot','دستیار هوشمند CRM'],
    ['Next Best Action','اقدام پیشنهادی بعدی'],['Smart Scoring','امتیازدهی هوشمند'],['Saved Views','نماهای ذخیره‌شده'],
    ['New deal','فرصت فروش جدید'],['New Deal','فرصت فروش جدید'],['New customer','مشتری جدید'],['New Customer','مشتری جدید'],
    ['New lead','سرنخ جدید'],['New Lead','سرنخ جدید'],['New task','کار جدید'],['New Task','کار جدید'],
    ['Create new','ایجاد مورد جدید'],['Create','ایجاد'],['Save changes','ذخیره تغییرات'],['Save','ذخیره'],
    ['Cancel','انصراف'],['Search','جستجو'],['Search menu','جستجوی منو'],['Clear search','پاک کردن جستجو'],
    ['Filter','فیلتر'],['Apply filters','اعمال فیلترها'],['Reset filters','بازنشانی فیلترها'],['Export to Excel','خروجی اکسل'],
    ['Export Excel','خروجی اکسل'],['Download Excel','دانلود اکسل'],['Export','خروجی گرفتن'],['Print / Save PDF','چاپ / ذخیره PDF'],
    ['Print','چاپ'],['Refresh','تازه‌سازی'],['Update','به‌روزرسانی'],['Apply','اعمال'],['Reset','بازنشانی'],
    ['Clear','پاک کردن'],['Close','بستن'],['Back','بازگشت'],['Next','بعدی'],['Previous','قبلی'],['Submit','ثبت'],
    ['Add','افزودن'],['Add New','افزودن مورد جدید'],['Edit','ویرایش'],['Delete','حذف'],['Remove','حذف'],
    ['Restore','بازیابی'],['Details','جزئیات'],['View details','مشاهده جزئیات'],['View all','مشاهده همه'],
    ['No data','داده‌ای وجود ندارد'],['No data available','داده‌ای برای نمایش وجود ندارد'],['No results found','نتیجه‌ای پیدا نشد'],
    ['No records found','رکوردی پیدا نشد'],['Nothing to display','موردی برای نمایش وجود ندارد'],['Loading...','در حال بارگذاری...'],
    ['Please wait...','لطفاً منتظر بمانید...'],['Success','موفقیت'],['Error','خطا'],['Warning','هشدار'],['Information','اطلاعات'],
    ['Today','امروز'],['Yesterday','دیروز'],['This week','این هفته'],['This month','این ماه'],['Last month','ماه گذشته'],
    ['This year','امسال'],['Last year','سال گذشته'],['All','همه'],['All statuses','همه وضعیت‌ها'],['Status','وضعیت'],
    ['Owner','مالک'],['Priority','اولویت'],['Description','توضیحات'],['Actions','عملیات'],['Name','نام'],['Company','شرکت'],
    ['Email','ایمیل'],['Phone','تلفن'],['Username','نام کاربری'],['Password','رمز عبور'],['Confirm password','تأیید رمز عبور'],
    ['Title','عنوان'],['Subject','موضوع'],['Category','دسته‌بندی'],['Content','محتوا'],['Created at','تاریخ ایجاد'],
    ['Updated at','آخرین به‌روزرسانی'],['Due date','موعد انجام'],['Close date','تاریخ بسته‌شدن'],['Created','ایجادشده'],
    ['Active','فعال'],['Inactive','غیرفعال'],['Enabled','فعال'],['Disabled','غیرفعال'],['Open','باز'],['Closed','بسته‌شده'],
    ['Pending','در انتظار'],['In progress','در حال انجام'],['Resolved','حل‌شده'],['Reopened','بازگشایی‌شده'],
    ['Critical','بحرانی'],['High','بالا'],['Medium','متوسط'],['Low','پایین'],['Healthy','سالم'],['Overdue','عقب‌افتاده'],
    ['Qualification','ارزیابی اولیه'],['Proposal','پیشنهاد'],['Negotiation','مذاکره'],['Won','موفق'],['Lost','ناموفق'],
    ['Prospect','مشتری بالقوه'],['Qualified','واجد شرایط'],['Contacted','تماس‌گرفته‌شده'],['Converted','تبدیل‌شده'],
    ['Enterprise','سازمانی'],['Startup','استارتاپ'],['SMB','کسب‌وکار کوچک و متوسط'],['General','عمومی'],['Technical','فنی'],
    ['Billing','صورتحساب'],['Feature request','درخواست قابلیت'],['All status','همه وضعیت‌ها'],['Pipeline','قیف فروش'],
    ['Forecast','پیش‌بینی'],['Revenue','درآمد'],['Conversion Rate','نرخ تبدیل'],['Risk Level','سطح ریسک'],
    ['High Risk','ریسک بالا'],['Low Risk','ریسک پایین'],['Medium Risk','ریسک متوسط'],['Sign in','ورود'],['Sign out','خروج'],
    ['Register','ثبت‌نام'],['Remember me','مرا به خاطر بسپار'],['Required','الزامی'],['Optional','اختیاری'],
    ['My records','رکوردهای من'],['My team','تیم من'],['Overview','نمای کلی'],['Analytics','تحلیل‌ها'],['Activity','فعالیت'],
    ['Activities','فعالیت‌ها'],['Calendar','تقویم'],['Users','کاربران'],['Teams','تیم‌ها'],['Roles','نقش‌ها'],
    ['Permissions','مجوزها'],['Security','امنیت'],['Automation','اتوماسیون'],['Integrations','یکپارچه‌سازی‌ها'],
    ['Login Audit','ممیزی ورود'],['Back to customers','بازگشت به مشتریان'],['Activity timeline','خط زمانی فعالیت‌ها'],
    ['No activity has been recorded yet.','هنوز فعالیتی ثبت نشده است.'],['Recycle bin is empty.','سطل بازیافت خالی است.'],
    ['Default report period (days)','بازه پیش‌فرض گزارش (روز)'],['Save as PDF','ذخیره به‌صورت PDF'],
    ['Department','واحد سازمانی'],['Language','زبان'],['Appearance','ظاهر'],['Light','روشن'],['Dark','تاریک'],['System','مطابق سیستم'],
    ['Compact mode','حالت فشرده'],['Favorites','علاقه‌مندی‌ها'],['Quick access','دسترسی سریع'],['Quick Add','ایجاد سریع'],
    ['Search customers, deals, tasks...','جستجو در مشتریان، فرصت‌ها و کارها...'],['Search customers, leads, deals and tasks...','جستجو در مشتریان، سرنخ‌ها، فرصت‌ها و کارها...'],
    ['New ticket','تیکت جدید'],['Ticket #','شماره تیکت'],['Requester','درخواست‌کننده'],['Assign to (username)','تخصیص به (نام کاربری)'],
    ['Update ticket','به‌روزرسانی تیکت'],['PDF / PNG / JPEG / TXT · Max 2 MB','PDF / PNG / JPEG / TXT · حداکثر ۲ مگابایت'],
    ['Automatic','خودکار'],['Manual','دستی'],['Setup required','نیازمند راه‌اندازی'],['Access Review','بازبینی دسترسی'],
    ['Least Privilege','حداقل سطح دسترسی'],['Default deny','رد پیش‌فرض'],['Own / Team / All','خودم / تیم / همه'],
    ['Role scoped','محدود به نقش'],['Human approval','تأیید انسانی'],['Live Operations','عملیات زنده'],
    ['Data lifecycle','چرخه عمر داده'],['Production-oriented persistence foundation','زیرساخت ماندگاری داده مناسب محیط عملیاتی'],
    ['Optimistic Concurrency','هم‌زمانی خوش‌بینانه'],['Server Audit','ممیزی سمت سرور'],['Search-ready','آماده جستجو'],
    ['Customer duplicates','مشتریان تکراری'],['Lead duplicates','سرنخ‌های تکراری'],['Growth Center','مرکز رشد'],
    ['PERSISTENT NOTIFICATION CENTER','مرکز اعلان‌های پایدار'],['CONNECTED CRM','CRM متصل'],['API Keys','کلیدهای API'],
    ['Secret Hygiene','مدیریت امن اسرار'],['Retry Policy','سیاست تلاش مجدد'],['Attempts','تلاش‌ها'],['Endpoint','نقطه پایانی'],['Event','رویداد']
  ];
  const toFa = new Map(pairs);
  const toEn = new Map(pairs.map(([en, fa]) => [fa, en]));
  const excluded = 'script,style,code,pre,textarea,input,select,option,[contenteditable],[data-no-translate],.user-content';
  const lang = () => localStorage.getItem('crm-lang') === 'en' ? 'en' : 'fa';
  function swap(value, language) {
    if (!value) return value;
    const trimmed = value.trim();
    const translated = (language === 'en' ? toEn : toFa).get(trimmed);
    return translated ? value.replace(trimmed, translated) : value;
  }
  function apply(root = document.body) {
    if (!root) return;
    const language = lang();
    const direction = language === 'en' ? 'ltr' : 'rtl';
    document.documentElement.lang = language;
    document.documentElement.dir = direction;
    document.documentElement.dataset.language = language;
    document.body?.setAttribute('dir', direction);
    document.querySelector('meta[name="crm-culture"]')?.setAttribute('content', language);
    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    let node;
    while ((node = walker.nextNode())) {
      const parent = node.parentElement;
      if (!parent || parent.closest(excluded)) continue;
      if (parent.closest('[data-fa][data-en],[data-stage58-fa][data-stage58-en],[data-stage59-fa][data-stage59-en],[data-i18n]')) continue;
      node.nodeValue = swap(node.nodeValue, language);
    }
    for (const element of root.querySelectorAll('[placeholder],[title],[aria-label]')) {
      if (element.closest('[data-no-translate],.user-content')) continue;
      for (const attr of ['placeholder', 'title', 'aria-label']) {
        if (element.hasAttribute(attr)) element.setAttribute(attr, swap(element.getAttribute(attr), language));
      }
    }
    for (const option of root.querySelectorAll('select option')) {
      if (option.dataset.stage99OriginalEn && option.dataset.stage99OriginalFa) {
        const target = language === 'en' ? option.dataset.stage99OriginalEn : option.dataset.stage99OriginalFa;
        if (option.textContent !== target) option.textContent = target;
      } else {
        const current = option.textContent.trim();
        const fa = toFa.get(current) || current;
        const en = toEn.get(current) || current;
        if (fa !== en) {
          option.dataset.stage99OriginalEn = en;
          option.dataset.stage99OriginalFa = fa;
          option.textContent = language === 'en' ? en : fa;
        }
      }
    }
  }
  function syncServerCultureAndReload() {
    const language = lang();
    const culture = language === 'en' ? 'en-US' : 'fa-IR';
    const encoded = encodeURIComponent(`c=${culture}|uic=${culture}`);
    const expected = `.AspNetCore.Culture=${encoded}`;
    const current = document.cookie.split('; ').find(x => x.startsWith('.AspNetCore.Culture='));
    if (current !== expected) {
      document.cookie = `${expected}; Path=/; SameSite=Lax; Max-Age=31536000`;
      // Reload once after the culture cookie changes so all Razor/server-generated strings use it.
      window.location.reload();
    } else {
      window.dispatchEvent(new Event('crm:language-changed'));
      apply();
    }
  }
  let queued = false;
  const schedule = () => {
    if (queued) return;
    queued = true;
    queueMicrotask(() => { queued = false; apply(); });
  };
  document.addEventListener('DOMContentLoaded', () => {
    const startupCulture = lang() === 'en' ? 'en-US' : 'fa-IR';
    const startupCookie = `.AspNetCore.Culture=${encodeURIComponent(`c=${startupCulture}|uic=${startupCulture}`)}`;
    const existingCookie = document.cookie.split('; ').find(x => x.startsWith('.AspNetCore.Culture='));
    if (existingCookie !== startupCookie) {
      document.cookie = `${startupCookie}; Path=/; SameSite=Lax; Max-Age=31536000`;
      window.location.reload();
      return;
    }
    apply();
    document.getElementById('langBtn')?.addEventListener('click', () => setTimeout(syncServerCultureAndReload, 25));
    document.addEventListener('click', e => {
      if (e.target.closest('[data-lang-toggle]') && !e.target.closest('#langBtn')) setTimeout(syncServerCultureAndReload, 25);
    });
    new MutationObserver(records => {
      // Ignore text-node churn from translators; only newly inserted elements
      // can introduce untranslated UI and require another pass.
      const hasElementChanges = records.some(r =>
        Array.from(r.addedNodes).some(n => n.nodeType === Node.ELEMENT_NODE) ||
        Array.from(r.removedNodes).some(n => n.nodeType === Node.ELEMENT_NODE)
      );
      if (hasElementChanges) schedule();
    }).observe(document.body, { childList: true, subtree: true });
  });
  window.addEventListener('storage', e => { if (e.key === 'crm-lang') schedule(); });
  window.addEventListener('crm:language-changed', schedule);
  window.crmStage99Localization = { apply, refresh: schedule };
})();
