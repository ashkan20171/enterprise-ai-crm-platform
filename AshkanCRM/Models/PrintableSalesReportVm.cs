namespace AshkanCRM.Models;
public sealed class PrintableSalesReportVm {
 public string TitleFa {get;set;}="";
 public string TitleEn {get;set;}="";
 public int Months {get;set;}
 public string OrganizationName {get;set;}="AshkanCRM";
 public string ReportingCurrency {get;set;}="EUR";
 public string TimeZoneId {get;set;}="UTC";
 public DateTime GeneratedAtLocal {get;set;}=DateTime.UtcNow;
 public string? Filter {get;set;}
 public string[] HeadersFa {get;set;}=System.Array.Empty<string>();
 public string[] HeadersEn {get;set;}=System.Array.Empty<string>();
 public System.Collections.Generic.List<string[]> Rows {get;set;}=new();
}
