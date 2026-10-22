namespace AshkanCRM.Models;
public sealed class SupportSlaSettingsViewModel {
 public string TimeZone {get;set;}="UTC";
 public int OpeningHour {get;set;}
 public int ClosingHour {get;set;}
 public string[] WorkingDays {get;set;}=System.Array.Empty<string>();
 public string[] Holidays {get;set;}=System.Array.Empty<string>();
 public int CriticalHours {get;set;}
 public int HighHours {get;set;}
 public int MediumHours {get;set;}
 public int LowHours {get;set;}
}
