using Microsoft.EntityFrameworkCore;
using Journal.Data;
using Journal.Models;
using Journal.Services.Abstractions;

namespace Journal.Services;

public class JournalService : IJournalService
{
    private readonly JournalDbContext _db;

    public JournalService(JournalDbContext db) => _db = db;

    public Task<JournalEntry?> GetEntryAsync(DateOnly date)
        => _db.JournalEntries.AsNoTracking().FirstOrDefaultAsync(e => e.EntryDate == date);

    public Task<JournalEntry?> GetTodayAsync()
        => GetEntryAsync(DateOnly.FromDateTime(DateTime.Today));

    public async Task UpsertAsync(JournalEntry entry)
    {
        // System timestamps
        entry.UpdatedAtUtc = DateTime.UtcNow;

        var existing = await _db.JournalEntries.FirstOrDefaultAsync(e => e.EntryDate == entry.EntryDate);
        if (existing is null)
        {
            entry.CreatedAtUtc = DateTime.UtcNow;
            _db.JournalEntries.Add(entry);
        }
        else
        {
            existing.Title = entry.Title;
            existing.ContentMarkdown = entry.ContentMarkdown;
            existing.PrimaryMood = entry.PrimaryMood;
            existing.SecondaryMood1 = entry.SecondaryMood1;
            existing.SecondaryMood2 = entry.SecondaryMood2;
            existing.Category = entry.Category;
            existing.TagsCsv = entry.TagsCsv;
            existing.UpdatedAtUtc = entry.UpdatedAtUtc;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(DateOnly date)
    {
        var existing = await _db.JournalEntries.FirstOrDefaultAsync(e => e.EntryDate == date);
        if (existing is null) return;
        _db.JournalEntries.Remove(existing);
        await _db.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<JournalEntry>> GetMonthAsync(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        return await _db.JournalEntries.AsNoTracking()
            .Where(e => e.EntryDate >= start && e.EntryDate <= end)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<JournalEntry> Items, int TotalCount)> SearchAsync(
    string? query,
    DateOnly? from,
    DateOnly? to,
    Mood? mood,
    string? tag,
    int page,
    int pageSize)
    {
        var q = _db.JournalEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(e =>
                e.Title.Contains(query) ||
                e.ContentMarkdown.Contains(query));
        }

        if (from.HasValue)
            q = q.Where(e => e.EntryDate >= from.Value);

        if (to.HasValue)
            q = q.Where(e => e.EntryDate <= to.Value);

        if (mood.HasValue)
            q = q.Where(e =>
                e.PrimaryMood == mood ||
                e.SecondaryMood1 == mood ||
                e.SecondaryMood2 == mood);

        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(e => e.TagsCsv.Contains(tag));

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(e => e.EntryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetRangeAsync(DateOnly from, DateOnly to)
    {
        return await _db.JournalEntries.AsNoTracking()
            .Where(e => e.EntryDate >= from && e.EntryDate <= to)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }


}

