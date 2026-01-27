using Journal.Data;
using Journal.Models;

namespace Journal.Services
{
    // Journal service - handles journal entry operations
    public class JournalService
    {
        private readonly JournalDatabase _database;
        private readonly AuthService _authService;

        public JournalService(JournalDatabase database, AuthService authService)
        {
            _database = database;
            _authService = authService;
        }

        // Get current user's ID
        private int GetCurrentUserId()
        {
            return _authService.CurrentUser?.UserId ?? 0;
        }

        // ========== JOURNAL ENTRY OPERATIONS ==========

        // Get all entries for current user
        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return new List<JournalEntry>();

            return await _database.GetAllEntriesAsync(userId);

        }

        // Get paged entries for current user
        public async Task<List<JournalEntry>> GetEntriesAsync(int skip, int take)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return new List<JournalEntry>();

            return await _database.GetEntriesAsync(userId, skip, take);
        }

        // Get entry for today
        public async Task<JournalEntry?> GetTodayEntryAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return null;

            var today = DateTime.Now.ToString("yyyy-MM-dd");
            return await _database.GetEntryByDateAsync(userId, today);
        }

        // Get entry for specific date
        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return null;

            var dateStr = date.ToString("yyyy-MM-dd");
            return await _database.GetEntryByDateAsync(userId, dateStr);
        }

        // Get entry by ID
        public async Task<JournalEntry?> GetEntryByIdAsync(int entryId)
        {
            return await _database.GetEntryByIdAsync(entryId);
        }

        // Create or update today's entry
        public async Task<(bool Success, string Message)> SaveTodayEntryAsync(string title, string content, int? categoryId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return (false, "User not authenticated");

                if (string.IsNullOrWhiteSpace(content))
                    return (false, "Content cannot be empty");

                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var existingEntry = await _database.GetEntryByDateAsync(userId, today);

                if (existingEntry != null)
                {
                    // Update existing entry
                    existingEntry.Title = title;
                    existingEntry.Content = content;
                    existingEntry.CategoryId = categoryId;

                    await _database.UpdateEntryAsync(existingEntry);
                    return (true, "Entry updated successfully!");
                }
                else
                {
                    // Create new entry
                    var newEntry = new JournalEntry
                    {
                        UserId = userId,
                        Title = title,
                        Content = content,
                        EntryDate = today,
                        CategoryId = categoryId
                    };

                    await _database.CreateEntryAsync(newEntry);
                    return (true, "Entry created successfully!");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Update specific entry
        public async Task<(bool Success, string Message)> UpdateEntryAsync(int entryId, string title, string content, int? categoryId = null)
        {
            try
            {
                var entry = await _database.GetEntryByIdAsync(entryId);
                if (entry == null)
                    return (false, "Entry not found");

                // Verify ownership
                if (entry.UserId != GetCurrentUserId())
                    return (false, "Unauthorized");

                if (string.IsNullOrWhiteSpace(content))
                    return (false, "Content cannot be empty");

                entry.Title = title;
                entry.Content = content;
                entry.CategoryId = categoryId;

                await _database.UpdateEntryAsync(entry);
                return (true, "Entry updated successfully!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Delete entry
        public async Task<(bool Success, string Message)> DeleteEntryAsync(int entryId)
        {
            try
            {
                var entry = await _database.GetEntryByIdAsync(entryId);
                if (entry == null)
                    return (false, "Entry not found");

                // Verify ownership
                if (entry.UserId != GetCurrentUserId())
                    return (false, "Unauthorized");

                await _database.DeleteEntryAsync(entryId);
                return (true, "Entry deleted successfully!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Get entries for a specific month (for calendar view)
        public async Task<List<JournalEntry>> GetEntriesByMonthAsync(int year, int month)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return new List<JournalEntry>();

            return await _database.GetEntriesByMonthAsync(userId, year, month);
        }

        // Get total entry count
        public async Task<int> GetEntryCountAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return 0;

            return await _database.GetEntryCountAsync(userId);
        }

        // Seed demo data for the current user when their journal is empty
        public async Task<(bool Success, string Message)> SeedSampleDataForCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return (false, "User not authenticated");

            var result = await _database.SeedSampleDataForUserAsync(userId);
            return (result.Seeded, result.Message);
        }

        // Search entries by keyword and filters
        public async Task<(List<JournalEntry> Entries, int TotalCount)> SearchEntriesAsync(
            string? searchText = null,
            List<int>? tagIds = null,
            List<int>? moodIds = null,
            int? categoryId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return (new List<JournalEntry>(), 0);

            var startStr = startDate?.ToString("yyyy-MM-dd");
            var endStr = endDate?.ToString("yyyy-MM-dd");
            var skip = (page - 1) * pageSize;

            var entries = await _database.SearchEntriesAsync(
                userId, searchText, tagIds, moodIds, categoryId, startStr, endStr, skip, pageSize);

            var count = await _database.CountEntriesAsync(
                userId, searchText, tagIds, moodIds, categoryId, startStr, endStr);

            return (entries, count);
        }

        // Get all categories
        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _database.GetAllCategoriesAsync();
        }
    }
}
