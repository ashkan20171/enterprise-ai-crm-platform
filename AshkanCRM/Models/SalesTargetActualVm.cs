namespace AshkanCRM.Models;

public sealed class SalesTargetActualVm
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Target { get; set; }
    public decimal Actual { get; set; }
    public decimal Stretch { get; set; }
    public int WonCount { get; set; }
    public decimal Attainment => Target > 0 ? Actual / Target * 100m : 0m;
}

public sealed class SalesTargetActualReportVm
{
    public int Months { get; set; }
    public string? Owner { get; set; }
    public List<string> Owners { get; set; } = new();
    public List<SalesTargetActualVm> Rows { get; set; } = new();
    public decimal TotalTarget => Rows.Sum(x => x.Target);
    public decimal TotalActual => Rows.Sum(x => x.Actual);
    public decimal OverallAttainment => TotalTarget > 0 ? TotalActual / TotalTarget * 100m : 0m;
}
