namespace AshkanCRM.Models;
public sealed class SalesAgentPerformanceVm {
 public string Owner {get;set;}="";
 public int WonCount {get;set;}
 public int LostCount {get;set;}
 public decimal WonValue {get;set;}
 public decimal OpenValue {get;set;}
 public double WinRate => WonCount+LostCount==0 ? 0 : 100.0*WonCount/(WonCount+LostCount);
}
