namespace AshkanCRM.Models;
public sealed class SupportAgentLoad {
 public string UserName {get;set;}="";
 public int ActiveTickets {get;set;}
 public int Capacity {get;set;}=15;
 public bool Available {get;set;}=true;
 public System.Collections.Generic.List<string> Categories {get;set;}=new();
}
public sealed class SupportAssignmentQueueViewModel {
 public System.Collections.Generic.List<SupportAgentLoad> Agents {get;set;}=new();
 public System.Collections.Generic.List<SupportTicket> Unassigned {get;set;}=new();
}
