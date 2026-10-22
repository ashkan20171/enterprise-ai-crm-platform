using System.ComponentModel.DataAnnotations;
namespace AshkanCRM.Models;
public class SupportTicket {
 public int Id {get;set;}
 [Required, StringLength(180)] public string Subject {get;set;}="";
 [Required, StringLength(3000)] public string Description {get;set;}="";
 [StringLength(30)] public string Status {get;set;}="Open";
 [StringLength(30)] public string Priority {get;set;}="Medium";
 [StringLength(100)] public string Category {get;set;}="General";
 [StringLength(180)] public string Requester {get;set;}="";
 [StringLength(180)] public string AssignedTo {get;set;}="";
 public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow;
 public DateTime UpdatedAtUtc {get;set;}=DateTime.UtcNow;
 public DateTime? ResolvedAtUtc {get;set;}
 public List<SupportTicketReply> Replies {get;set;}=new();
}
public class SupportTicketReply {
 public int Id {get;set;}
 public int TicketId {get;set;}
 public SupportTicket Ticket {get;set;}=null!;
 [Required, StringLength(3000)] public string Message {get;set;}="";
 [StringLength(180)] public string Author {get;set;}="";
 public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow;
}

public class SupportSatisfaction {
 public int Id {get;set;}
 public int TicketId {get;set;}
 [Range(1,5)] public int Rating {get;set;}
 [StringLength(1000)] public string Comment {get;set;}="";
 [StringLength(180)] public string SubmittedBy {get;set;}="";
 public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow;
}

public class SupportKnowledgeArticle {
 public int Id {get;set;}
 [Required,StringLength(180)] public string Title {get;set;}="";
 [Required,StringLength(6000)] public string Content {get;set;}="";
 [StringLength(100)] public string Category {get;set;}="General";
 [StringLength(180)] public string Author {get;set;}="";
 public bool IsPublished {get;set;}=true;
 public DateTime UpdatedAtUtc {get;set;}=DateTime.UtcNow;
}
public class SupportEscalation {
 public int Id {get;set;}
 public int TicketId {get;set;}
 [Required,StringLength(1000)] public string Reason {get;set;}="";
 [StringLength(180)] public string Actor {get;set;}="";
 public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow;
}

public class SupportAttachment {
 public int Id {get;set;}
 public int TicketId {get;set;}
 [Required,StringLength(180)] public string FileName {get;set;}="";
 [Required,StringLength(100)] public string ContentType {get;set;}="";
 [Required] public byte[] Data {get;set;}=Array.Empty<byte>();
 [StringLength(180)] public string UploadedBy {get;set;}="";
 public DateTime UploadedAtUtc {get;set;}=DateTime.UtcNow;
}
