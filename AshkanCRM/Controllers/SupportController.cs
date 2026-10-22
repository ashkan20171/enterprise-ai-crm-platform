using System.Globalization;
using AshkanCRM.Hubs;
using Microsoft.AspNetCore.SignalR;
using AshkanCRM.Data;
using AshkanCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AshkanCRM.Models.Identity;
namespace AshkanCRM.Controllers;
[Authorize]
public class SupportController(CrmDbContext db, IHubContext<SupportHub> supportHub, IConfiguration configuration, UserManager<ApplicationUser> userManager) : Controller {
 private async Task NotifyAsync(SupportTicket ticket, string kind) {
  var recipients = new[]{ticket.Requester,ticket.AssignedTo}.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);
  foreach(var recipient in recipients) await supportHub.Clients.Group("support-user:"+recipient).SendAsync("SupportChanged",new {id=ticket.Id,kind});
  await supportHub.Clients.Group("support-managers").SendAsync("SupportChanged",new {id=ticket.Id,kind});
 }
 private int AgentCapacity(string userName) {
  var configured=configuration.GetValue<int?>("SupportRouting:Capacities:"+userName);
  var fallback=configuration.GetValue<int?>("SupportRouting:DefaultCapacity")??15;
  return Math.Clamp(configured??fallback,1,500);
 }
 private bool AgentAvailable(string userName) {
  var section=configuration.GetSection("SupportRouting:Agents").GetChildren()
   .FirstOrDefault(x=>string.Equals(x.Key,userName,StringComparison.OrdinalIgnoreCase));
  if(section is null)return true;
  if(section.GetValue<bool?>("Available") == false)return false;
  var timeZoneName=section["TimeZone"];
  if(string.IsNullOrWhiteSpace(timeZoneName))return true;
  try {
   var zone=TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);
   var local=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,zone);
   var days=section.GetSection("WorkingDays").Get<string[]>();
   if(days is {Length:>0} && !days.Contains(local.DayOfWeek.ToString(),StringComparer.OrdinalIgnoreCase))return false;
   var start=section.GetValue<int?>("StartHour")??0;
   var end=section.GetValue<int?>("EndHour")??24;
   if(start<0 || end>24 || start>=end)return false;
   return local.TimeOfDay>=TimeSpan.FromHours(start) && local.TimeOfDay<TimeSpan.FromHours(end);
  } catch(TimeZoneNotFoundException){return false;} catch(InvalidTimeZoneException){return false;}
 }
 // Server-side localization: messages are selected per request culture, not rewritten in the browser.
 private static string Ui(string fa, string en) =>
  CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fa" ? fa : en;
 private string Me => User.Identity?.Name ?? "";
 private bool Manager => User.IsInRole("Admin") || User.IsInRole("SalesManager");
 private IQueryable<SupportTicket> Visible => Manager ? db.SupportTickets : db.SupportTickets.Where(x=>x.Requester==Me || x.AssignedTo==Me);
 public async Task<IActionResult> Index(string? status,string? q){
  var query=Visible.AsNoTracking();
  if(!string.IsNullOrWhiteSpace(status)) query=query.Where(x=>x.Status==status);
  if(!string.IsNullOrWhiteSpace(q)) query=query.Where(x=>x.Subject.Contains(q) || x.Description.Contains(q));
  ViewBag.Status=status; ViewBag.Query=q;
  return View(await query.OrderByDescending(x=>x.UpdatedAtUtc).Take(200).ToListAsync());
 }
 public async Task<IActionResult> Details(int id){var ticket=await Visible.AsNoTracking().Include(x=>x.Replies).FirstOrDefaultAsync(x=>x.Id==id);if(ticket is null)return NotFound();ViewBag.Attachments=await db.SupportAttachments.AsNoTracking().Where(x=>x.TicketId==id).OrderByDescending(x=>x.UploadedAtUtc).ToListAsync();return View(ticket);}
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Create([Bind("Subject,Description,Priority,Category")] SupportTicket input){
  if(!new[]{"Low","Medium","High","Critical"}.Contains(input.Priority))ModelState.AddModelError("Priority",Ui("اولویت انتخاب‌شده معتبر نیست.", "Invalid priority."));
  if(!ModelState.IsValid){TempData["SupportError"]=Ui("موضوع، توضیحات و اولویت معتبر وارد کنید.", "Please enter a valid subject, description and priority.");return RedirectToAction(nameof(Index));}
  input.Requester=Me;input.AssignedTo="";input.Status="Open";input.CreatedAtUtc=DateTime.UtcNow;input.UpdatedAtUtc=DateTime.UtcNow;
  db.SupportTickets.Add(input);await db.SaveChangesAsync();await NotifyAsync(input,"created");return RedirectToAction(nameof(Details),new{id=input.Id});
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Reply(int id,[FromForm] string message){
  var ticket=await Visible.FirstOrDefaultAsync(x=>x.Id==id);if(ticket is null)return NotFound();
  if(string.IsNullOrWhiteSpace(message)||message.Length>3000){TempData["SupportError"]=Ui("طول پیام باید بین ۱ تا ۳۰۰۰ نویسه باشد.", "Message must be 1–3000 characters.");return RedirectToAction(nameof(Details),new{id});}
  db.SupportTicketReplies.Add(new SupportTicketReply{TicketId=id,Author=Me,Message=message.Trim()});ticket.UpdatedAtUtc=DateTime.UtcNow;
  if(ticket.Status=="Resolved")ticket.Status="Reopened";
  await db.SaveChangesAsync();await NotifyAsync(ticket,"updated");return RedirectToAction(nameof(Details),new{id});
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Update(int id,string status,string? assignedTo){
  if(!Manager)return Forbid();
  var ticket=await db.SupportTickets.FirstOrDefaultAsync(x=>x.Id==id);if(ticket is null)return NotFound();
  if(!new[]{"Open","InProgress","Waiting","Resolved","Closed","Reopened"}.Contains(status))return BadRequest();
  var assignee=(assignedTo??"").Trim();
  if(assignee.Length>180)return BadRequest(Ui("کارشناس انتخاب‌شده معتبر نیست.", "Invalid assignee"));
  if(assignee.Length>0 && !await db.Users.AnyAsync(u=>u.UserName==assignee))return BadRequest(Ui("کارشناس باید یکی از کاربران موجود باشد.", "Assignee must be an existing user"));
  ticket.Status=status;ticket.AssignedTo=assignee;
  ticket.UpdatedAtUtc=DateTime.UtcNow;ticket.ResolvedAtUtc=status is "Resolved" or "Closed"?DateTime.UtcNow:null;
  await db.SaveChangesAsync();await NotifyAsync(ticket,"updated");return RedirectToAction(nameof(Details),new{id});
 }

 // SLA policy: first response within 4h (critical), 8h (high), 24h (medium), 48h (low).
 private int FirstResponseHours(string priority)
 {
  var fallback = priority switch { "Critical"=>4, "High"=>8, "Low"=>48, _=>24 };
  var configured = configuration.GetValue<int?>("SupportSla:PriorityHours:"+priority);
  return configured is >= 1 and <= 1000 ? configured.Value : fallback;
 }
 [Authorize(Roles="Admin,SalesManager")]
 public IActionResult SlaSettings(){
  var model=new AshkanCRM.Models.SupportSlaSettingsViewModel {
   TimeZone=configuration["SupportSla:TimeZone"]??"UTC",
   OpeningHour=configuration.GetValue<int?>("SupportSla:OpeningHour")??9,
   ClosingHour=configuration.GetValue<int?>("SupportSla:ClosingHour")??17,
   WorkingDays=configuration.GetSection("SupportSla:WorkingDays").Get<string[]>()??new[]{"Monday","Tuesday","Wednesday","Thursday","Friday"},
   Holidays=configuration.GetSection("SupportSla:Holidays").Get<string[]>()??Array.Empty<string>(),
   CriticalHours=FirstResponseHours("Critical"),HighHours=FirstResponseHours("High"),
   MediumHours=FirstResponseHours("Medium"),LowHours=FirstResponseHours("Low")
  };
  return View(model);
 }
 [Authorize(Roles="Admin,SalesManager")]
 public async Task<IActionResult> AssignmentQueue(){
  var active=await db.SupportTickets.AsNoTracking()
   .Where(t=>t.Status!="Resolved" && t.Status!="Closed").ToListAsync();
  // Only users explicitly assigned to support-capable roles are eligible.
  var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var role in new[]{"Support","SupportAgent","Admin","SalesManager"}){
   foreach(var user in await userManager.GetUsersInRoleAsync(role)){
    if(!string.IsNullOrWhiteSpace(user.UserName) &&
       (!user.LockoutEnd.HasValue || user.LockoutEnd.Value<=DateTimeOffset.UtcNow))
     eligible.Add(user.UserName);
   }
  }
  var history=await db.SupportTickets.AsNoTracking()
   .Where(t=>t.AssignedTo!="" && (t.Status=="Resolved" || t.Status=="Closed"))
   .Select(t=>new{t.AssignedTo,t.Category}).ToListAsync();
  var workloads=eligible.Select(name=>new AshkanCRM.Models.SupportAgentLoad {
   UserName=name,ActiveTickets=active.Count(t=>t.AssignedTo==name),
   Capacity=AgentCapacity(name),Available=AgentAvailable(name),
   Categories=history.Where(t=>t.AssignedTo==name)
    .GroupBy(t=>t.Category).OrderByDescending(g=>g.Count()).Take(3)
    .Select(g=>g.Key).ToList()
  }).OrderBy(a=>a.ActiveTickets>=a.Capacity).ThenBy(a=>a.ActiveTickets).ThenBy(a=>a.UserName).ToList();
  return View(new AshkanCRM.Models.SupportAssignmentQueueViewModel {
   Agents=workloads,
   Unassigned=active.Where(t=>string.IsNullOrWhiteSpace(t.AssignedTo))
    .OrderByDescending(t=>t.Priority=="Critical").ThenBy(t=>t.CreatedAtUtc).Take(100).ToList()
  });
 }
 [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin,SalesManager")]
 public async Task<IActionResult> AssignTicket(int id,string assignee){
  if(string.IsNullOrWhiteSpace(assignee) || assignee.Length>180)return BadRequest();
  if(!AgentAvailable(assignee))return Conflict("Agent is unavailable or outside configured shift");
  if(await db.SupportTickets.CountAsync(t=>t.AssignedTo==assignee && t.Status!="Resolved" && t.Status!="Closed")>=AgentCapacity(assignee))return Conflict("Agent is at capacity");
  var target=await userManager.FindByNameAsync(assignee);
  if(target is null || (target.LockoutEnd.HasValue && target.LockoutEnd.Value>DateTimeOffset.UtcNow) ||
     !(await userManager.IsInRoleAsync(target,"Support") || await userManager.IsInRoleAsync(target,"SupportAgent") || await userManager.IsInRoleAsync(target,"Admin") || await userManager.IsInRoleAsync(target,"SalesManager")))
   return BadRequest(Ui("کاربر انتخاب‌شده مجوز پشتیبانی ندارد.", "Assignee is not an eligible support agent"));
  var ticket=await db.SupportTickets.FirstOrDefaultAsync(t=>t.Id==id);
  if(ticket is null)return NotFound();
  if(ticket.Status=="Closed" || ticket.Status=="Resolved")return BadRequest(Ui("تیکت فعال نیست.", "Ticket is not active"));
  if(!string.IsNullOrWhiteSpace(ticket.AssignedTo))return Conflict("Ticket is already assigned");
  ticket.AssignedTo=assignee;ticket.UpdatedAtUtc=DateTime.UtcNow;
  await db.SaveChangesAsync();await NotifyAsync(ticket,"assigned");
  return RedirectToAction(nameof(AssignmentQueue));
 }
 [Authorize(Roles="Admin,SalesManager")]
 public async Task<IActionResult> SlaBoard(){
  var tickets=await db.SupportTickets.AsNoTracking().Where(x=>x.Status!="Closed" && x.Status!="Resolved")
   .OrderBy(x=>x.CreatedAtUtc).Take(500).ToListAsync();
  var ids=tickets.Select(x=>x.Id).ToArray();
  var replies=await db.SupportTicketReplies.AsNoTracking().Where(x=>ids.Contains(x.TicketId))
   .Select(x=>new{x.TicketId,x.Author,x.CreatedAtUtc}).ToListAsync();
  var rows=tickets.Select(ticket=>{
   var deadline=AshkanCRM.Services.SupportBusinessCalendar.AddBusinessHours(ticket.CreatedAtUtc,FirstResponseHours(ticket.Priority),configuration);
   var answered=replies.Any(r=>r.TicketId==ticket.Id && r.Author!=ticket.Requester);
   return new AshkanCRM.Models.SupportSlaRow(ticket.Id,ticket.Subject,ticket.Priority,ticket.AssignedTo,deadline,answered,!answered && DateTime.UtcNow>deadline);
  }).OrderByDescending(x=>x.Breached).ThenBy(x=>x.DeadlineUtc).ToList();
  return View(rows);
 }
 public async Task<IActionResult> Insights(){
  var tickets=await Visible.AsNoTracking().Select(x=>new{x.Id,x.Priority,x.Status,x.Requester,x.CreatedAtUtc,x.ResolvedAtUtc}).ToListAsync();
  var ids=tickets.Select(x=>x.Id).ToArray();
  var replies=await db.SupportTicketReplies.AsNoTracking().Where(x=>ids.Contains(x.TicketId)).Select(x=>new{x.TicketId,x.Author,x.CreatedAtUtc}).ToListAsync();
  var slaBreaches=tickets.Count(t=>
   t.Status!="Closed" && t.Status!="Resolved" &&
   !replies.Any(r=>r.TicketId==t.Id && r.Author!=t.Requester &&
     r.CreatedAtUtc<=AshkanCRM.Services.SupportBusinessCalendar.AddBusinessHours(t.CreatedAtUtc,FirstResponseHours(t.Priority),configuration)) &&
   DateTime.UtcNow>AshkanCRM.Services.SupportBusinessCalendar.AddBusinessHours(t.CreatedAtUtc,FirstResponseHours(t.Priority),configuration));
  ViewBag.Total=tickets.Count;ViewBag.Open=tickets.Count(t=>t.Status!="Closed" && t.Status!="Resolved");ViewBag.Breaches=slaBreaches;
  ViewBag.Resolved=tickets.Count(t=>t.Status=="Resolved" || t.Status=="Closed");
  return View();
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Rate(int id,int rating,string? comment){
  var ticket=await Visible.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);
  if(ticket is null)return NotFound();
  if(ticket.Requester!=Me)return Forbid();
  if(ticket.Status!="Resolved" && ticket.Status!="Closed")return BadRequest(Ui("تیکت هنوز حل نشده است.", "Ticket is not resolved"));
  if(rating<1 || rating>5 || (comment?.Length??0)>1000)return BadRequest();
  if(await db.SupportSatisfactions.AnyAsync(x=>x.TicketId==id && x.SubmittedBy==Me))return Conflict("Feedback already submitted");
  db.SupportSatisfactions.Add(new SupportSatisfaction{TicketId=id,Rating=rating,Comment=(comment??"").Trim(),SubmittedBy=Me});
  try {await db.SaveChangesAsync();}catch(DbUpdateException){return Conflict("Feedback already submitted");}
  return RedirectToAction(nameof(Details),new{id});
 }

 public async Task<IActionResult> Knowledge(string? q){
  var articles=db.SupportKnowledgeArticles.AsNoTracking().Where(x=>x.IsPublished || Manager);
  if(!string.IsNullOrWhiteSpace(q)) articles=articles.Where(x=>x.Title.Contains(q) || x.Content.Contains(q) || x.Category.Contains(q));
  ViewBag.Query=q; return View(await articles.OrderByDescending(x=>x.UpdatedAtUtc).Take(100).ToListAsync());
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> PublishArticle(string title,string content,string category){
  if(!Manager)return Forbid();
  if(string.IsNullOrWhiteSpace(title)||title.Length>180||string.IsNullOrWhiteSpace(content)||content.Length>6000||string.IsNullOrWhiteSpace(category)||category.Length>100)return BadRequest();
  db.SupportKnowledgeArticles.Add(new SupportKnowledgeArticle{Title=title.Trim(),Content=content.Trim(),Category=category.Trim(),Author=Me});
  await db.SaveChangesAsync();return RedirectToAction(nameof(Knowledge));
 }
 [HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> Escalate(int id,string reason){
  if(!Manager)return Forbid();
  var ticket=await db.SupportTickets.FirstOrDefaultAsync(x=>x.Id==id);if(ticket is null)return NotFound();
  if(string.IsNullOrWhiteSpace(reason)||reason.Length>1000)return BadRequest();
  db.SupportEscalations.Add(new SupportEscalation{TicketId=id,Reason=reason.Trim(),Actor=Me});
  ticket.Priority="Critical";ticket.Status="InProgress";ticket.UpdatedAtUtc=DateTime.UtcNow;
  await db.SaveChangesAsync();await NotifyAsync(ticket,"escalated");return RedirectToAction(nameof(Details),new{id});
 }

 // Attachments are stored outside wwwroot and served only after ticket-scope authorization.
 [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(2500000)]
 public async Task<IActionResult> Upload(int id, Microsoft.AspNetCore.Http.IFormFile? file){
  if(!await Visible.AnyAsync(x=>x.Id==id))return NotFound();
  if(file is null || file.Length==0 || file.Length>2*1024*1024)return BadRequest(Ui("اندازه فایل باید بین ۱ بایت تا ۲ مگابایت باشد.", "File must be 1 byte to 2 MB"));
  var allowed=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){[".pdf"]="application/pdf",[".png"]="image/png",[".jpg"]="image/jpeg",[".jpeg"]="image/jpeg",[".txt"]="text/plain"};
  var name=System.IO.Path.GetFileName(file.FileName);
  var ext=System.IO.Path.GetExtension(name);
  if(name.Length>180 || !allowed.TryGetValue(ext,out var mime))return BadRequest(Ui("نوع فایل پشتیبانی نمی‌شود.", "Unsupported file type"));
  await using var stream=new System.IO.MemoryStream();await file.CopyToAsync(stream);
  var bytes=stream.ToArray();
  var signatureOk=ext.ToLowerInvariant() switch {
   ".pdf"=>bytes.Length>=5 && bytes[0]==37 && bytes[1]==80 && bytes[2]==68 && bytes[3]==70 && bytes[4]==45,
   ".png"=>bytes.Length>=8 && bytes[0]==137 && bytes[1]==80 && bytes[2]==78 && bytes[3]==71,
   ".jpg" or ".jpeg"=>bytes.Length>=3 && bytes[0]==255 && bytes[1]==216 && bytes[2]==255,
   _=>!bytes.Contains((byte)0)
  };
  if(!signatureOk)return BadRequest(Ui("امضای فایل معتبر نیست.", "Invalid file signature"));
  db.SupportAttachments.Add(new SupportAttachment{TicketId=id,FileName=name,ContentType=mime,Data=bytes,UploadedBy=Me});
  await db.SaveChangesAsync();return RedirectToAction(nameof(Details),new{id});
 }
 public async Task<IActionResult> Download(int id){
  var meta=await db.SupportAttachments.AsNoTracking().Where(x=>x.Id==id).Select(x=>new{x.TicketId}).FirstOrDefaultAsync();
  if(meta is null || !await Visible.AnyAsync(x=>x.Id==meta.TicketId))return NotFound();
  var item=await db.SupportAttachments.AsNoTracking().FirstAsync(x=>x.Id==id);
  Response.Headers["X-Content-Type-Options"]="nosniff";
  return File(item.Data,"application/octet-stream",item.FileName);
 }
 [Authorize(Roles="Admin,SalesManager")]
 public async Task<IActionResult> AgentPerformance(){
  // Aggregates across the full ticket set; unlike Workspace this is not capped at 200.
  var tickets=await db.SupportTickets.AsNoTracking().Select(x=>new {x.Id,x.AssignedTo,x.Status,x.CreatedAtUtc,x.ResolvedAtUtc,x.Priority}).ToListAsync();
  var rows=tickets.Where(x=>!string.IsNullOrWhiteSpace(x.AssignedTo))
   .GroupBy(x=>x.AssignedTo,StringComparer.OrdinalIgnoreCase)
   .Select(g=>new AshkanCRM.Models.SupportAgentMetric {
     Agent=g.Key, Total=g.Count(), Active=g.Count(t=>t.Status!="Closed" && t.Status!="Resolved"),
     Resolved=g.Count(t=>t.Status=="Closed" || t.Status=="Resolved"),
     CriticalOpen=g.Count(t=>t.Priority=="Critical" && t.Status!="Closed" && t.Status!="Resolved"),
     AverageResolutionHours=g.Where(t=>t.ResolvedAtUtc.HasValue && t.ResolvedAtUtc.Value>=t.CreatedAtUtc)
       .Select(t=>(t.ResolvedAtUtc!.Value-t.CreatedAtUtc).TotalHours).DefaultIfEmpty().Average()
   }).OrderByDescending(x=>x.Active).ThenBy(x=>x.Agent).ToList();
  ViewBag.Unassigned=tickets.Count(t=>string.IsNullOrWhiteSpace(t.AssignedTo) && t.Status!="Closed" && t.Status!="Resolved");
  ViewBag.Total=tickets.Count;
  return View(rows);
 }
 [Authorize(Roles="Admin,SalesManager")]
 public async Task<IActionResult> Workspace(string? priority){
  var tickets=db.SupportTickets.AsNoTracking().Where(x=>x.Status!="Closed" && x.Status!="Resolved");
  if(!string.IsNullOrWhiteSpace(priority))tickets=tickets.Where(x=>x.Priority==priority);
  ViewBag.Priority=priority;
  return View(await tickets.OrderByDescending(x=>x.Priority=="Critical").ThenBy(x=>x.CreatedAtUtc).Take(200).ToListAsync());
 }
 [Authorize(Roles="Admin,SalesManager")]
 public async Task<IActionResult> EscalationReport(int days = 30)
 {
  if (days is not (7 or 30 or 90)) days = 30;
  var from = DateTime.UtcNow.AddDays(-days);
  var events = await db.SupportEscalations.AsNoTracking()
   .Where(e => e.CreatedAtUtc >= from)
   .OrderByDescending(e => e.CreatedAtUtc).Take(500).ToListAsync();
  var ids = events.Select(e => e.TicketId).Distinct().ToArray();
  var subjects = await db.SupportTickets.AsNoTracking()
   .Where(t => ids.Contains(t.Id))
   .Select(t => new { t.Id, t.Subject, t.Priority, t.Status })
   .ToDictionaryAsync(t => t.Id);
  ViewBag.Days = days;
  ViewBag.Automatic = events.Count(e => e.Actor == "SLA Automation");
  ViewBag.Manual = events.Count - (int)ViewBag.Automatic;
  ViewBag.UniqueTickets = events.Select(e => e.TicketId).Distinct().Count();
  ViewBag.Tickets = subjects;
  return View(events);
 }

}
