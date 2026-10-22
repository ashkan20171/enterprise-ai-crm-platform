using System.ComponentModel.DataAnnotations;
namespace AshkanCRM.Models;
public sealed class OrganizationSettingsEntity {
 public int Id {get;set;}
 [Required, StringLength(120)] public string OrganizationName {get;set;}="AshkanCRM";
 [Required, StringLength(64)] public string TimeZoneId {get;set;}="UTC";
 [Required, StringLength(8)] public string ReportingCurrency {get;set;}="EUR";
 [Range(7,365)] public int DefaultReportingDays {get;set;}=30;
 public DateTime UpdatedAtUtc {get;set;}=DateTime.UtcNow;
 [StringLength(180)] public string UpdatedBy {get;set;}="";
}
public sealed class OrganizationSettingsVm {
 [Required, StringLength(120)] public string OrganizationName {get;set;}="AshkanCRM";
 [Required] public string TimeZoneId {get;set;}="UTC";
 [Required] public string ReportingCurrency {get;set;}="EUR";
 [Range(7,365)] public int DefaultReportingDays {get;set;}=30;
 public DateTime? UpdatedAtUtc {get;set;}
 public DateTime? UpdatedAtLocal {get;set;}
 public string UpdatedBy {get;set;}="";
}
