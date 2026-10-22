namespace AshkanCRM.Models;

/// <summary>Actual won sales over matching elapsed UTC durations in adjacent months.</summary>
public sealed class ComparableSalesVm
{
 public decimal CurrentAmount { get; set; }
 public decimal PreviousAmount { get; set; }
 public int CurrentWon { get; set; }
 public int PreviousWon { get; set; }
 public DateTime CurrentStartUtc { get; set; }
 public DateTime PreviousStartUtc { get; set; }
 public DateTime PreviousEndUtc { get; set; }
 public DateTime AsOfUtc { get; set; }
 public decimal? GrowthPercent => PreviousAmount != 0m
  ? (CurrentAmount - PreviousAmount) / Math.Abs(PreviousAmount) * 100m : null;
}
