namespace AshkanCRM.Models;
public sealed record SupportSlaRow(int Id,string Subject,string Priority,string AssignedTo,DateTime DeadlineUtc,bool Answered,bool Breached);
