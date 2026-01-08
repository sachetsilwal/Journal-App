using Journal.Models;

namespace Journal.Services.Abstractions;

public interface IJournalService
{
    Task<JournalEntry?> GetEntryAsync(DateOnly date);
    Task<JournalEntry?> GetTodayAsync();
    Task UpsertAsync(JournalEntry entry);      // Create or Update (one per date)
    Task DeleteAsync(DateOnly date);

    Task<(IReadOnlyList<JournalEntry> Items, int TotalCount)> SearchAsync(
    string? query,
    DateOnly? from,
    DateOnly? to,
    Mood? mood,
    string? tag,
    int page,
    int pageSize);

    Task<IReadOnlyList<JournalEntry>> GetMonthAsync(int year, int month);

    Task<IReadOnlyList<JournalEntry>> GetRangeAsync(DateOnly from, DateOnly to);



}
