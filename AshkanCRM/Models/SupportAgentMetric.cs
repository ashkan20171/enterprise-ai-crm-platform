namespace AshkanCRM.Models;
/// <summary>Read-only support workload metrics; no schema migration required.</summary>
public sealed class SupportAgentMetric {
 public string Agent {get;set;}="";
 public int Total {get;set;}
 public int Active {get;set;}
 public int Resolved {get;set;}
 public int CriticalOpen {get;set;}
 public double AverageResolutionHours {get;set;}
}
