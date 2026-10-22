(() => {
  'use strict';
  // No MutationObserver: applying translations never recursively triggers itself.
  const lang = () => localStorage.getItem('crm-lang') === 'en' ? 'en' : 'fa';
  const labels = {
    fa: {required:'پر کردن این فیلد الزامی است.',email:'لطفاً یک ایمیل معتبر وارد کنید.',range:'مقدار واردشده خارج از محدوده مجاز است.',delete:'آیا از حذف این مورد اطمینان دارید؟',save:'ذخیره',invalid:'لطفاً اطلاعات فرم را بررسی کنید.'},
    en: {required:'This field is required.',email:'Enter a valid email address.',range:'Value is outside the allowed range.',delete:'Are you sure you want to delete this item?',save:'Save',invalid:'Please check the form fields.'}
  };
  function translate() {
    const l=lang();
    document.querySelectorAll('[data-stage60-fa][data-stage60-en]').forEach(el => {
      const value=el.getAttribute('data-stage60-'+l);
      if(value!==null && el.textContent!==value) el.textContent=value;
    });
    document.querySelectorAll('input,textarea,select').forEach(el => {
      if(el.validationMessage && el.dataset.stage60Validated==='1') el.setCustomValidity('');
    });
  }
  document.addEventListener('invalid', e => {
    const el=e.target;
    if(!(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement || el instanceof HTMLSelectElement)) return;
    // Native validity must be checked before setting a custom message.
    const validity=el.validity, dict=labels[lang()];
    const message=validity.valueMissing?dict.required:validity.typeMismatch?dict.email:
      (validity.rangeUnderflow||validity.rangeOverflow)?dict.range:'';
    if(message){el.setCustomValidity(message);el.dataset.stage60Validated='1';}
  },true);
  document.addEventListener('input',e=>{
    const el=e.target;
    if(el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement || el instanceof HTMLSelectElement){
      if(el.dataset.stage60Validated==='1'){el.setCustomValidity('');delete el.dataset.stage60Validated;}
    }
  },true);
  document.addEventListener('submit',e=>{
    const form=e.target;
    if(!(form instanceof HTMLFormElement)) return;
    if(form.dataset.stage60Confirm==='delete' && !window.confirm(labels[lang()].delete)) e.preventDefault();
  },true);
  document.addEventListener('DOMContentLoaded',()=>{
    translate();
    document.getElementById('langBtn')?.addEventListener('click',()=>queueMicrotask(translate));
  });
  window.addEventListener('crm:language-changed',translate);
  window.addEventListener('storage',e=>{if(e.key==='crm-lang')translate();});
})();
