using System.Security.Claims;
using Microsoft.Extensions.Localization;
using AshkanCRM.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AshkanCRM.Data;
using AshkanCRM.Models;
using Microsoft.AspNetCore.RateLimiting;
namespace AshkanCRM.Controllers;
public class AuthController(UserManager<ApplicationUser> users,SignInManager<ApplicationUser> signIn,CrmDbContext db,IStringLocalizer<AshkanCRM.SharedResource> localizer):Controller{
 [HttpGet] public IActionResult Login()=>User.Identity?.IsAuthenticated==true?RedirectToAction("Dashboard","CRM"):View();
 [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting("auth")] public async Task<IActionResult> Login(string email,string password,bool remember=false){
  var user=await users.FindByEmailAsync(email);
  if(user is null||!user.IsActive){await AuditLogin(email,false,"Invalid or inactive account");ViewBag.Error=localizer["InvalidCredentials"].Value;return View();}
  var result=await signIn.PasswordSignInAsync(user,password,remember,lockoutOnFailure:true);
  if(!result.Succeeded){await AuditLogin(email,false,result.IsLockedOut?"Locked out":"Invalid credentials");ViewBag.Error=result.IsLockedOut?localizer["LockedOut"].Value:localizer["IncorrectCredentials"].Value;return View();}
  user.LastLoginAtUtc=DateTime.UtcNow;await users.UpdateAsync(user);await AuditLogin(email,true,"Success");
  return RedirectToAction("Dashboard","CRM");
 }
 [HttpGet] public IActionResult Register()=>View();
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Register(string name,string email,string company,string password="Temp123!"){
  if(await users.FindByEmailAsync(email) is not null){ViewBag.Error=localizer["EmailExists"].Value;return View();}
  var initials=string.Concat((name??"").Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>x[0]));
  var user=new ApplicationUser{UserName=email,Email=email,DisplayName=name??email,Department=company??"",Initials=initials,IsActive=false};
  var result=await users.CreateAsync(user,password);
  if(!result.Succeeded){ViewBag.Error=string.Join(" | ",result.Errors.Select(x=>localizer[IdentityErrorKey(x.Code)].Value));return View();}
  await users.AddToRoleAsync(user,"Sales");ViewBag.Success=localizer["RegistrationPending"].Value;return View();
 }
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return RedirectToAction(nameof(Login));}
 private static string IdentityErrorKey(string code)=>code switch{
  "DuplicateUserName"=>"DuplicateUserName", "DuplicateEmail"=>"DuplicateEmail",
  "PasswordTooShort"=>"PasswordTooShort", "PasswordRequiresDigit"=>"PasswordRequiresDigit",
  "PasswordRequiresUpper"=>"PasswordRequiresUpper", "PasswordRequiresLower"=>"PasswordRequiresLower",
  "PasswordRequiresNonAlphanumeric"=>"PasswordRequiresNonAlphanumeric", _=>"RegistrationFailed"};
 private async Task AuditLogin(string email,bool ok,string result){db.CrmLoginAudits.Add(new LoginAuditEntity{UserName=email??"",IpAddress=HttpContext.Connection.RemoteIpAddress?.ToString()??"",UserAgent=Request.Headers["User-Agent"].ToString(),Succeeded=ok,Result=result,CreatedBy="Auth"});await db.SaveChangesAsync();}
 public IActionResult Denied()=>View();
}
