using AshkanCRM.Data;
using AshkanCRM.Models.Identity;
using AshkanCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<CrmStore>();
builder.Services.AddDbContext<CrmDbContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser,IdentityRole>(o=>{
 o.Password.RequiredLength=8;o.Password.RequireDigit=true;o.Password.RequireUppercase=true;o.Password.RequireLowercase=true;o.Password.RequireNonAlphanumeric=true;
 o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);o.User.RequireUniqueEmail=true;
}).AddEntityFrameworkStores<CrmDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o=>{o.LoginPath="/Auth/Login";o.AccessDeniedPath="/Auth/Denied";o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=true;o.Cookie.HttpOnly=true;o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;});
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddFixedWindowLimiter("auth",x=>{x.PermitLimit=10;x.Window=TimeSpan.FromMinutes(1);x.QueueLimit=0;x.AutoReplenishment=true;});});
var app=builder.Build();
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Home/Error");app.UseHsts();}
app.UseHttpsRedirection();app.Use(async(ctx,next)=>{ctx.Response.Headers["X-Content-Type-Options"]="nosniff";ctx.Response.Headers["X-Frame-Options"]="DENY";ctx.Response.Headers["Referrer-Policy"]="strict-origin-when-cross-origin";ctx.Response.Headers["Permissions-Policy"]="camera=(), microphone=(), geolocation=()";ctx.Response.Headers["X-Correlation-ID"]=ctx.TraceIdentifier;await next();});app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();app.MapStaticAssets();
using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<CrmDbContext>();await db.Database.EnsureCreatedAsync();}
await IdentitySeeder.SeedAsync(app.Services);
await CrmDataBootstrapper.SeedAsync(app.Services);
app.MapHealthChecks("/health");
app.MapControllerRoute(name:"default",pattern:"{controller=CRM}/{action=Dashboard}/{id?}").WithStaticAssets();app.Run();
