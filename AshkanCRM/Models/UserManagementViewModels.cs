using System.ComponentModel.DataAnnotations;
namespace AshkanCRM.Models;
public class UserAdminRow { public string Id {get;set;}=""; public string Name{get;set;}=""; public string Email{get;set;}=""; public string Department{get;set;}=""; public string Initials{get;set;}=""; public bool IsActive{get;set;} public bool IsLocked{get;set;} public DateTime? LastLoginAtUtc{get;set;} public IList<string> Roles{get;set;}=[]; }
public class CreateUserVm { [Required] public string Name{get;set;}=""; [Required,EmailAddress] public string Email{get;set;}=""; public string Department{get;set;}=""; [Required,MinLength(8)] public string Password{get;set;}=""; public string Role{get;set;}=Roles.Sales; public bool IsActive{get;set;}=true; }
public class EditUserVm { public string Id{get;set;}=""; [Required] public string Name{get;set;}=""; public string Department{get;set;}=""; public string Role{get;set;}=Roles.Sales; public bool IsActive{get;set;}=true; }
