namespace AshkanCRM.Models;
public sealed class SalesTargetManagementRow {
 public string Owner {get;set;}="";
 public int Year {get;set;}
 public int Month {get;set;}
 public decimal Target {get;set;}
 public decimal Stretch {get;set;}
}
public sealed class SalesTargetManagementVm {
 public int Year {get;set;}
 public List<string> Owners {get;set;}=new();
 public List<SalesTargetManagementRow> Rows {get;set;}=new();
}
