using Microsoft.AspNetCore.Components;
using Journal.Models;
using Journal.Services;
using Journal.Services.Abstractions;

namespace Journal.Components.Pages;

public partial class Dashboard 
{
    [Inject] public IJournalService JournalService { get; set; } = default!;

    protected int _currentStreak;
    protected int _longestStreak;
    protected int _missedDays;

    protected double[] _moodData = { 0, 0, 0 };
    protected string[] _moodLabels = { "Positive", "Neutral", "Negative" };

    protected List<JournalEntry> _recent = new();

    protected override async Task OnInitializedAsync()
    {
        var to = DateOnly.FromDateTime(DateTime.Today);
        var from = to.AddDays(-30);

        var result = await JournalService.SearchAsync(
            query: null,
            from: from,
            to: to,
            mood: null,
            tag: null,
            page: 1,
            pageSize: 1000);

        var entries = result.Items.ToList();

        _recent = entries
            .OrderByDescending(e => e.EntryDate)
            .Take(5)
            .ToList();

        var (current, longest, missed) =
            StreakService.CalculateStreaks(entries);

        _currentStreak = current;
        _longestStreak = longest;
        _missedDays = missed;

        int pos = entries.Count(e => IsPositive(e.PrimaryMood));
        int neu = entries.Count(e => IsNeutral(e.PrimaryMood));
        int neg = entries.Count(e => IsNegative(e.PrimaryMood));

        _moodData = new double[] { pos, neu, neg };
    }

    private static bool IsPositive(Mood m) =>
        m is Mood.Happy or Mood.Excited or Mood.Relaxed or Mood.Grateful or Mood.Confident;

    private static bool IsNeutral(Mood m) =>
        m is Mood.Calm or Mood.Thoughtful or Mood.Curious or Mood.Nostalgic or Mood.Bored;

    private static bool IsNegative(Mood m) =>
        m is Mood.Sad or Mood.Angry or Mood.Stressed or Mood.Lonely or Mood.Anxious;
}
