namespace AshkanCRM.Models;

/// <summary>Actual won deals grouped by the UTC month of ClosedAtUtc.</summary>
public sealed class MonthlyRealizedSalesVm
{
    public DateTime Month { get; set; }
    public decimal Amount { get; set; }
    public int WonDeals { get; set; }
}
