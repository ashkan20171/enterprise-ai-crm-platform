(() => {
  'use strict';
  const translations = {
    'Qualification':['ارزیابی اولیه','Qualification'], 'Proposal':['پیشنهاد','Proposal'],
    'Negotiation':['مذاکره','Negotiation'], 'Won':['موفق','Won'], 'Lost':['ناموفق','Lost'],
    'Active':['فعال','Active'], 'Inactive':['غیرفعال','Inactive'],
    'Prospect':['مشتری بالقوه','Prospect'], 'All status':['همه وضعیت‌ها','All statuses'],
    'Enterprise':['سازمانی','Enterprise'], 'Startup':['استارتاپ','Startup'],
    'SMB':['کسب‌وکار کوچک و متوسط','SMB'],
    'Open':['باز','Open'], 'InProgress':['در حال انجام','In progress'],
    'Waiting':['در انتظار','Waiting'], 'Resolved':['حل‌شده','Resolved'],
    'Closed':['بسته‌شده','Closed'], 'Reopened':['بازگشایی‌شده','Reopened'],
    'Critical':['بحرانی','Critical'], 'High':['بالا','High'],
    'Medium':['متوسط','Medium'], 'Low':['کم','Low'],
    'General':['عمومی','General'], 'Technical':['فنی','Technical'],
    'Billing':['مالی','Billing'], 'FeatureRequest':['درخواست قابلیت','Feature request'],
    'New':['جدید','New'], 'Qualified':['واجد شرایط','Qualified'],
    'Contacted':['تماس گرفته‌شده','Contacted'], 'Converted':['تبدیل‌شده','Converted']
  };
  const language = () => localStorage.getItem('crm-lang') === 'en' ? 'en' : 'fa';
  function apply() {
    const l=language(), idx=l==='en'?1:0;
    if(document.documentElement.lang!==l) document.documentElement.lang=l;
    const dir=l==='en'?'ltr':'rtl';
    if(document.documentElement.dir!==dir) document.documentElement.dir=dir;
    document.querySelectorAll('[data-stage58-code]').forEach(el=>{
      const item=translations[el.dataset.stage58Code];
      if(item && el.textContent!==item[idx]) el.textContent=item[idx];
    });
    // Select options preserve their underlying values; only visible labels are localized.
    document.querySelectorAll('select option').forEach(option=>{
      const key=option.dataset.stage58Code || option.value || option.textContent.trim();
      const item=translations[key];
      if(!item) return;
      option.dataset.stage58Code=key;
      if(option.textContent!==item[idx]) option.textContent=item[idx];
    });
    document.querySelectorAll('[data-stage58-fa][data-stage58-en]').forEach(el=>{
      const v=el.getAttribute('data-stage58-'+l);
      if(v!==null && el.textContent!==v) el.textContent=v;
    });
    document.querySelectorAll('[data-stage59-fa][data-stage59-en]').forEach(el=>{
      const v=el.getAttribute('data-stage59-'+l);
      if(v!==null && el.textContent!==v) el.textContent=v;
    });
    document.querySelectorAll('[data-stage59-placeholder-fa][data-stage59-placeholder-en]').forEach(el=>{
      const v=el.getAttribute('data-stage59-placeholder-'+l);
      if(v!==null && el.placeholder!==v) el.placeholder=v;
    });
    document.querySelectorAll('[data-stage58-placeholder-fa][data-stage58-placeholder-en]').forEach(el=>{
      const v=el.getAttribute('data-stage58-placeholder-'+l);
      if(v!==null && el.placeholder!==v) el.placeholder=v;
    });
  }
  let pending=false;
  const queue=()=>{if(pending)return;pending=true;requestAnimationFrame(()=>{pending=false;apply()})};
  document.addEventListener('DOMContentLoaded',()=>{
    apply();
    document.getElementById('langBtn')?.addEventListener('click',()=>setTimeout(queue,0));
    // Observe only inserted elements; never observe attributes/text mutated by this translator.
    const content=document.querySelector('.content');
    if(content)new MutationObserver(queue).observe(content,{childList:true,subtree:true});
  });
  window.addEventListener('storage',e=>{if(e.key==='crm-lang')queue()});
  window.addEventListener('crm:language-changed',queue);
})();
