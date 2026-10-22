namespace AshkanCRM.Models;
public sealed class SalesLeaderVm { public string Owner {get;set;}=""; public decimal Amount {get;set;} public int WonDeals {get;set;} }
public sealed class YearSalesVm {
 public int CurrentYear {get;set;}
 public decimal CurrentAmount {get;set;}
 public decimal PreviousAmount {get;set;}
 public int CurrentWon {get;set;}
 public int PreviousWon {get;set;}
 public DateTime AsOfUtc {get;set;}
 public List<SalesLeaderVm> Leaders {get;set;}=new();
 public decimal? GrowthPercent => PreviousAmount==0m ? null : (CurrentAmount-PreviousAmount)/Math.Abs(PreviousAmount)*100m;
}
