using System.ComponentModel.DataAnnotations;
namespace AshkanCRM.Models;
public abstract class AuditableEntity {
 public int Id {get;set;} public DateTime CreatedAtUtc {get;set;}=DateTime.UtcNow; public DateTime UpdatedAtUtc {get;set;}=DateTime.UtcNow;
 public bool IsDeleted {get;set;} public string? CreatedBy {get;set;} public string? UpdatedBy {get;set;}
}
public class CustomerEntity:AuditableEntity {
 [Required,MaxLength(120)] public string Name {get;set;}=""; [Required,MaxLength(160)] public string Company {get;set;}="";
 [EmailAddress,MaxLength(180)] public string Email {get;set;}=""; [MaxLength(40)] public string Phone {get;set;}=""; [MaxLength(60)] public string Segment {get;set;}="SMB";
 [MaxLength(160)] public string Owner {get;set;}=""; [MaxLength(80)] public string TeamKey {get;set;}=""; [MaxLength(60)] public string Status {get;set;}="Active"; [Range(0,100)] public int Score {get;set;}=50;
 [Timestamp] public byte[]? RowVersion {get;set;}
}
public class LeadEntity:AuditableEntity {
 [Required,MaxLength(120)] public string Name {get;set;}=""; [MaxLength(160)] public string Company {get;set;}=""; [MaxLength(80)] public string Source {get;set;}="Direct";
 [MaxLength(160)] public string Owner {get;set;}=""; [MaxLength(80)] public string TeamKey {get;set;}=""; [MaxLength(60)] public string Status {get;set;}="New"; [Range(0,100)] public int Score {get;set;}=50; [MaxLength(80)] public string LastContact {get;set;}="-";
}
public class DealEntity:AuditableEntity {
 [Required,MaxLength(180)] public string Title {get;set;}=""; [MaxLength(160)] public string Customer {get;set;}=""; [MaxLength(160)] public string Owner {get;set;}=""; [MaxLength(80)] public string TeamKey {get;set;}="";
 [Range(0,double.MaxValue)] public decimal Value {get;set;} [MaxLength(60)] public string Stage {get;set;}="Qualification"; [Range(0,100)] public int Probability {get;set;}=20; [MaxLength(40)] public string CloseDate {get;set;}=""; [MaxLength(500)] public string OutcomeReason {get;set;}=""; public DateTime? ClosedAtUtc {get;set;}
}
public class AuditEventEntity:AuditableEntity {
 [MaxLength(180)] public string Actor {get;set;}=""; [MaxLength(80)] public string Action {get;set;}=""; [MaxLength(120)] public string EntityType {get;set;}=""; [MaxLength(80)] public string EntityId {get;set;}=""; [MaxLength(1000)] public string Detail {get;set;}="";
}
public class ActivityEntity:AuditableEntity {
 [MaxLength(40)] public string EntityType {get;set;}="Customer"; public int EntityId {get;set;} [MaxLength(80)] public string Type {get;set;}="Note";
 [Required,MaxLength(240)] public string Subject {get;set;}=""; [MaxLength(2000)] public string Detail {get;set;}=""; [MaxLength(160)] public string Owner {get;set;}=""; public DateTime OccurredAtUtc {get;set;}=DateTime.UtcNow;
}

public class NotificationEntity:AuditableEntity {
 [MaxLength(180)] public string UserName {get;set;}=""; [MaxLength(80)] public string Type {get;set;}="Info"; [Required,MaxLength(240)] public string Title {get;set;}=""; [MaxLength(1000)] public string Message {get;set;}=""; public bool IsRead {get;set;} public DateTime? ReadAtUtc {get;set;}
}

public class CrmTaskEntity:AuditableEntity {
 [Required,MaxLength(240)] public string Title {get;set;}=""; [MaxLength(80)] public string Type {get;set;}="FollowUp"; [MaxLength(180)] public string Assignee {get;set;}=""; [MaxLength(80)] public string TeamKey {get;set;}=""; [MaxLength(40)] public string Priority {get;set;}="Medium"; [MaxLength(40)] public string Status {get;set;}="Open"; public DateTime DueAtUtc {get;set;}=DateTime.UtcNow.AddDays(1); public DateTime? ReminderAtUtc {get;set;} [MaxLength(40)] public string RelatedEntityType {get;set;}=""; public int? RelatedEntityId {get;set;} [MaxLength(1200)] public string Description {get;set;}=""; public bool IsRecurring {get;set;} [MaxLength(20)] public string Recurrence {get;set;}="None"; public DateTime? LastReminderSentAtUtc {get;set;}
}

public class GlobalSearchVm { public List<CustomerEntity> Customers {get;set;}=[]; public List<LeadEntity> Leads {get;set;}=[]; public List<DealEntity> Deals {get;set;}=[]; }

public class SalesTargetEntity:AuditableEntity { [MaxLength(180)] public string Owner {get;set;}=""; public int Year {get;set;}=DateTime.UtcNow.Year; public int Month {get;set;}=DateTime.UtcNow.Month; public decimal TargetAmount {get;set;} public decimal StretchAmount {get;set;} }

public class CrmTeamEntity:AuditableEntity { [Required,MaxLength(80)] public string TeamKey {get;set;}=""; [Required,MaxLength(160)] public string Name {get;set;}=""; [MaxLength(120)] public string Department {get;set;}="Sales"; [MaxLength(180)] public string ManagerUserName {get;set;}=""; public bool IsActive {get;set;}=true; }
public class CrmTeamMemberEntity:AuditableEntity { public int TeamId {get;set;} [Required,MaxLength(180)] public string UserName {get;set;}=""; [MaxLength(20)] public string AccessScope {get;set;}="Own"; public bool IsPrimary {get;set;}=true; }
public class NextBestActionVm { public int DealId {get;set;} public string DealTitle {get;set;}=""; public string Action {get;set;}=""; public string Reason {get;set;}=""; public int Priority {get;set;} }

public class LoginAuditEntity:AuditableEntity { [MaxLength(180)] public string UserName {get;set;}=""; [MaxLength(64)] public string IpAddress {get;set;}=""; [MaxLength(500)] public string UserAgent {get;set;}=""; public bool Succeeded {get;set;} [MaxLength(120)] public string Result {get;set;}=""; }
public class LeadRoutingRuleEntity:AuditableEntity { [Required,MaxLength(120)] public string Name {get;set;}=""; [MaxLength(80)] public string Source {get;set;}="*"; public int MinScore {get;set;}=0; [MaxLength(180)] public string AssignTo {get;set;}=""; [MaxLength(80)] public string TeamKey {get;set;}=""; public int Priority {get;set;}=100; public bool IsActive {get;set;}=true; }
public class CustomerIntelligenceVm { public int CustomerId {get;set;} public string Customer {get;set;}=""; public int HealthScore {get;set;} public string Risk {get;set;}="Low"; public string Summary {get;set;}=""; public string RecommendedAction {get;set;}=""; public decimal OpenPipeline {get;set;} }
