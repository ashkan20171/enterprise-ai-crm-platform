/* Stage 29: additive keyboard navigation and accessible status messages. */
(()=>{'use strict';
 const root=document.documentElement;
 const t=(fa,en)=>root.lang==='en'?en:fa;
 const skip=document.createElement('a');skip.href='#crm-main';skip.className='ux-skip-link';skip.textContent=t('رفتن به محتوای اصلی','Skip to main content');document.body.prepend(skip);
 const main=document.querySelector('main');if(main){main.id='crm-main';main.tabIndex=-1;}
 const status=document.createElement('div');status.className='ux-status';status.setAttribute('role','status');status.setAttribute('aria-live','polite');status.hidden=true;document.body.append(status);
 let hide;
 function announce(message){status.textContent=message;status.hidden=false;clearTimeout(hide);hide=setTimeout(()=>status.hidden=true,3600)}
 document.addEventListener('keydown',e=>{
  if(e.key==='Escape') {const sidebar=document.querySelector('.sidebar.open');if(sidebar){sidebar.classList.remove('open');announce(t('منو بسته شد','Menu closed'));}}
 });
 const menu=document.querySelector('.mobile-menu');if(menu){menu.setAttribute('aria-label',t('باز کردن منو','Open navigation'));menu.setAttribute('aria-expanded','false');menu.addEventListener('click',()=>{const open=document.querySelector('.sidebar')?.classList.contains('open');menu.setAttribute('aria-expanded',String(!!open));});}
 const lang=document.querySelector('#langBtn');if(lang){lang.setAttribute('aria-label',t('تغییر زبان','Change language'));}
 const theme=document.querySelector('#themeBtn');if(theme){theme.setAttribute('aria-label',t('تغییر پوسته','Change theme'));}
 const observer=new MutationObserver(()=>{skip.textContent=t('رفتن به محتوای اصلی','Skip to main content');if(menu)menu.setAttribute('aria-label',t('باز کردن منو','Open navigation'));});
 observer.observe(root,{attributes:true,attributeFilter:['lang','dir']});
})();
