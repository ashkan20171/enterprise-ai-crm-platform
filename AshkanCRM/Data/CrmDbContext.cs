using AshkanCRM.Models.Identity;
using AshkanCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace AshkanCRM.Data;
public class CrmDbContext(DbContextOptions<CrmDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
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
 protected override void OnModelCreating(ModelBuilder b){
  base.OnModelCreating(b);
  b.Entity<CustomerEntity>().HasQueryFilter(x=>!x.IsDeleted);
  b.Entity<LeadEntity>().HasQueryFilter(x=>!x.IsDeleted);
  b.Entity<DealEntity>().HasQueryFilter(x=>!x.IsDeleted);
  b.Entity<CustomerEntity>().HasIndex(x=>x.Email);
  b.Entity<LeadEntity>().HasIndex(x=>new{x.Company,x.Name});
  b.Entity<CrmTeamEntity>().HasIndex(x=>x.TeamKey).IsUnique(); b.Entity<CrmTeamMemberEntity>().HasIndex(x=>new{x.TeamId,x.UserName}).IsUnique();
  b.Entity<DealEntity>().Property(x=>x.Value).HasPrecision(18,2); b.Entity<SalesTargetEntity>().Property(x=>x.TargetAmount).HasPrecision(18,2); b.Entity<SalesTargetEntity>().Property(x=>x.StretchAmount).HasPrecision(18,2);
 }
}
