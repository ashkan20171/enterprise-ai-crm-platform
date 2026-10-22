using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace AshkanCRM.Services;

/// <summary>Configurable business-hours calendar. Deadlines are returned in UTC.</summary>
public static class SupportBusinessCalendar
{
    public static DateTime AddBusinessHours(DateTime startUtc, int hours) =>
        AddBusinessHours(startUtc, hours, null);

    public static DateTime AddBusinessHours(DateTime startUtc, int hours, IConfiguration? config)
    {
        if (hours < 0 || hours > 10000) throw new ArgumentOutOfRangeException(nameof(hours));
        var zoneName = config?["SupportSla:TimeZone"] ?? "UTC";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneName);
        var opening = int.TryParse(config?["SupportSla:OpeningHour"], out var o) ? o : 9;
        var closing = int.TryParse(config?["SupportSla:ClosingHour"], out var c) ? c : 17;
        if (opening < 0 || closing > 24 || opening >= closing) throw new InvalidOperationException("Invalid SupportSla opening/closing hours.");
        var holidays = (config?.GetSection("SupportSla:Holidays").Get<string[]>() ?? Array.Empty<string>())
            .Select(x => DateOnly.TryParseExact(x, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : throw new InvalidOperationException("Invalid SupportSla holiday."))
            .ToHashSet();
        var workingDays = config?.GetSection("SupportSla:WorkingDays").Get<string[]>() ?? new[] {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday"};
        var days = new HashSet<DayOfWeek>();
        foreach (var value in workingDays)
        {
            if (!Enum.TryParse<DayOfWeek>(value, true, out var parsed)) throw new InvalidOperationException("Invalid SupportSla working day.");
            days.Add(parsed);
        }
        if (days.Count == 0) throw new InvalidOperationException("SupportSla requires at least one working day.");
        var cursor = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var remaining = TimeSpan.FromHours(hours);
        if (remaining == TimeSpan.Zero) return cursor;
        // Traverse local business windows, converting each boundary independently for DST safety.
        for (var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(cursor, zone)); day < DateOnly.FromDateTime(cursor).AddYears(10); day = day.AddDays(1))
        {
            if (!days.Contains(day.DayOfWeek) || holidays.Contains(day)) continue;
            var localOpen = day.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(opening)), DateTimeKind.Unspecified);
            var localClose = day.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(closing)), DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(localOpen) || zone.IsInvalidTime(localClose)) throw new InvalidOperationException("SLA hours overlap a daylight-saving gap.");
            var openUtc = TimeZoneInfo.ConvertTimeToUtc(localOpen, zone);
            var closeUtc = TimeZoneInfo.ConvertTimeToUtc(localClose, zone);
            if (cursor < openUtc) cursor = openUtc;
            if (cursor >= closeUtc) continue;
            var available = closeUtc - cursor;
            var take = available < remaining ? available : remaining;
            cursor += take;
            remaining -= take;
            if (remaining == TimeSpan.Zero) return cursor;
        }
        throw new InvalidOperationException("SLA deadline exceeded the supported calendar horizon.");
    }
}
