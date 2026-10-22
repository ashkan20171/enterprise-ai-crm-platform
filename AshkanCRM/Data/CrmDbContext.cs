using AshkanCRM.Models.Identity;
using AshkanCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace AshkanCRM.Data;
public class CrmDbContext(DbContextOptions<CrmDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
 public DbSet<SidebarPreferenceEntity> CrmSidebarPreferences => Set<SidebarPreferenceEntity>();
 public DbSet<OrganizationSettingsEntity> CrmOrganizationSettings => Set<OrganizationSettingsEntity>();
 public DbSet<CustomerEntity> CrmCustomers => Set<CustomerEntity>();
 public DbSet<LeadEntity> CrmLeads => Set<LeadEntity>();
 public DbSet<DealEntity> CrmDeals => Set<DealEntity>();
 public DbSet<AuditEventEntity> CrmAuditEvents => Set<AuditEventEntity>();
 public DbSet<ActivityEntity> CrmActivities => Set<ActivityEntity>();
 public DbSet<NotificationEntity> CrmNotifications => Set<NotificationEntity>();
 public DbSet<CrmTaskEntity> CrmTasks => Set<CrmTaskEntity>();
 public DbSet<SalesTargetEntity> CrmSalesTargets => Set<SalesTargetEntity>();
 public DbSet<CrmTeamEntity> CrmTeams => Set<CrmTeamEntity>();
 public DbSet<CrmTeamMemberEntity> CrmTeamMembers => Set<CrmTeamMemberEntity>();
 public DbSet<LoginAuditEntity> CrmLoginAudits => Set<LoginAuditEntity>();
 public DbSet<LeadRoutingRuleEntity> CrmLeadRoutingRules => Set<LeadRoutingRuleEntity>();
 public DbSet<SupportSatisfaction> SupportSatisfactions => Set<SupportSatisfaction>();
 public DbSet<SupportKnowledgeArticle> SupportKnowledgeArticles => Set<SupportKnowledgeArticle>();
 public DbSet<SupportEscalation> SupportEscalations => Set<SupportEscalation>();
 public DbSet<SupportAttachment> SupportAttachments => Set<SupportAttachment>();
 public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
 public DbSet<SupportTicketReply> SupportTicketReplies => Set<SupportTicketReply>();
 protected override void OnModelCreating(ModelBuilder b){
  base.OnModelCreating(b);
  // SQL Server nvarchar(n) only supports n <= 4000; preserve 6000-character validation via StringLength.
  b.Entity<SupportKnowledgeArticle>().Property(x=>x.Content).HasColumnType("nvarchar(max)");
  b.Entity<SupportTicket>().HasIndex(x=>new{x.Status,x.Priority});
  b.Entity<SupportTicketReply>().HasOne(x=>x.Ticket).WithMany(x=>x.Replies).HasForeignKey(x=>x.TicketId).OnDelete(DeleteBehavior.Cascade);
  b.Entity<CustomerEntity>().HasQueryFilter(x=>!x.IsDeleted);
  b.Entity<LeadEntity>().HasQueryFilter(x=>!x.IsDeleted);
  b.Entity<DealEntity>().HasQueryFilter(x=>!x.IsDeleted);
  b.Entity<CustomerEntity>().HasIndex(x=>x.Email);
  b.Entity<LeadEntity>().HasIndex(x=>new{x.Company,x.Name});
  b.Entity<CrmTeamEntity>().HasIndex(x=>x.TeamKey).IsUnique(); b.Entity<CrmTeamMemberEntity>().HasIndex(x=>new{x.TeamId,x.UserName}).IsUnique();
  b.Entity<DealEntity>().Property(x=>x.Value).HasPrecision(18,2); b.Entity<SalesTargetEntity>().Property(x=>x.TargetAmount).HasPrecision(18,2); b.Entity<SalesTargetEntity>().Property(x=>x.StretchAmount).HasPrecision(18,2);
 }
}
