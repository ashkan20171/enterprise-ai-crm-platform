(function(){
  const status=document.getElementById('support-live-status');
  if(!status || !window.signalR) return;
  const connection=new signalR.HubConnectionBuilder().withUrl('/hubs/support').withAutomaticReconnect().build();
  const setStatus=(value)=>{status.textContent=value;};
  connection.on('SupportChanged',function(evt){
    if(!evt || typeof evt.id!=='number')return;
    setStatus(document.documentElement.lang==='fa'?'تغییر جدید در تیکت‌ها؛ برای مشاهده تازه‌سازی کنید':'Ticket update available — refresh to view');
    const btn=document.getElementById('support-refresh');if(btn)btn.hidden=false;
  });
  connection.onreconnecting(()=>setStatus('Reconnecting…'));
  connection.onreconnected(()=>setStatus('Live connected'));
  connection.start().then(()=>setStatus('Live connected')).catch(()=>setStatus('Live unavailable'));
  document.getElementById('support-refresh')?.addEventListener('click',()=>location.reload());
})();
