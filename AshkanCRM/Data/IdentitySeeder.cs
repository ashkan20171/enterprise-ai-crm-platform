using AshkanCRM.Models;
using AshkanCRM.Models.Identity;
using Microsoft.AspNetCore.Identity;
namespace AshkanCRM.Data;
public static class IdentitySeeder {
 public static async Task SeedAsync(IServiceProvider services) {
  using var scope=services.CreateScope();
  var roleManager=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
  var userManager=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
  foreach(var role in new[]{Roles.Admin,Roles.SalesManager,Roles.Sales,Roles.Support,Roles.Marketing})
   if(!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));
  await Ensure(userManager,"admin@ashkancrm.local","Admin123!","مدیر سیستم","مدیریت","AM",Roles.Admin);
  await Ensure(userManager,"sales@ashkancrm.local","Sales123!","سارا احمدی","فروش","SA",Roles.SalesManager);
  await Ensure(userManager,"salesrep@ashkancrm.local","SalesRep123!","رضا محمدی","فروش","RM",Roles.Sales);
  await Ensure(userManager,"support@ashkancrm.local","Support123!","علی رضایی","پشتیبانی","AR",Roles.Support);
  await Ensure(userManager,"marketing@ashkancrm.local","Marketing123!","مریم کریمی","بازاریابی","MK",Roles.Marketing);
 }
 static async Task Ensure(UserManager<ApplicationUser> manager,string email,string password,string name,string department,string initials,string role){
  var user=await manager.FindByEmailAsync(email);
  if(user is null){user=new ApplicationUser{UserName=email,Email=email,EmailConfirmed=true,DisplayName=name,Department=department,Initials=initials,IsActive=true};var result=await manager.CreateAsync(user,password);if(!result.Succeeded) throw new InvalidOperationException(string.Join("; ",result.Errors.Select(x=>x.Description)));}
  if(!await manager.IsInRoleAsync(user,role)) await manager.AddToRoleAsync(user,role);
 }
}
