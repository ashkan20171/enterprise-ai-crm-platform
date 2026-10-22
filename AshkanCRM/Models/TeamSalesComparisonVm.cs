namespace AshkanCRM.Models;
public sealed class TeamSalesComparisonVm {
 public int Months { get; set; }
 public List<TeamSalesComparisonRow> Rows { get; set; } = new();
}
public sealed class TeamSalesComparisonRow {
 public string TeamKey { get; set; } = "";
 public string TeamName { get; set; } = "";
 public decimal Target { get; set; }
 public decimal Actual { get; set; }
 public int WonDeals { get; set; }
 public decimal Attainment => Target > 0 ? Actual / Target * 100m : 0m;
}
