using Journal.Models;

namespace Journal.Services;

public static class StreakService
{
    public static (int current, int longest, int missedDays) CalculateStreaks(IEnumerable<JournalEntry> entries)
    {
        var dates = entries.Select(e => e.EntryDate).Distinct().OrderBy(d => d).ToList();
        if (dates.Count == 0) return (0, 0, 0);

        // Missed days across the span
        int missed = 0;
        for (int i = 1; i < dates.Count; i++)
        {
            var gap = dates[i].DayNumber - dates[i - 1].DayNumber;
            if (gap > 1) missed += (gap - 1);
        }

        // Longest streak
        int longest = 1, run = 1;
        for (int i = 1; i < dates.Count; i++)
        {
            if (dates[i].DayNumber == dates[i - 1].DayNumber + 1) run++;
            else run = 1;

            if (run > longest) longest = run;
        }

        // Current streak ending today (or yesterday if no today entry is allowed to count)
        var today = DateOnly.FromDateTime(DateTime.Today);
        int current = 0;
        var set = new HashSet<int>(dates.Select(d => d.DayNumber));

        for (var d = today; set.Contains(d.DayNumber); d = d.AddDays(-1))
            current++;

        return (current, longest, missed);
    }
}
