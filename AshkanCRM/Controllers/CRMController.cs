using AshkanCRM.Models;using AshkanCRM.Models.Identity;using AshkanCRM.Data;using Microsoft.EntityFrameworkCore;using AshkanCRM.Services;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using System.Text;using System.Globalization;
namespace AshkanCRM.Controllers;
[Authorize] public class CRMController(CrmStore store, CrmDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager):Controller{
 // A zero/omitted period uses the organization's saved reporting preference.
 // Explicit periods always win, and reports remain limited to 1–12 months.
 private async Task<int> ResolveReportingMonthsAsync(int requestedMonths){
  if(requestedMonths!=0)return Math.Clamp(requestedMonths,1,12);
  var days=await db.CrmOrganizationSettings.AsNoTracking()
   .Where(x=>x.Id==1).Select(x=>(int?)x.DefaultReportingDays).FirstOrDefaultAsync() ?? 30;
  return Math.Clamp((int)Math.Ceiling(Math.Clamp(days,7,365)/30.0),1,12);
 }
 public async Task<IActionResult> Dashboard(int period=0){
  if(period!=0 && period!=3 && period!=6 && period!=12)return BadRequest(Ui("بازه زمانی داشبورد معتبر نیست.", "Invalid dashboard period"));
  ViewBag.DashboardPeriod=period;
  var access=await GetAccessAsync();
  var deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Where(x=>!x.IsDeleted)
   .Select(x=>new Deal(x.Id,x.Title,x.Customer,x.Owner,x.Value,x.Stage,x.Probability,x.CloseDate))
   .ToListAsync();
  // Alerts use the complete authorized deal set, independent of the selected forecast filter.
  var todayUtc=DateTime.UtcNow.Date;
  ViewBag.OverdueScheduledDeals=deals
   .Where(d=>d.Stage!="Won" && d.Stage!="Lost" && d.Stage!="برنده" && d.Stage!="ناموفق"
    && DateTime.TryParseExact(d.CloseDate,new[]{"yyyy-MM-dd","yyyy/MM/dd","MM/dd/yyyy","yyyy-MM-ddTHH:mm:ss"},CultureInfo.InvariantCulture,DateTimeStyles.None,out var closeDate)
    && closeDate.Date<todayUtc)
   .OrderBy(d=>d.CloseDate).Take(8).ToList();
  // The period applies only to deals with a parseable scheduled CloseDate.
  // It does not represent realized revenue or actual ClosedAtUtc.
  if(period>0){
   var from=DateTime.UtcNow.Date;
   var until=from.AddMonths(period);
   deals=deals.Where(d=>DateTime.TryParseExact(d.CloseDate,
    new[]{"yyyy-MM-dd","yyyy/MM/dd","MM/dd/yyyy","yyyy-MM-ddTHH:mm:ss"},
    CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)
    && date.Date>=from && date.Date<until).ToList();
  }
  // Realized revenue is based on actual UTC close dates, not the planned closing date.
  var realized=await GetMonthlyRealizedSalesAsync(access);
  ViewBag.RealizedSales=realized;
  ViewBag.RealizedComparable=await GetComparableSalesAsync(access);
  ViewBag.YearSales=await GetYearSalesAsync(access);
  ViewBag.Customers=await ApplyScope(db.CrmCustomers.AsNoTracking(),access).CountAsync();
  ViewBag.Pipeline=deals.Where(x=>x.Stage!="Won" && x.Stage!="Lost" && x.Stage!="برنده" && x.Stage!="ناموفق").Sum(x=>x.Value);
  ViewBag.Won=deals.Where(x=>x.Stage=="Won" || x.Stage=="برنده").Sum(x=>x.Value);
  ViewBag.WeightedPipeline=deals.Where(x=>x.Stage!="Won" && x.Stage!="Lost" && x.Stage!="برنده" && x.Stage!="ناموفق").Sum(x=>x.Value*Math.Clamp(x.Probability,0,100)/100m);
  ViewBag.WinRate=deals.Count(x=>x.Stage=="Won"||x.Stage=="برنده"||x.Stage=="Lost"||x.Stage=="ناموفق") is var closed && closed>0 ? (double)deals.Count(x=>x.Stage=="Won"||x.Stage=="برنده")*100/closed : 0d;
  ViewBag.Tasks=await ApplyScope(db.CrmTasks.AsNoTracking().Where(x=>!x.IsDeleted && x.Status!="Done"),access).CountAsync();
  return View(deals.OrderByDescending(x=>x.Value).ToList());
 }
 // Shared by the dashboard and Excel export to avoid divergent calculations.
 private async Task<List<MonthlyRealizedSalesVm>> GetMonthlyRealizedSalesAsync(AccessCtx access){
  var end=new DateTime(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1,0,0,0,DateTimeKind.Utc).AddMonths(1);
  var start=end.AddMonths(-12);
  var won=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Where(x=>!x.IsDeleted && (x.Stage=="Won" || x.Stage=="برنده")
    && x.ClosedAtUtc>=start && x.ClosedAtUtc<end)
   .Select(x=>new {x.ClosedAtUtc,x.Value}).ToListAsync();
  return Enumerable.Range(0,12).Select(i=>{
   var month=start.AddMonths(i);
   var matching=won.Where(x=>x.ClosedAtUtc.HasValue
    && x.ClosedAtUtc.Value.Year==month.Year && x.ClosedAtUtc.Value.Month==month.Month).ToList();
   return new MonthlyRealizedSalesVm {Month=month,Amount=matching.Sum(x=>x.Value),WonDeals=matching.Count};
  }).ToList();
 }
 // Equal-duration comparison: elapsed UTC time in the current month versus the
 // same elapsed duration from the start of the previous calendar month.
 private async Task<ComparableSalesVm> GetComparableSalesAsync(AccessCtx access){
  var now=DateTime.UtcNow;
  var currentStart=new DateTime(now.Year,now.Month,1,0,0,0,DateTimeKind.Utc);
  var previousStart=currentStart.AddMonths(-1);
  var elapsed=now-currentStart;
  var previousEnd=previousStart.Add(elapsed);
  if(previousEnd>currentStart)previousEnd=currentStart;
  var sales=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Where(x=>!x.IsDeleted && (x.Stage=="Won"||x.Stage=="برنده")
    && x.ClosedAtUtc>=previousStart && x.ClosedAtUtc<now)
   .Select(x=>new {x.ClosedAtUtc,x.Value}).ToListAsync();
  var current=sales.Where(x=>x.ClosedAtUtc>=currentStart).ToList();
  var previous=sales.Where(x=>x.ClosedAtUtc<previousEnd).ToList();
  return new ComparableSalesVm{
   CurrentAmount=current.Sum(x=>x.Value),PreviousAmount=previous.Sum(x=>x.Value),
   CurrentWon=current.Count,PreviousWon=previous.Count,
   CurrentStartUtc=currentStart,PreviousStartUtc=previousStart,
   PreviousEndUtc=previousEnd,AsOfUtc=now
  };
 }
 // Calendar-year-to-date comparisons use the same UTC cutoff in both years.
 private async Task<YearSalesVm> GetYearSalesAsync(AccessCtx access){
  var now=DateTime.UtcNow;
  var thisStart=new DateTime(now.Year,1,1,0,0,0,DateTimeKind.Utc);
  var lastStart=thisStart.AddYears(-1);
  var lastEnd=now.AddYears(-1);
  var sales=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Where(x=>!x.IsDeleted && (x.Stage=="Won"||x.Stage=="برنده")
    && x.ClosedAtUtc>=lastStart && x.ClosedAtUtc<now)
   .Select(x=>new {x.Owner,x.Value,x.ClosedAtUtc}).ToListAsync();
  var current=sales.Where(x=>x.ClosedAtUtc>=thisStart).ToList();
  var previous=sales.Where(x=>x.ClosedAtUtc<lastEnd).ToList();
  var leaders=current.GroupBy(x=>string.IsNullOrWhiteSpace(x.Owner)?"Unassigned":x.Owner)
   .Select(g=>new SalesLeaderVm{Owner=g.Key,Amount=g.Sum(x=>x.Value),WonDeals=g.Count()})
   .OrderByDescending(x=>x.Amount).ThenByDescending(x=>x.WonDeals)
   .ThenBy(x=>x.Owner,StringComparer.OrdinalIgnoreCase).Take(5).ToList();
  return new YearSalesVm{CurrentYear=now.Year,CurrentAmount=current.Sum(x=>x.Value),
   PreviousAmount=previous.Sum(x=>x.Value),CurrentWon=current.Count,PreviousWon=previous.Count,
   AsOfUtc=now,Leaders=leaders};
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportRealizedSalesExcel(){
  var access=await GetAccessAsync();
  var months=await GetMonthlyRealizedSalesAsync(access);
  var rows=months.Select(m=>(IReadOnlyList<object?>)new object?[]{m.Month.Year,m.Month.Month,m.Amount,m.WonDeals});
  return ExcelFile("Realized Sales",new[]{"Year","Month (UTC)","Realized value","Won deals"},rows,"realized-sales-12-months");
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ManageSalesTargets(int year=0){
  year=year==0?DateTime.UtcNow.Year:year;
  if(year<2000||year>2100)return BadRequest(Ui("سال انتخاب‌شده معتبر نیست.", "Invalid year"));
  var access=await GetAccessAsync();
  var owners=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Where(x=>x.Owner!=null && x.Owner!="").Select(x=>x.Owner).Distinct().ToListAsync();
  var rows=await db.CrmSalesTargets.AsNoTracking()
   .Where(x=>!x.IsDeleted && x.Year==year)
   .OrderBy(x=>x.Owner).ThenBy(x=>x.Month)
   .Select(x=>new SalesTargetManagementRow {Owner=x.Owner,Year=x.Year,Month=x.Month,Target=x.TargetAmount,Stretch=x.StretchAmount})
   .ToListAsync();
  return View(new SalesTargetManagementVm {Year=year,Owners=owners.Concat(rows.Select(x=>x.Owner))
   .Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToList(),Rows=rows});
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> TeamSalesComparison(int months=0){
  months=await ResolveReportingMonthsAsync(months);
  var end=new DateTime(DateTime.UtcNow.Year,DateTime.UtcNow.Month,1,0,0,0,DateTimeKind.Utc).AddMonths(1);
  var start=end.AddMonths(-months);
  var access=await GetAccessAsync();
  var won=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Where(x=>!x.IsDeleted&&(x.Stage=="Won"||x.Stage=="برنده")&&x.ClosedAtUtc>=start&&x.ClosedAtUtc<end)
   .Select(x=>new {x.TeamKey,x.Value}).ToListAsync();
  var teams=await db.CrmTeams.AsNoTracking().Where(x=>!x.IsDeleted&&x.IsActive)
   .Select(x=>new {x.Id,x.TeamKey,x.Name}).ToListAsync();
  var members=await db.CrmTeamMembers.AsNoTracking().Where(x=>!x.IsDeleted&&x.IsPrimary)
   .Select(x=>new {x.TeamId,x.UserName}).ToListAsync();
  var targets=await db.CrmSalesTargets.AsNoTracking()
   .Where(x=>!x.IsDeleted&&(x.Year>start.Year||(x.Year==start.Year&&x.Month>=start.Month))
    &&(x.Year<end.Year||(x.Year==end.Year&&x.Month<end.Month)))
   .Select(x=>new {x.Owner,x.TargetAmount}).ToListAsync();
  var model=new TeamSalesComparisonVm {Months=months};
  foreach(var team in teams){
   var owners=members.Where(x=>x.TeamId==team.Id).Select(x=>x.UserName).ToHashSet(StringComparer.OrdinalIgnoreCase);
   var sales=won.Where(x=>string.Equals(x.TeamKey,team.TeamKey,StringComparison.OrdinalIgnoreCase)).ToList();
   model.Rows.Add(new TeamSalesComparisonRow {
    TeamKey=team.TeamKey,TeamName=team.Name,Actual=sales.Sum(x=>x.Value),WonDeals=sales.Count,
    Target=targets.Where(x=>owners.Contains(x.Owner)).Sum(x=>x.TargetAmount)
   });
  }
  model.Rows=model.Rows.OrderByDescending(x=>x.Actual).ThenBy(x=>x.TeamName).ToList();
  return View(model);
 }

 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> PrintTeamSalesComparison(int months=0){
  var result=await TeamSalesComparison(months);
  if(result is not ViewResult view || view.Model is not TeamSalesComparisonVm report)return StatusCode(500);
  var rows=report.Rows.Select(r=>new[]{r.TeamName,r.Target.ToString("N0"),r.Actual.ToString("N0"),r.WonDeals.ToString(),r.Target>0?r.Attainment.ToString("N1")+"%":"—"}).ToList();
  return View("PrintableSalesReport",await ApplyPrintPreferencesAsync(PrintReport("مقایسه فروش تیم‌ها","Team sales comparison",report.Months,
   new[]{"تیم","هدف","عملکرد","فرصت موفق","تحقق"},new[]{"Team","Target","Actual","Won deals","Attainment"},rows)));
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> PrintTargetVsActual(int months=0,string? owner=null){
  var result=await TargetVsActual(months,owner);
  if(result is not ViewResult view || view.Model is not SalesTargetActualReportVm report)return StatusCode(500);
  var rows=report.Rows.Select(r=>new[]{r.Year.ToString(),r.Month.ToString("00"),r.Target.ToString("N0"),r.Stretch.ToString("N0"),r.Actual.ToString("N0"),r.WonCount.ToString(),r.Target>0?r.Attainment.ToString("N1")+"%":"—"}).ToList();
  var m=PrintReport("هدف در برابر عملکرد","Sales targets vs actual",report.Months,
   new[]{"سال","ماه","هدف","هدف توسعه‌ای","عملکرد","موفق","تحقق"},new[]{"Year","Month","Target","Stretch","Actual","Won","Attainment"},rows);
  m.Filter=report.Owner;
  return View("PrintableSalesReport",await ApplyPrintPreferencesAsync(m));
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> PrintSalesPerformance(int months=0){
  var result=await SalesPerformance(months);
  if(result is not ViewResult view || view.Model is not List<SalesAgentPerformanceVm> report)return StatusCode(500);
  var rows=report.Select(r=>new[]{r.Owner,r.WonCount.ToString(),r.LostCount.ToString(),r.WinRate.ToString("N1")+"%",r.WonValue.ToString("N0"),r.OpenValue.ToString("N0")}).ToList();
  return View("PrintableSalesReport",await ApplyPrintPreferencesAsync(PrintReport("عملکرد کارشناسان فروش","Sales agent performance",(int)(ViewBag.Months ?? 6),
   new[]{"کارشناس","موفق","ناموفق","نرخ موفقیت","ارزش فروش","فرصت‌های باز"},new[]{"Agent","Won","Lost","Win rate","Won value","Open pipeline"},rows)));
 }
 private static AshkanCRM.Models.PrintableSalesReportVm PrintReport(string fa,string en,int months,string[] faHeaders,string[] enHeaders,List<string[]> rows)=>
  new(){TitleFa=fa,TitleEn=en,Months=months,HeadersFa=faHeaders,HeadersEn=enHeaders,Rows=rows};

 private async Task<PrintableSalesReportVm> ApplyPrintPreferencesAsync(PrintableSalesReportVm report){
  var settings=await db.CrmOrganizationSettings.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==1);
  report.OrganizationName=settings?.OrganizationName??"AshkanCRM";
  report.ReportingCurrency=settings?.ReportingCurrency??"EUR";
  report.TimeZoneId=settings?.TimeZoneId??"UTC";
  try { report.GeneratedAtLocal=TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById(report.TimeZoneId)); }
  catch(TimeZoneNotFoundException){report.TimeZoneId="UTC";report.GeneratedAtLocal=DateTime.UtcNow;}
  catch(InvalidTimeZoneException){report.TimeZoneId="UTC";report.GeneratedAtLocal=DateTime.UtcNow;}
  return report;
 }

 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportTeamSalesComparisonCsv(int months=0){
  var result=await TeamSalesComparison(months);
  if(result is not ViewResult view || view.Model is not TeamSalesComparisonVm report)return StatusCode(500);
  var csv=new StringBuilder("Team,TeamKey,Target,Actual,WonDeals,AttainmentPercent\r\n");
  foreach(var r in report.Rows){
   csv.Append(CsvCell(r.TeamName)).Append(',').Append(CsvCell(r.TeamKey)).Append(',')
    .Append(r.Target.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Actual.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.WonDeals).Append(',')
    .Append(r.Target>0?r.Attainment.ToString("0.##",CultureInfo.InvariantCulture):"").Append("\r\n");
  }
  return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
   "text/csv; charset=utf-8",$"team-sales-comparison-{DateTime.UtcNow:yyyyMMdd}.csv");
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportTeamSalesComparisonExcel(int months=0){
  var result=await TeamSalesComparison(months);
  if(result is not ViewResult view || view.Model is not TeamSalesComparisonVm report)return StatusCode(500);
  var rows=report.Rows.Select(r=>(IReadOnlyList<object?>)new object?[]{r.TeamName,r.TeamKey,r.Target,r.Actual,r.WonDeals,r.Target>0?r.Attainment:null});
  return ExcelFile("Team Sales",new[]{"Team","Team key","Target","Actual","Won deals","Attainment %"},rows,"team-sales");
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportTargetVsActualExcel(int months=0,string? owner=null){
  var result=await TargetVsActual(months,owner);
  if(result is not ViewResult view || view.Model is not SalesTargetActualReportVm report)return StatusCode(500);
  var rows=report.Rows.Select(r=>(IReadOnlyList<object?>)new object?[]{r.Year,r.Month,r.Target,r.Stretch,r.Actual,r.WonCount,r.Target>0?r.Attainment:null});
  return ExcelFile("Targets vs Actual",new[]{"Year","Month","Target","Stretch","Actual","Won deals","Attainment %"},rows,"sales-targets");
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportSalesPerformanceExcel(int months=0){
  var result=await SalesPerformance(months);
  if(result is not ViewResult view || view.Model is not List<SalesAgentPerformanceVm> report)return StatusCode(500);
  var rows=report.Select(r=>(IReadOnlyList<object?>)new object?[]{r.Owner,r.WonCount,r.LostCount,r.WinRate,r.WonValue,r.OpenValue});
  return ExcelFile("Agent Performance",new[]{"Agent","Won","Lost","Win rate %","Won value","Open pipeline"},rows,"sales-agents");
 }
 private FileContentResult ExcelFile(string sheet,string[] headers,IEnumerable<IReadOnlyList<object?>> rows,string filename)=>
  File(ExcelReportWriter.Create(sheet,headers,rows),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"{filename}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 public async Task<IActionResult> TargetVsActual(int months=0,string? owner=null){
  months=await ResolveReportingMonthsAsync(months);
  owner=string.IsNullOrWhiteSpace(owner)?null:owner.Trim();
  if(owner?.Length>180)return BadRequest(Ui("مسئول انتخاب‌شده معتبر نیست.", "Invalid owner"));
  var today=DateTime.UtcNow;
  var start=new DateTime(today.Year,today.Month,1,0,0,0,DateTimeKind.Utc).AddMonths(1-months);
  var end=new DateTime(today.Year,today.Month,1,0,0,0,DateTimeKind.Utc).AddMonths(1);
  var access=await GetAccessAsync();
  var dealQuery=ApplyScope(db.CrmDeals.AsNoTracking(),access);
  if(owner is not null)dealQuery=dealQuery.Where(x=>x.Owner==owner);
  var won=await dealQuery
   .Where(x=>(x.Stage=="Won"||x.Stage=="برنده")&&x.ClosedAtUtc>=start&&x.ClosedAtUtc<end)
   .Select(x=>new {x.Value,x.ClosedAtUtc}).ToListAsync();
  // Targets are managerial configuration; this report is restricted to managers.
  var targetQuery=db.CrmSalesTargets.AsNoTracking();
  if(owner is not null)targetQuery=targetQuery.Where(x=>x.Owner==owner);
  var targets=await targetQuery
   .Where(x=>!x.IsDeleted&&(x.Year>start.Year||(x.Year==start.Year&&x.Month>=start.Month))
    &&(x.Year<today.Year||(x.Year==today.Year&&x.Month<=today.Month)))
   .Select(x=>new {x.Year,x.Month,x.TargetAmount,x.StretchAmount}).ToListAsync();
  var owners=await db.CrmSalesTargets.AsNoTracking().Where(x=>!x.IsDeleted).Select(x=>x.Owner).Distinct().ToListAsync();
  var dealOwners=await ApplyScope(db.CrmDeals.AsNoTracking(),access).Select(x=>x.Owner).Distinct().ToListAsync();
  var model=new SalesTargetActualReportVm { Months=months, Owner=owner, Owners=owners.Concat(dealOwners).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x).ToList() };
  for(var i=0;i<months;i++){
   var date=start.AddMonths(i);
   var actual=won.Where(x=>x.ClosedAtUtc.HasValue&&x.ClosedAtUtc.Value.Year==date.Year&&x.ClosedAtUtc.Value.Month==date.Month).ToList();
   var monthlyTargets=targets.Where(x=>x.Year==date.Year&&x.Month==date.Month);
   model.Rows.Add(new SalesTargetActualVm{
    Year=date.Year,Month=date.Month,Target=monthlyTargets.Sum(x=>x.TargetAmount),
    Stretch=monthlyTargets.Sum(x=>x.StretchAmount),Actual=actual.Sum(x=>x.Value),WonCount=actual.Count
   });
  }
  return View(model);
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportTargetVsActualCsv(int months=0,string? owner=null){
  // Reuse the same scoped query and reporting calculation as the on-screen report.
  var result=await TargetVsActual(months,owner);
  if(result is not ViewResult view || view.Model is not SalesTargetActualReportVm report)
   return StatusCode(500);
  var csv=new StringBuilder("Year,Month,Target,Stretch,Actual,WonDeals,AttainmentPercent\r\n");
  foreach(var r in report.Rows){
   csv.Append(r.Year).Append(',').Append(r.Month).Append(',')
    .Append(r.Target.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Stretch.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.Actual.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.WonCount).Append(',')
    .Append(r.Target>0?r.Attainment.ToString("0.##",CultureInfo.InvariantCulture):"")
    .Append("\r\n");
  }
  return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
   "text/csv; charset=utf-8",$"sales-target-vs-actual-{DateTime.UtcNow:yyyyMMdd}.csv");
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 [HttpGet]
 public async Task<IActionResult> ExportSalesPerformanceCsv(int months=0){
  var result=await SalesPerformance(months);
  if(result is not ViewResult view || view.Model is not List<SalesAgentPerformanceVm> report)
   return StatusCode(500);
  var csv=new StringBuilder("Agent,WonDeals,LostDeals,WinRatePercent,WonValue,OpenPipeline\r\n");
  foreach(var r in report){
   csv.Append(CsvCell(r.Owner)).Append(',').Append(r.WonCount).Append(',')
    .Append(r.LostCount).Append(',')
    .Append(r.WinRate.ToString("0.##",CultureInfo.InvariantCulture)).Append(',')
    .Append(r.WonValue.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(r.OpenValue.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
  }
  return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
   "text/csv; charset=utf-8",$"sales-agent-performance-{DateTime.UtcNow:yyyyMMdd}.csv");
 }
 private static string CsvCell(string? raw){
  var value=raw??"";
  // Protect spreadsheet users against formula injection when opening exported CSV files.
  if(value.Length>0 && (value[0]=='=' || value[0]=='+' || value[0]=='-' || value[0]=='@' || value[0]=='\t' || value[0]=='\r'))
   value="'"+value;
  return "\""+value.Replace("\"","\"\"")+"\"";
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)]
 public async Task<IActionResult> SalesPerformance(int months=0){
  months=await ResolveReportingMonthsAsync(months);
  var access=await GetAccessAsync();
  var from=DateTime.UtcNow.Date.AddMonths(-months);
  var deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access)
   .Select(x=>new {x.Owner,x.Value,x.Stage,x.ClosedAtUtc}).ToListAsync();
  var rows=deals.GroupBy(x=>string.IsNullOrWhiteSpace(x.Owner)?"(Unassigned)":x.Owner,StringComparer.OrdinalIgnoreCase)
   .Select(g=>{
    var won=g.Where(x=>(x.Stage=="Won"||x.Stage=="برنده")&&x.ClosedAtUtc>=from).ToList();
    var lost=g.Where(x=>(x.Stage=="Lost"||x.Stage=="ناموفق")&&x.ClosedAtUtc>=from).ToList();
    return new AshkanCRM.Models.SalesAgentPerformanceVm{
     Owner=g.Key,WonCount=won.Count,LostCount=lost.Count,WonValue=won.Sum(x=>x.Value),
     OpenValue=g.Where(x=>x.Stage!="Won"&&x.Stage!="برنده"&&x.Stage!="Lost"&&x.Stage!="ناموفق").Sum(x=>x.Value)
    };
   }).OrderByDescending(x=>x.WonValue).ThenBy(x=>x.Owner).ToList();
  ViewBag.Months=months;
  return View(rows);
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> Customers(string? q=null,string? status=null,int page=1){
  const int size=10; page=Math.Max(1,page); var access=await GetAccessAsync(); var query=ApplyScope(db.CrmCustomers.AsNoTracking(),access); if(!string.IsNullOrWhiteSpace(q)) query=query.Where(x=>x.Name.Contains(q)||x.Company.Contains(q)||x.Email.Contains(q)); if(!string.IsNullOrWhiteSpace(status)) query=query.Where(x=>x.Status==status);
  ViewBag.Total=await query.CountAsync(); ViewBag.Page=page;ViewBag.PageSize=size;ViewBag.Query=q;ViewBag.Status=status; var rows=await query.OrderByDescending(x=>x.UpdatedAtUtc).Skip((page-1)*size).Take(size).Select(x=>new Customer(x.Id,x.Name,x.Company,x.Email,x.Phone,x.Segment,x.Owner,x.Status,x.Score)).ToListAsync();return View(rows);
 }
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateCustomer(CustomerEntity input){ModelState.Remove(nameof(input.RowVersion));if(!ModelState.IsValid){TempData["Error"]=Ui("اطلاعات مشتری معتبر نیست.","Invalid customer data.");return RedirectToAction(nameof(Customers));}input.Id=0;input.CreatedBy=input.UpdatedBy=User.Identity?.Name;input.CreatedAtUtc=input.UpdatedAtUtc=DateTime.UtcNow;if(!User.IsInRole(Roles.Admin)&&!User.IsInRole(Roles.SalesManager))input.Owner=User.Identity?.Name??"";input.TeamKey=await ResolveTeamKey(input.Owner);db.CrmCustomers.Add(input);await db.SaveChangesAsync();await WriteAudit("Create","Customer",input.Id.ToString(),input.Company);await AddActivity("Customer",input.Id,"Created","Customer created",input.Company);TempData["Success"]=Ui("مشتری با موفقیت ایجاد شد.","Customer created.");return RedirectToAction(nameof(Customers));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> UpdateCustomer(CustomerEntity input){var row=await db.CrmCustomers.FirstOrDefaultAsync(x=>x.Id==input.Id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();row.Name=input.Name;row.Company=input.Company;row.Email=input.Email;row.Phone=input.Phone;row.Segment=input.Segment;row.Status=input.Status;row.Score=input.Score;if(User.IsInRole(Roles.Admin)||User.IsInRole(Roles.SalesManager)){row.Owner=input.Owner;row.TeamKey=await ResolveTeamKey(input.Owner);}row.UpdatedAtUtc=DateTime.UtcNow;row.UpdatedBy=User.Identity?.Name;await db.SaveChangesAsync();await WriteAudit("Update","Customer",row.Id.ToString(),row.Company);await AddActivity("Customer",row.Id,"Updated","Customer updated",row.Company);return RedirectToAction(nameof(Customers));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> DeleteCustomer(int id){var row=await db.CrmCustomers.FirstOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();row.IsDeleted=true;row.UpdatedAtUtc=DateTime.UtcNow;row.UpdatedBy=User.Identity?.Name;await db.SaveChangesAsync();await WriteAudit("SoftDelete","Customer",id.ToString(),row.Company);return RedirectToAction(nameof(Customers));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public async Task<IActionResult> CustomerRecycleBin()=>View(await db.CrmCustomers.IgnoreQueryFilters().AsNoTracking().Where(x=>x.IsDeleted).OrderByDescending(x=>x.UpdatedAtUtc).ToListAsync());
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> RestoreCustomer(int id){var row=await db.CrmCustomers.IgnoreQueryFilters().FirstOrDefaultAsync(x=>x.Id==id&&x.IsDeleted);if(row==null)return NotFound();row.IsDeleted=false;row.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await WriteAudit("Restore","Customer",id.ToString(),row.Company);return RedirectToAction(nameof(CustomerRecycleBin));}
 private static string Ui(string fa,string en)=>CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="en"?en:fa;
 private bool CanManage(string owner)=>User.IsInRole(Roles.Admin)||User.IsInRole(Roles.SalesManager)||string.Equals(owner,User.Identity?.Name,StringComparison.OrdinalIgnoreCase);
 private async Task AddActivity(string entity,int id,string type,string subject,string detail){db.CrmActivities.Add(new ActivityEntity{EntityType=entity,EntityId=id,Type=type,Subject=subject,Detail=detail,Owner=User.Identity?.Name??"system",CreatedBy=User.Identity?.Name});await db.SaveChangesAsync();}
 private async Task WriteAudit(string action,string entity,string entityId,string detail){db.CrmAuditEvents.Add(new AuditEventEntity{Actor=User.Identity?.Name??"system",Action=action,EntityType=entity,EntityId=entityId,Detail=detail,CreatedBy=User.Identity?.Name});await db.SaveChangesAsync();}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> Leads(){var access=await GetAccessAsync();var q=ApplyScope(db.CrmLeads.AsNoTracking(),access);var rows=await q.OrderByDescending(x=>x.Score).Select(x=>new Lead(x.Id,x.Name,x.Company,x.Source,x.Owner,x.Status,x.Score,x.LastContact)).ToListAsync();return View(rows);}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateLead(LeadEntity input){if(!ModelState.IsValid){TempData["Error"]=Ui("اطلاعات سرنخ معتبر نیست.","Invalid lead data.");return RedirectToAction(nameof(Leads));}input.Id=0;input.CreatedBy=input.UpdatedBy=User.Identity?.Name;input.Owner=(User.IsInRole(Roles.Admin)||User.IsInRole(Roles.SalesManager))?input.Owner:(User.Identity?.Name??"");input.TeamKey=await ResolveTeamKey(input.Owner);db.CrmLeads.Add(input);await db.SaveChangesAsync();await WriteAudit("Create","Lead",input.Id.ToString(),input.Company);return RedirectToAction(nameof(Leads));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> Deals(){var access=await GetAccessAsync();var q=ApplyScope(db.CrmDeals.AsNoTracking(),access);var rows=await q.OrderByDescending(x=>x.Value).Select(x=>new Deal(x.Id,x.Title,x.Customer,x.Owner,x.Value,x.Stage,x.Probability,x.CloseDate)).ToListAsync();return View(rows);}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateDeal(DealEntity input){if(!ModelState.IsValid){TempData["Error"]=Ui("اطلاعات فرصت فروش معتبر نیست.","Invalid deal data.");return RedirectToAction(nameof(Deals));}input.Id=0;input.CreatedBy=input.UpdatedBy=User.Identity?.Name;input.Owner=(User.IsInRole(Roles.Admin)||User.IsInRole(Roles.SalesManager))?input.Owner:(User.Identity?.Name??"");input.TeamKey=await ResolveTeamKey(input.Owner);db.CrmDeals.Add(input);await db.SaveChangesAsync();await WriteAudit("Create","Deal",input.Id.ToString(),input.Customer);return RedirectToAction(nameof(Deals));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> UpdateLead(LeadEntity input){var row=await db.CrmLeads.FirstOrDefaultAsync(x=>x.Id==input.Id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();row.Name=input.Name;row.Company=input.Company;row.Source=input.Source;row.Status=input.Status;row.Score=input.Score;row.LastContact=input.LastContact;if(User.IsInRole(Roles.Admin)||User.IsInRole(Roles.SalesManager)){row.Owner=input.Owner;row.TeamKey=await ResolveTeamKey(input.Owner);}row.UpdatedAtUtc=DateTime.UtcNow;row.UpdatedBy=User.Identity?.Name;await db.SaveChangesAsync();await WriteAudit("Update","Lead",row.Id.ToString(),row.Company);await AddActivity("Lead",row.Id,"Updated","Lead updated",row.Company);return RedirectToAction(nameof(Leads));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> DeleteLead(int id){var row=await db.CrmLeads.FirstOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();row.IsDeleted=true;row.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await WriteAudit("SoftDelete","Lead",id.ToString(),row.Company);return RedirectToAction(nameof(Leads));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> UpdateDeal(DealEntity input){var row=await db.CrmDeals.FirstOrDefaultAsync(x=>x.Id==input.Id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();row.Title=input.Title;row.Customer=input.Customer;row.Value=input.Value;row.Stage=input.Stage;row.Probability=input.Probability;row.CloseDate=input.CloseDate;if(User.IsInRole(Roles.Admin)||User.IsInRole(Roles.SalesManager)){row.Owner=input.Owner;row.TeamKey=await ResolveTeamKey(input.Owner);}row.UpdatedAtUtc=DateTime.UtcNow;row.UpdatedBy=User.Identity?.Name;await db.SaveChangesAsync();await WriteAudit("Update","Deal",row.Id.ToString(),row.Title);await AddActivity("Deal",row.Id,"Updated","Deal updated",row.Stage);return RedirectToAction(nameof(Deals));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> MoveDealStage(int id,string stage){var allowed=new[]{"Qualification","Proposal","Negotiation","Won","Lost"};if(!allowed.Contains(stage))return BadRequest();var row=await db.CrmDeals.FirstOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();var old=row.Stage;row.Stage=stage;row.Probability=stage switch{"Qualification"=>20,"Proposal"=>50,"Negotiation"=>75,"Won"=>100,"Lost"=>0,_=>row.Probability};row.ClosedAtUtc=(stage=="Won"||stage=="Lost")?DateTime.UtcNow:null;row.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await WriteAudit("StageChange","Deal",id.ToString(),old+" -> "+stage);await AddActivity("Deal",id,"StageChange","Pipeline stage changed",old+" -> "+stage);db.CrmNotifications.Add(new NotificationEntity{UserName=row.Owner,Type="Deal",Title="Pipeline updated",Message=row.Title+": "+old+" → "+stage,CreatedBy=User.Identity?.Name});await db.SaveChangesAsync();return RedirectToAction(nameof(Deals));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> DeleteDeal(int id){var row=await db.CrmDeals.FirstOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();row.IsDeleted=true;row.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();await WriteAudit("SoftDelete","Deal",id.ToString(),row.Title);return RedirectToAction(nameof(Deals));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> LeadTimeline(int id){var row=await db.CrmLeads.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();ViewBag.EntityTitle=row.Name+" · "+row.Company;ViewBag.EntityType="Lead";ViewBag.EntityId=id;return View("EntityTimeline",await db.CrmActivities.AsNoTracking().Where(x=>x.EntityType=="Lead"&&x.EntityId==id).OrderByDescending(x=>x.OccurredAtUtc).ToListAsync());}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> DealTimeline(int id){var row=await db.CrmDeals.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(row==null)return NotFound();if(!await CanManageScoped(row.Owner,row.TeamKey))return Forbid();ViewBag.EntityTitle=row.Title;ViewBag.EntityType="Deal";ViewBag.EntityId=id;return View("EntityTimeline",await db.CrmActivities.AsNoTracking().Where(x=>x.EntityType=="Deal"&&x.EntityId==id).OrderByDescending(x=>x.OccurredAtUtc).ToListAsync());}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> AddEntityActivity(string entityType,int entityId,string subject,string detail){if(entityType!="Lead"&&entityType!="Deal"&&entityType!="Customer")return BadRequest();await AddActivity(entityType,entityId,"Note",subject,detail);return RedirectToAction(entityType=="Lead"?nameof(LeadTimeline):entityType=="Deal"?nameof(DealTimeline):nameof(CustomerTimeline),new{id=entityId});}

 public async Task<IActionResult> Tasks(){await ProcessDueReminders();var access=await GetAccessAsync();var q=ApplyScope(db.CrmTasks.AsNoTracking().Where(x=>!x.IsDeleted),access);return View(await q.OrderBy(x=>x.Status=="Done").ThenBy(x=>x.DueAtUtc).ToListAsync());}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateTask(CrmTaskEntity input){if(string.IsNullOrWhiteSpace(input.Title))return RedirectToAction(nameof(Tasks));input.Id=0;input.CreatedBy=input.UpdatedBy=User.Identity?.Name;if(!User.IsInRole(Roles.Admin)&&!User.IsInRole(Roles.SalesManager))input.Assignee=User.Identity?.Name??"";input.TeamKey=await ResolveTeamKey(input.Assignee);db.CrmTasks.Add(input);await db.SaveChangesAsync();await WriteAudit("Create","Task",input.Id.ToString(),input.Title);return RedirectToAction(nameof(Tasks));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> ToggleTask(int id){var t=await db.CrmTasks.FirstOrDefaultAsync(x=>x.Id==id);if(t==null)return NotFound();if(!await CanManageScoped(t.Assignee,t.TeamKey))return Forbid();var completing=t.Status!="Done";t.Status=completing?"Done":"Open";t.UpdatedAtUtc=DateTime.UtcNow;if(completing&&t.IsRecurring&&t.Recurrence!="None"){var nextDue=t.Recurrence switch{"Daily"=>t.DueAtUtc.AddDays(1),"Weekly"=>t.DueAtUtc.AddDays(7),"Monthly"=>t.DueAtUtc.AddMonths(1),_=>t.DueAtUtc.AddDays(1)};db.CrmTasks.Add(new CrmTaskEntity{Title=t.Title,Type=t.Type,Assignee=t.Assignee,TeamKey=t.TeamKey,Priority=t.Priority,Status="Open",DueAtUtc=nextDue,ReminderAtUtc=t.ReminderAtUtc.HasValue?nextDue-(t.DueAtUtc-t.ReminderAtUtc.Value):null,RelatedEntityType=t.RelatedEntityType,RelatedEntityId=t.RelatedEntityId,Description=t.Description,IsRecurring=true,Recurrence=t.Recurrence,CreatedBy=User.Identity?.Name});}await db.SaveChangesAsync();await WriteAudit("StatusChange","Task",id.ToString(),t.Status);return RedirectToAction(nameof(Tasks));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> DeleteTask(int id){var t=await db.CrmTasks.FirstOrDefaultAsync(x=>x.Id==id);if(t==null)return NotFound();if(!await CanManageScoped(t.Assignee,t.TeamKey))return Forbid();t.IsDeleted=true;t.UpdatedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();return RedirectToAction(nameof(Tasks));}
 public IActionResult Activities()=>View(store.Activities);
 public async Task<IActionResult> Calendar(){var q=db.CrmTasks.AsNoTracking().Where(x=>!x.IsDeleted);if(!User.IsInRole(Roles.Admin)&&!User.IsInRole(Roles.SalesManager))q=q.Where(x=>x.Assignee==User.Identity!.Name);return View(await q.OrderBy(x=>x.DueAtUtc).Take(100).ToListAsync());}
 public async Task<IActionResult> GlobalSearch(string q){q=(q??"").Trim();ViewBag.Query=q;var result=new GlobalSearchVm();if(q.Length>=2){var access=await GetAccessAsync();var customers=ApplyScope(db.CrmCustomers.AsNoTracking(),access);var leads=ApplyScope(db.CrmLeads.AsNoTracking(),access);var deals=ApplyScope(db.CrmDeals.AsNoTracking(),access);result.Customers=await customers.Where(x=>x.Name.Contains(q)||x.Company.Contains(q)||x.Email.Contains(q)).Take(8).ToListAsync();result.Leads=await leads.Where(x=>x.Name.Contains(q)||x.Company.Contains(q)).Take(8).ToListAsync();result.Deals=await deals.Where(x=>x.Title.Contains(q)||x.Customer.Contains(q)).Take(8).ToListAsync();}return View(result);}
 public async Task<IActionResult> Notifications(){var u=User.Identity?.Name??"";return View("SqlNotifications",await db.CrmNotifications.AsNoTracking().Where(x=>x.UserName==u||User.IsInRole(Roles.Admin)).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync());}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> MarkNotificationRead(int id){var u=User.Identity?.Name??"";var n=await db.CrmNotifications.FirstOrDefaultAsync(x=>x.Id==id&&(x.UserName==u||User.IsInRole(Roles.Admin)));if(n==null)return NotFound();n.IsRead=true;n.ReadAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();return RedirectToAction(nameof(Notifications));}
 [Authorize(Roles=Roles.Admin+","+Roles.Support)] public IActionResult Tickets()=>View(store.Tickets);
 [Authorize(Roles=Roles.Admin+","+Roles.Marketing)] public IActionResult Marketing()=>View();
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public IActionResult Reports()=>View();
 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> Users(){
  var rows=new List<UserAdminRow>();
  foreach(var u in userManager.Users.OrderByDescending(x=>x.CreatedAtUtc).ToList()) rows.Add(new UserAdminRow{Id=u.Id,Name=u.DisplayName,Email=u.Email??"",Department=u.Department,Initials=u.Initials,IsActive=u.IsActive,IsLocked=u.LockoutEnd>DateTimeOffset.UtcNow,LastLoginAtUtc=u.LastLoginAtUtc,Roles=await userManager.GetRolesAsync(u)});
  ViewBag.Roles=roleManager.Roles.Select(x=>x.Name!).ToList(); return View(rows);
 }
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> CreateUser(CreateUserVm vm){
  if(!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.Name) || !await roleManager.RoleExistsAsync(vm.Role)){
   TempData["Error"]=Ui("اطلاعات کاربر یا نقش انتخاب‌شده معتبر نیست.","Invalid user details or selected role.");
   return RedirectToAction(nameof(Users));
  }
  vm.Name=vm.Name.Trim();vm.Email=vm.Email.Trim();
  if(await userManager.FindByEmailAsync(vm.Email)!=null){TempData["Error"]=Ui("این ایمیل قبلاً ثبت شده است.","Email is already registered.");return RedirectToAction(nameof(Users));}
  var initials=string.Concat(vm.Name.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>x[0]));
  var u=new ApplicationUser{UserName=vm.Email,Email=vm.Email,EmailConfirmed=true,DisplayName=vm.Name,Department=vm.Department,Initials=initials,IsActive=vm.IsActive};
  var result=await userManager.CreateAsync(u,vm.Password);
  if(!result.Succeeded){TempData["Error"]=string.Join(" | ",result.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Users));}
  var assigned=await userManager.AddToRoleAsync(u,vm.Role);
  if(!assigned.Succeeded){
   // Do not leave an unexpectedly role-less account after a failed assignment.
   await userManager.DeleteAsync(u);
   TempData["Error"]=string.Join(" | ",assigned.Errors.Select(x=>x.Description));
   return RedirectToAction(nameof(Users));
  }
  await WriteAudit("UserCreate","User",u.Id,$"role={vm.Role}");
  TempData["Success"]=Ui("کاربر با موفقیت ایجاد شد.","User created successfully.");
  return RedirectToAction(nameof(Users));
 }
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> UpdateUser(EditUserVm vm){
  if(!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.Name) || !await roleManager.RoleExistsAsync(vm.Role)){
   TempData["Error"]=Ui("اطلاعات یا نقش انتخاب‌شده معتبر نیست.","Invalid details or selected role.");
   return RedirectToAction(nameof(Users));
  }
  var u=await userManager.FindByIdAsync(vm.Id);if(u==null)return NotFound();
  var existing=await userManager.GetRolesAsync(u);
  var isSelf=string.Equals(u.UserName,User.Identity?.Name,StringComparison.OrdinalIgnoreCase);
  var isBootstrapAdmin=string.Equals(u.Email,"admin@ashkancrm.local",StringComparison.OrdinalIgnoreCase);
  if((isSelf||isBootstrapAdmin)&&(!vm.IsActive||vm.Role!=Roles.Admin)){
   TempData["Error"]=Ui("نمی‌توان حساب مدیر فعلی یا مدیر اصلی را غیرفعال کرد یا نقش مدیریتی آن را برداشت.","Cannot disable or demote the current or primary administrator.");
   return RedirectToAction(nameof(Users));
  }
  // Identity and CRM audit records share CrmDbContext: commit the entire change together.
  // A failed IdentityResult leaves the previous roles and profile unchanged.
  await using var transaction=await db.Database.BeginTransactionAsync();
  if(!existing.Contains(vm.Role,StringComparer.OrdinalIgnoreCase)){
   var added=await userManager.AddToRoleAsync(u,vm.Role);
   if(!added.Succeeded){TempData["Error"]=string.Join(" | ",added.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Users));}
  }
  var obsolete=existing.Where(x=>!string.Equals(x,vm.Role,StringComparison.OrdinalIgnoreCase)).ToList();
  if(obsolete.Count>0){
   var removed=await userManager.RemoveFromRolesAsync(u,obsolete);
   if(!removed.Succeeded){TempData["Error"]=string.Join(" | ",removed.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Users));}
  }
  vm.Name=vm.Name.Trim();u.DisplayName=vm.Name;u.Department=vm.Department;u.IsActive=vm.IsActive;
  u.Initials=string.Concat(vm.Name.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>x[0]));
  var updated=await userManager.UpdateAsync(u);
  if(!updated.Succeeded){TempData["Error"]=string.Join(" | ",updated.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Users));}
  var stamp=await userManager.UpdateSecurityStampAsync(u);
  if(!stamp.Succeeded){TempData["Error"]=string.Join(" | ",stamp.Errors.Select(x=>x.Description));return RedirectToAction(nameof(Users));}
  await WriteAudit("UserUpdate","User",u.Id,$"role={vm.Role};active={vm.IsActive}");
  await transaction.CommitAsync();
  TempData["Success"]=Ui("کاربر و سطح دسترسی به‌روزرسانی شد.","User and permissions updated.");
  return RedirectToAction(nameof(Users));
 }
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> ResetUserPassword(string id,string newPassword){
  if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(newPassword)){
   TempData["Error"]=Ui("شناسه کاربر و رمز جدید الزامی هستند.","User ID and new password are required.");
   return RedirectToAction(nameof(Users));
  }
  var u=await userManager.FindByIdAsync(id);
  if(u==null)return NotFound();
  var token=await userManager.GeneratePasswordResetTokenAsync(u);
  var result=await userManager.ResetPasswordAsync(u,token,newPassword);
  if(!result.Succeeded){
   TempData["Error"]=string.Join(" | ",result.Errors.Select(x=>x.Description));
   return RedirectToAction(nameof(Users));
  }
  var stamp=await userManager.UpdateSecurityStampAsync(u);
  if(!stamp.Succeeded){
   TempData["Error"]=Ui("رمز تغییر کرد اما ابطال نشست‌های قبلی تأیید نشد؛ لطفاً بررسی کنید.","Password changed, but session invalidation could not be confirmed; please investigate.");
   return RedirectToAction(nameof(Users));
  }
  await WriteAudit("UserPasswordReset","User",u.Id,"Password reset by administrator; no secret logged");
  TempData["Success"]=Ui("رمز عبور تغییر کرد و نشست‌های قبلی نامعتبر شدند.","Password changed and previous sessions invalidated.");
  return RedirectToAction(nameof(Users));
 }
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> UnlockUser(string id){
  if(string.IsNullOrWhiteSpace(id))return BadRequest();
  var u=await userManager.FindByIdAsync(id);
  if(u==null)return NotFound();
  var unlock=await userManager.SetLockoutEndDateAsync(u,null);
  if(!unlock.Succeeded){
   TempData["Error"]=string.Join(" | ",unlock.Errors.Select(x=>x.Description));
   return RedirectToAction(nameof(Users));
  }
  var reset=await userManager.ResetAccessFailedCountAsync(u);
  if(!reset.Succeeded){
   TempData["Error"]=Ui("قفل برداشته شد، اما بازنشانی شمارنده ورود ناموفق انجام نشد.","Account unlocked, but failed-login counter could not be reset.");
   return RedirectToAction(nameof(Users));
  }
  await WriteAudit("UserUnlock","User",u.Id,"Administrator unlocked account");
  TempData["Success"]=Ui("قفل حساب باز شد.","Account unlocked successfully.");
  return RedirectToAction(nameof(Users));
 }
 [Authorize(Roles=Roles.Admin)] public IActionResult AuditLog()=>View(store.Audit);
 [Authorize(Roles=Roles.Admin),HttpGet]
 public async Task<IActionResult> Settings(){
  var row=await db.CrmOrganizationSettings.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==1);
  DateTime? localUpdated=null;
  if(row!=null){try{localUpdated=TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(row.UpdatedAtUtc,DateTimeKind.Utc),TimeZoneInfo.FindSystemTimeZoneById(row.TimeZoneId));}catch(TimeZoneNotFoundException){localUpdated=row.UpdatedAtUtc;}catch(InvalidTimeZoneException){localUpdated=row.UpdatedAtUtc;}}
  return View(new OrganizationSettingsVm{
   UpdatedAtLocal=localUpdated,OrganizationName=row?.OrganizationName??"AshkanCRM",TimeZoneId=row?.TimeZoneId??"UTC",
   ReportingCurrency=row?.ReportingCurrency??"EUR",DefaultReportingDays=row?.DefaultReportingDays??30,
   UpdatedAtUtc=row?.UpdatedAtUtc,UpdatedBy=row?.UpdatedBy??""
  });
 }
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken]
 public async Task<IActionResult> SaveOrganizationSettings(OrganizationSettingsVm input){
  input.OrganizationName=input.OrganizationName?.Trim()??"";
  var zones=new[]{"UTC","Asia/Tehran","Europe/Berlin","Europe/London","Europe/Stockholm"};
  var currencies=new[]{"EUR","USD","IRR","GBP","SEK"};
  if(!ModelState.IsValid||string.IsNullOrWhiteSpace(input.OrganizationName)||!zones.Contains(input.TimeZoneId)||!currencies.Contains(input.ReportingCurrency)){
   ModelState.AddModelError(string.Empty,Ui("مقادیر تنظیمات معتبر نیستند.","Invalid organization settings."));
   return View("Settings",input);
  }
  var row=await db.CrmOrganizationSettings.FirstOrDefaultAsync(x=>x.Id==1);
  if(row==null){row=new OrganizationSettingsEntity{Id=1};db.CrmOrganizationSettings.Add(row);}
  var detail=$"Organization settings updated: name={input.OrganizationName}, timezone={input.TimeZoneId}, currency={input.ReportingCurrency}, reportingDays={input.DefaultReportingDays}";
  row.OrganizationName=input.OrganizationName;row.TimeZoneId=input.TimeZoneId;
  row.ReportingCurrency=input.ReportingCurrency;row.DefaultReportingDays=input.DefaultReportingDays;
  row.UpdatedAtUtc=DateTime.UtcNow;row.UpdatedBy=User.Identity?.Name??"system";
  await db.SaveChangesAsync();
  await WriteAudit("SettingsUpdate","OrganizationSettings","1",detail);
  TempData["Success"]=Ui("تنظیمات سازمان ذخیره شد.","Organization settings saved.");
  return RedirectToAction(nameof(Settings));
 }

 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> DataCore(){ViewBag.Customers=await db.CrmCustomers.CountAsync();ViewBag.Leads=await db.CrmLeads.CountAsync();ViewBag.Deals=await db.CrmDeals.CountAsync();ViewBag.Audits=await db.CrmAuditEvents.CountAsync();return View();}

 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public IActionResult Quotes()=>View(store.Quotes);
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public IActionResult Products()=>View(store.Products);
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public async Task<IActionResult> Forecast(){var deals=await db.CrmDeals.AsNoTracking().ToListAsync();var now=DateTime.UtcNow;var targets=await db.CrmSalesTargets.AsNoTracking().Where(x=>x.Year==now.Year&&x.Month==now.Month&&!x.IsDeleted).ToListAsync();ViewBag.Pipeline=deals.Where(x=>x.Stage!="Won"&&x.Stage!="Lost").Sum(x=>x.Value);ViewBag.Weighted=deals.Where(x=>x.Stage!="Won"&&x.Stage!="Lost").Sum(x=>x.Value*x.Probability/100m);ViewBag.Won=deals.Where(x=>x.Stage=="Won").Sum(x=>x.Value);ViewBag.Commit=deals.Where(x=>x.Stage=="Negotiation").Sum(x=>x.Value);ViewBag.Target=targets.Sum(x=>x.TargetAmount);ViewBag.Stretch=targets.Sum(x=>x.StretchAmount);return View("SqlForecast",deals);}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> SaveSalesTarget(string owner,int year,int month,decimal targetAmount,decimal stretchAmount,string? returnTo=null){if(string.IsNullOrWhiteSpace(owner)||owner.Length>180||year<2000||year>2100||month<1||month>12||targetAmount<0||stretchAmount<0)return BadRequest(Ui("هدف فروش معتبر نیست.", "Invalid sales target"));owner=owner.Trim();var row=await db.CrmSalesTargets.FirstOrDefaultAsync(x=>x.Owner==owner&&x.Year==year&&x.Month==month);if(row==null){row=new SalesTargetEntity{Owner=owner,Year=year,Month=month,CreatedBy=User.Identity?.Name};db.CrmSalesTargets.Add(row);}row.TargetAmount=targetAmount;row.StretchAmount=stretchAmount;row.UpdatedAtUtc=DateTime.UtcNow;row.UpdatedBy=User.Identity?.Name;await db.SaveChangesAsync();await WriteAudit("TargetUpsert","SalesTarget",row.Id.ToString(),owner);return RedirectToAction(returnTo=="ManageSalesTargets"?nameof(ManageSalesTargets):nameof(Forecast),returnTo=="ManageSalesTargets"?new {year}:null);}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Marketing+","+Roles.Support)] public IActionResult Automations()=>View(store.Automations);
 public IActionResult Knowledge()=>View(store.Knowledge);
 public IActionResult CommandCenter()=>View();
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> CustomerTimeline(int id){var c=await db.CrmCustomers.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(c==null)return NotFound();if(!await CanManageScoped(c.Owner,c.TeamKey))return Forbid();ViewBag.Customer=c;return View(await db.CrmActivities.AsNoTracking().Where(x=>x.EntityType=="Customer"&&x.EntityId==id).OrderByDescending(x=>x.OccurredAtUtc).ToListAsync());}
 public async Task<IActionResult> Customer360(int? id){var access=await GetAccessAsync();var q=ApplyScope(db.CrmCustomers.AsNoTracking(),access);var c=id.HasValue?await q.FirstOrDefaultAsync(x=>x.Id==id.Value):await q.OrderByDescending(x=>x.Score).FirstOrDefaultAsync();if(c==null)return RedirectToAction(nameof(Customers));var model=new Customer(c.Id,c.Name,c.Company,c.Email,c.Phone,c.Segment,c.Owner,c.Status,c.Score);ViewBag.Deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access).Where(x=>x.Customer==c.Company).Select(x=>new Deal(x.Id,x.Title,x.Customer,x.Owner,x.Value,x.Stage,x.Probability,x.CloseDate)).ToListAsync();ViewBag.Contacts=store.Contacts.Where(x=>x.CustomerId==c.Id).ToList();ViewBag.Notes=store.Notes.Where(x=>x.CustomerId==c.Id).ToList();return View(model);}
 [Authorize(Roles=Roles.Admin)] public IActionResult Permissions()=>View(store.Permissions);
 [Authorize(Roles=Roles.Admin)] public IActionResult DataManagement()=>View(store.Imports);
 [Authorize(Roles=Roles.Admin)] public IActionResult Integrations()=>View(store.ApiKeys);
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public IActionResult RevenueIntelligence()=>View(store.Deals);
 [Authorize(Roles=Roles.Admin)] public IActionResult SecurityCenter()=>View();
 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> AdminHub(){
  var users=userManager.Users.ToList(); ViewBag.TotalUsers=users.Count; ViewBag.ActiveUsers=users.Count(x=>x.IsActive); ViewBag.LockedUsers=users.Count(x=>x.LockoutEnd>DateTimeOffset.UtcNow); ViewBag.RolesCount=roleManager.Roles.Count(); ViewBag.OpenTickets=store.Tickets.Count(x=>x.Status!="حل شده"); ViewBag.Automations=store.Automations.Count(x=>x.Enabled); return View();
 }
 public async Task<IActionResult> AiCopilot(){var access=await GetAccessAsync();var q=ApplyScope(db.CrmDeals.AsNoTracking(),access);var deals=await q.ToListAsync();var lq=ApplyScope(db.CrmLeads.AsNoTracking(),access);var leads=await lq.ToListAsync();ViewBag.Pipeline=deals.Where(x=>x.Stage!="Won"&&x.Stage!="Lost").Sum(x=>x.Value);ViewBag.Weighted=deals.Where(x=>x.Stage!="Won"&&x.Stage!="Lost").Sum(x=>x.Value*x.Probability/100m);ViewBag.HotLeads=leads.Count(x=>x.Score>=80);ViewBag.RiskyDeals=deals.Count(x=>x.Stage!="Won"&&x.Stage!="Lost"&&x.Probability<60);ViewBag.UrgentTickets=store.Tickets.Count(x=>x.Priority=="فوری"&&x.Status!="حل شده");return View(deals.Select(x=>new Deal(x.Id,x.Title,x.Customer,x.Owner,x.Value,x.Stage,x.Probability,x.CloseDate)).ToList());}
 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> Teams(){ViewBag.Members=await db.CrmTeamMembers.AsNoTracking().Where(x=>!x.IsDeleted).ToListAsync();ViewBag.Users=await userManager.Users.Where(x=>x.IsActive).OrderBy(x=>x.DisplayName).ToListAsync();return View(await db.CrmTeams.AsNoTracking().Where(x=>!x.IsDeleted).OrderBy(x=>x.Name).ToListAsync());}
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateTeam(string teamKey,string name,string department,string managerUserName){if(string.IsNullOrWhiteSpace(teamKey)||string.IsNullOrWhiteSpace(name))return RedirectToAction(nameof(Teams));if(await db.CrmTeams.AnyAsync(x=>x.TeamKey==teamKey)){TempData["Error"]=Ui("شناسه تیم تکراری است.","Team key already exists.");return RedirectToAction(nameof(Teams));}db.CrmTeams.Add(new CrmTeamEntity{TeamKey=teamKey.Trim().ToLowerInvariant(),Name=name,Department=department??"Sales",ManagerUserName=managerUserName??"",CreatedBy=User.Identity?.Name});await db.SaveChangesAsync();return RedirectToAction(nameof(Teams));}
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> AddTeamMember(int teamId,string userName,string accessScope){if(!new[]{"Own","Team","All"}.Contains(accessScope))accessScope="Own";var row=await db.CrmTeamMembers.FirstOrDefaultAsync(x=>x.TeamId==teamId&&x.UserName==userName);if(row==null)db.CrmTeamMembers.Add(new CrmTeamMemberEntity{TeamId=teamId,UserName=userName,AccessScope=accessScope,CreatedBy=User.Identity?.Name});else{row.AccessScope=accessScope;row.IsDeleted=false;row.UpdatedAtUtc=DateTime.UtcNow;}await db.SaveChangesAsync();return RedirectToAction(nameof(Teams));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public IActionResult Approvals()=>View(store.Approvals);
 [Authorize(Roles=Roles.Admin)] public IActionResult Sessions()=>View(store.Sessions);
 [Authorize(Roles=Roles.Admin)] public IActionResult FeatureFlags()=>View(store.FeatureFlags);
 [Authorize(Roles=Roles.Admin)] public IActionResult AccessStudio()=>View(store.UserPermissions);
 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> AccessHealth(){ var users=userManager.Users.ToList(); ViewBag.Total=users.Count; ViewBag.Inactive=users.Count(x=>!x.IsActive); ViewBag.Locked=users.Count(x=>x.LockoutEnd>DateTimeOffset.UtcNow); ViewBag.NoRecentLogin=users.Count(x=>x.LastLoginAtUtc==null || x.LastLoginAtUtc<DateTime.UtcNow.AddDays(-30)); ViewBag.HighRisk=store.UserPermissions.Count(x=>x.Delete||x.Approve||x.Scope=="All"); return View(store.UserPermissions); }
 [Authorize(Roles=Roles.Admin)] public IActionResult DataQuality()=>View(store.Duplicates);
 [Authorize(Roles=Roles.Admin)] public IActionResult IntegrationMonitor()=>View(store.WebhookDeliveries);
 public IActionResult SmartScoring()=>View(store.SmartScores);
 public IActionResult SavedViews()=>View(store.SavedViews);
 [Authorize(Roles=Roles.Admin),HttpPost,ValidateAntiForgeryToken] public IActionResult SavePermission(int id,bool view,bool create,bool edit,bool delete,bool export,bool approve,string scope){
  var i=store.UserPermissions.FindIndex(x=>x.Id==id); if(i<0)return NotFound(); var old=store.UserPermissions[i];
  store.UserPermissions[i]=old with {View=view,Create=create,Edit=edit,Delete=delete,Export=export,Approve=approve,Scope=scope,UpdatedBy=User.Identity?.Name??"Admin",UpdatedAt="همین حالا"};
  TempData["Success"]=Ui("دسترسی کاربر به‌روزرسانی شد.", "User permissions updated."); return RedirectToAction(nameof(AccessStudio));
 }
 [Authorize(Roles=Roles.Admin)] public IActionResult AiAgents()=>View(store.AiAgents);
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Support+","+Roles.Marketing)] public IActionResult Playbooks()=>View(store.Playbooks);
 [Authorize(Roles=Roles.Admin+","+Roles.Support)] public IActionResult SlaPolicies()=>View(store.SlaPolicies);
 [Authorize(Roles=Roles.Admin)] public IActionResult DashboardBuilder()=>View(store.DashboardWidgets);
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public IActionResult ExecutiveCockpit(){ ViewBag.Pipeline=store.Deals.Where(x=>x.Stage!="برنده").Sum(x=>x.Value); ViewBag.Weighted=store.Deals.Where(x=>x.Stage!="برنده").Sum(x=>x.Value*x.Probability/100m); ViewBag.HotLeads=store.Leads.Count(x=>x.Score>=80); ViewBag.OpenTickets=store.Tickets.Count(x=>x.Status!="حل شده"); return View(store.Deals); }
 public IActionResult AiInsights(){
  var audience=User.IsInRole(Roles.Admin)?"Admin":User.IsInRole(Roles.Support)?"Support":User.IsInRole(Roles.Marketing)?"Marketing":"Sales";
  return View(store.AiInsights.Where(x=>x.Audience==audience||User.IsInRole(Roles.Admin)).ToList());
 }

 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public async Task<IActionResult> WinLossAnalytics(){var access=await GetAccessAsync();var deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access).Where(x=>x.Stage=="Won"||x.Stage=="Lost").OrderByDescending(x=>x.ClosedAtUtc).ToListAsync();ViewBag.Won=deals.Count(x=>x.Stage=="Won");ViewBag.Lost=deals.Count(x=>x.Stage=="Lost");ViewBag.WinRate=deals.Count==0?0:Math.Round(100m*deals.Count(x=>x.Stage=="Won")/deals.Count,1);ViewBag.WonValue=deals.Where(x=>x.Stage=="Won").Sum(x=>x.Value);return View(deals);}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public async Task<IActionResult> DuplicateDetection(){var customers=await db.CrmCustomers.AsNoTracking().ToListAsync();var leads=await db.CrmLeads.AsNoTracking().ToListAsync();ViewBag.CustomerGroups=customers.GroupBy(x=>(x.Email??"").Trim().ToLower()).Where(g=>g.Key!=""&&g.Count()>1).Select(g=>g.ToList()).ToList();ViewBag.LeadGroups=leads.GroupBy(x=>((x.Name??"").Trim().ToLower(),(x.Company??"").Trim().ToLower())).Where(g=>g.Count()>1).Select(g=>g.ToList()).ToList();return View();}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> ExportSalesCsv(string entity="deals"){var access=await GetAccessAsync();var sb=new StringBuilder();if(entity=="customers"){sb.AppendLine("Id,Name,Company,Email,Owner,Team,Status,Score");foreach(var x in await ApplyScope(db.CrmCustomers.AsNoTracking(),access).ToListAsync())sb.AppendLine(Csv(x.Id,x.Name,x.Company,x.Email,x.Owner,x.TeamKey,x.Status,x.Score));}else if(entity=="leads"){sb.AppendLine("Id,Name,Company,Source,Owner,Team,Status,Score");foreach(var x in await ApplyScope(db.CrmLeads.AsNoTracking(),access).ToListAsync())sb.AppendLine(Csv(x.Id,x.Name,x.Company,x.Source,x.Owner,x.TeamKey,x.Status,x.Score));}else{sb.AppendLine("Id,Title,Customer,Owner,Team,Value,Stage,Probability,OutcomeReason");foreach(var x in await ApplyScope(db.CrmDeals.AsNoTracking(),access).ToListAsync())sb.AppendLine(Csv(x.Id,x.Title,x.Customer,x.Owner,x.TeamKey,x.Value,x.Stage,x.Probability,x.OutcomeReason));}return File(new UTF8Encoding(true).GetBytes(sb.ToString()),"text/csv",$"ashkan-crm-{entity}-{DateTime.UtcNow:yyyyMMdd}.csv");}
 public async Task<IActionResult> NextBestActions(){var access=await GetAccessAsync();var deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access).Where(x=>x.Stage!="Won"&&x.Stage!="Lost").ToListAsync();var tasks=await ApplyScope(db.CrmTasks.AsNoTracking().Where(x=>!x.IsDeleted&&x.Status!="Done"),access).ToListAsync();var result=deals.Select(d=>new NextBestActionVm{DealId=d.Id,DealTitle=d.Title,Priority=(d.Probability>=70?100:60)+(d.Value>=1000000000m?20:0),Action=d.Probability>=70?"Schedule closing call":"Create next follow-up",Reason=d.Probability>=70?"High-probability opportunity should have a concrete close plan.":"Pipeline opportunity needs a documented next step."}).OrderByDescending(x=>x.Priority).Take(12).ToList();ViewBag.OverdueTasks=tasks.Count(x=>x.DueAtUtc<DateTime.UtcNow);return View(result);}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> ProcessReminders(){await ProcessDueReminders();return RedirectToAction(nameof(Notifications));}
 private sealed record AccessCtx(string Scope,string UserName,string[] TeamKeys);
 private async Task<AccessCtx> GetAccessAsync(){var u=User.Identity?.Name??"";if(User.IsInRole(Roles.Admin))return new("All",u,[]);var memberships=await (from m in db.CrmTeamMembers.AsNoTracking() join t in db.CrmTeams.AsNoTracking() on m.TeamId equals t.Id where m.UserName==u&&!m.IsDeleted&&!t.IsDeleted&&t.IsActive select new{m.AccessScope,t.TeamKey}).ToListAsync();var scope=memberships.Any(x=>x.AccessScope=="All")?"All":memberships.Any(x=>x.AccessScope=="Team")?"Team":"Own";return new(scope,u,memberships.Select(x=>x.TeamKey).Distinct().ToArray());}
 private static IQueryable<CustomerEntity> ApplyScope(IQueryable<CustomerEntity> q,AccessCtx a)=>a.Scope=="All"?q:a.Scope=="Team"?q.Where(x=>a.TeamKeys.Contains(x.TeamKey)):q.Where(x=>x.Owner==a.UserName);
 private static IQueryable<LeadEntity> ApplyScope(IQueryable<LeadEntity> q,AccessCtx a)=>a.Scope=="All"?q:a.Scope=="Team"?q.Where(x=>a.TeamKeys.Contains(x.TeamKey)):q.Where(x=>x.Owner==a.UserName);
 private static IQueryable<DealEntity> ApplyScope(IQueryable<DealEntity> q,AccessCtx a)=>a.Scope=="All"?q:a.Scope=="Team"?q.Where(x=>a.TeamKeys.Contains(x.TeamKey)):q.Where(x=>x.Owner==a.UserName);
 private static IQueryable<CrmTaskEntity> ApplyScope(IQueryable<CrmTaskEntity> q,AccessCtx a)=>a.Scope=="All"?q:a.Scope=="Team"?q.Where(x=>a.TeamKeys.Contains(x.TeamKey)):q.Where(x=>x.Assignee==a.UserName);
 private async Task<bool> CanManageScoped(string owner,string teamKey){var a=await GetAccessAsync();return a.Scope=="All"||string.Equals(owner,a.UserName,StringComparison.OrdinalIgnoreCase)||(a.Scope=="Team"&&a.TeamKeys.Contains(teamKey));}
 private async Task<string> ResolveTeamKey(string userName){return await (from m in db.CrmTeamMembers.AsNoTracking() join t in db.CrmTeams.AsNoTracking() on m.TeamId equals t.Id where m.UserName==userName&&!m.IsDeleted&&t.IsActive orderby m.IsPrimary descending select t.TeamKey).FirstOrDefaultAsync()??"";}
 private async Task ProcessDueReminders(){var now=DateTime.UtcNow;var due=await db.CrmTasks.Where(x=>!x.IsDeleted&&x.Status!="Done"&&x.ReminderAtUtc!=null&&x.ReminderAtUtc<=now&&(x.LastReminderSentAtUtc==null||x.LastReminderSentAtUtc<x.ReminderAtUtc)).Take(100).ToListAsync();foreach(var t in due){db.CrmNotifications.Add(new NotificationEntity{UserName=t.Assignee,Type="Reminder",Title="Task reminder",Message=t.Title,CreatedBy="ReminderEngine"});t.LastReminderSentAtUtc=now;}if(due.Count>0)await db.SaveChangesAsync();}
 private static string Csv(params object?[] values)=>string.Join(",",values.Select(v=>"\""+(v?.ToString()??"").Replace("\"","\"\"")+"\""));

 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> SecurityOperations(){ViewBag.Failed24h=await db.CrmLoginAudits.AsNoTracking().CountAsync(x=>!x.Succeeded&&x.CreatedAtUtc>DateTime.UtcNow.AddDays(-1));ViewBag.Success24h=await db.CrmLoginAudits.AsNoTracking().CountAsync(x=>x.Succeeded&&x.CreatedAtUtc>DateTime.UtcNow.AddDays(-1));return View(await db.CrmLoginAudits.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync());}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager)] public async Task<IActionResult> LeadRouting(){return View(await db.CrmLeadRoutingRules.AsNoTracking().Where(x=>!x.IsDeleted).OrderBy(x=>x.Priority).ToListAsync());}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> CreateRoutingRule(LeadRoutingRuleEntity input){input.Id=0;input.CreatedBy=input.UpdatedBy=User.Identity?.Name;db.CrmLeadRoutingRules.Add(input);await db.SaveChangesAsync();await WriteAudit("Create","LeadRoutingRule",input.Id.ToString(),input.Name);return RedirectToAction(nameof(LeadRouting));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager),HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> AutoRouteLead(int id){var lead=await db.CrmLeads.FirstOrDefaultAsync(x=>x.Id==id);if(lead==null)return NotFound();if(!await CanManageScoped(lead.Owner,lead.TeamKey))return Forbid();var rule=await db.CrmLeadRoutingRules.AsNoTracking().Where(x=>x.IsActive&&!x.IsDeleted&&x.MinScore<=lead.Score&&(x.Source=="*"||x.Source==lead.Source)).OrderBy(x=>x.Priority).FirstOrDefaultAsync();if(rule==null){TempData["Error"]=Ui("قانون تخصیص مناسبی یافت نشد.","No matching routing rule.");return RedirectToAction(nameof(Leads));}lead.Owner=rule.AssignTo;lead.TeamKey=rule.TeamKey;lead.UpdatedAtUtc=DateTime.UtcNow;lead.UpdatedBy=User.Identity?.Name;await db.SaveChangesAsync();await WriteAudit("AutoRoute","Lead",id.ToString(),$"{rule.Name} -> {rule.AssignTo}");return RedirectToAction(nameof(Leads));}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> CustomerIntelligence(){var access=await GetAccessAsync();var customers=await ApplyScope(db.CrmCustomers.AsNoTracking(),access).OrderByDescending(x=>x.Score).Take(50).ToListAsync();var deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access).ToListAsync();var result=customers.Select(c=>{var cd=deals.Where(d=>d.Customer==c.Company).ToList();var open=cd.Where(d=>d.Stage!="Won"&&d.Stage!="Lost").Sum(d=>d.Value);var stalled=cd.Count(d=>d.Stage!="Won"&&d.Stage!="Lost"&&d.Probability<50);var health=Math.Clamp(c.Score-(stalled*10)+(cd.Any(d=>d.Stage=="Won")?10:0),0,100);var risk=health<50?"High":health<75?"Medium":"Low";return new CustomerIntelligenceVm{CustomerId=c.Id,Customer=c.Company,HealthScore=health,Risk=risk,OpenPipeline=open,Summary=$"Customer score {c.Score}; {cd.Count} deals; {stalled} low-confidence open deals.",RecommendedAction=risk=="High"?"Schedule an executive check-in and review open risks.":open>0?"Confirm the next milestone for active opportunities.":"Create a value-based expansion follow-up."};}).ToList();return View(result);}
 [Authorize(Roles=Roles.Admin+","+Roles.SalesManager+","+Roles.Sales)] public async Task<IActionResult> AiOperations(string? q=null){var access=await GetAccessAsync();var deals=await ApplyScope(db.CrmDeals.AsNoTracking(),access).Where(x=>x.Stage!="Won"&&x.Stage!="Lost").OrderByDescending(x=>x.Value).Take(20).ToListAsync();var leads=await ApplyScope(db.CrmLeads.AsNoTracking(),access).OrderByDescending(x=>x.Score).Take(20).ToListAsync();ViewBag.Query=q;ViewBag.Answer=string.IsNullOrWhiteSpace(q)?"Ask about pipeline risk, hot leads, or next actions.":BuildAiAnswer(q,deals,leads);ViewBag.Deals=deals;ViewBag.Leads=leads;return View();}
 private static string BuildAiAnswer(string q,List<DealEntity> deals,List<LeadEntity> leads){var t=q.ToLowerInvariant();if(t.Contains("risk")||t.Contains("ریسک")){var r=deals.Where(x=>x.Probability<50).OrderByDescending(x=>x.Value).Take(3).Select(x=>$"{x.Title} ({x.Probability}%)");return "Priority risks: "+string.Join("; ",r);}if(t.Contains("lead")||t.Contains("سرنخ")){return "Top leads: "+string.Join("; ",leads.Take(5).Select(x=>$"{x.Name} / {x.Company} ({x.Score})"));}var pipeline=deals.Sum(x=>x.Value);var weighted=deals.Sum(x=>x.Value*x.Probability/100m);return $"Open pipeline: {pipeline:N0}; weighted: {weighted:N0}. Highest-value next focus: "+(deals.FirstOrDefault()?.Title??"No open deal")+".";}
 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> DataGovernance(){ViewBag.Customers=await db.CrmCustomers.IgnoreQueryFilters().CountAsync();ViewBag.Leads=await db.CrmLeads.IgnoreQueryFilters().CountAsync();ViewBag.Deals=await db.CrmDeals.IgnoreQueryFilters().CountAsync();ViewBag.Audit=await db.CrmAuditEvents.CountAsync();ViewBag.OldAudit=await db.CrmAuditEvents.CountAsync(x=>x.CreatedAtUtc<DateTime.UtcNow.AddDays(-365));return View();}
 [Authorize(Roles=Roles.Admin)] public async Task<IActionResult> BackupExport(){var payload=new{exportedAtUtc=DateTime.UtcNow,customers=await db.CrmCustomers.IgnoreQueryFilters().AsNoTracking().ToListAsync(),leads=await db.CrmLeads.IgnoreQueryFilters().AsNoTracking().ToListAsync(),deals=await db.CrmDeals.IgnoreQueryFilters().AsNoTracking().ToListAsync(),tasks=await db.CrmTasks.AsNoTracking().ToListAsync(),teams=await db.CrmTeams.AsNoTracking().ToListAsync()};var json=System.Text.Json.JsonSerializer.Serialize(payload,new System.Text.Json.JsonSerializerOptions{WriteIndented=true});return File(Encoding.UTF8.GetBytes(json),"application/json",$"ashkan-crm-backup-{DateTime.UtcNow:yyyyMMdd-HHmm}.json");}

}
