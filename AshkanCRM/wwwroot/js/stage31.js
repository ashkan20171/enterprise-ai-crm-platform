/* Stage 31: accessible, deterministic navigation localization. */
(() => {
  'use strict';
  const names = {
    DuplicateDetection:['تشخیص موارد تکراری','Duplicate Detection'],Integrations:['مرکز یکپارچه‌سازی','Integration Hub'],
    AiOperations:['عملیات هوش مصنوعی','AI Operations'],AiCopilot:['دستیار هوش مصنوعی','AI Copilot'],
    CustomerIntelligence:['هوشمندی مشتریان','Customer Intelligence'],LeadRouting:['تخصیص سرنخ‌ها','Lead Routing'],
    SecurityOperations:['عملیات امنیتی','Security Operations'],DataGovernance:['حاکمیت داده','Data Governance'],
    WorkflowStudio:['طراح گردش کار','Workflow Studio'],ExecutiveCockpit:['داشبورد مدیریتی','Executive Cockpit'],
    RevenueIntelligence:['هوشمندی درآمد','Revenue Intelligence'],GlobalSearch:['جستجوی سراسری','Global Search'],
    WinLossAnalytics:['تحلیل برد و باخت','Win/Loss Analytics'],IntegrationMonitor:['پایش یکپارچه‌سازی','Integration Monitor'],
    CustomerRecycleBin:['سطل بازیافت مشتریان','Customer Recycle Bin'],AuditLog:['گزارش رویدادها','Audit Log'],
    Customers:['مشتریان','Customers'],Leads:['سرنخ‌ها','Leads'],Deals:['معاملات','Deals'],Tasks:['وظایف','Tasks'],
    Reports:['گزارش‌ها','Reports'],Settings:['تنظیمات','Settings'],Teams:['تیم‌ها','Teams'],Users:['کاربران','Users'],
    Notifications:['اعلان‌ها','Notifications'],Knowledge:['پایگاه دانش','Knowledge'],Forecast:['پیش‌بینی فروش','Forecast']
  };
  const faText = {'Duplicate Detection':'تشخیص موارد تکراری','Integration Hub':'مرکز یکپارچه‌سازی','AI Operations':'عملیات هوش مصنوعی','Customer Intelligence':'هوشمندی مشتریان','Lead Routing':'تخصیص سرنخ‌ها'};
  const enText = Object.fromEntries(Object.entries(faText).map(([en,fa])=>[fa,en]));
  function isFa(){return document.documentElement.lang?.toLowerCase().startsWith('fa') || document.documentElement.dir==='rtl';}
  function translate(){
    const fa=isFa();
    document.querySelectorAll('.sidebar a[data-page]').forEach(a=>{
      const value=names[a.dataset.page]; if(!value)return;
      const span=a.querySelector('span');if(!span)return;
      // Respect the primary dictionary when it owns the translation.
      if(span.hasAttribute('data-i18n'))return;
      span.textContent=value[fa?0:1];
    });
    document.querySelectorAll('[data-stage31-label]').forEach(el=>{
      const pair=names[el.dataset.stage31Label];if(pair)el.textContent=pair[fa?0:1];
    });
    document.querySelectorAll('.content h1,.content h2').forEach(el=>{
      if(el.children.length || el.hasAttribute('data-i18n'))return;
      const t=el.textContent.trim();const translation=(fa?faText:enText)[t];
      if(translation)el.textContent=translation;
    });
  }
  let queued=false;
  function schedule(){if(queued)return;queued=true;requestAnimationFrame(()=>{queued=false;translate();});}
  document.addEventListener('DOMContentLoaded',()=>{
    translate();
    const observer=new MutationObserver(schedule);
    observer.observe(document.documentElement,{attributes:true,attributeFilter:['lang','dir']});
    document.getElementById('langBtn')?.addEventListener('click',()=>setTimeout(translate,0));
    document.addEventListener('crm:languagechange',schedule);
  });
})();
