using Journal.Data;
using Journal.Models;

namespace Journal.Services
{
    public class StreakService
    {
        private readonly JournalDatabase _database;
        private readonly AuthService _authService;

        public StreakService(JournalDatabase database, AuthService authService)
        {
            _database = database;
            _authService = authService;
        }

        // Calculate and return current active streak
        public async Task<(bool Success, JournalStreak? Streak, string Message)> GetCurrentStreakAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var streak = await _database.GetActiveStreakAsync(currentUser.UserId);

                if (streak == null)
                {
                    // No active streak, calculate from entries
                    await CalculateAndSaveStreakAsync();
                    streak = await _database.GetActiveStreakAsync(currentUser.UserId);
                }

                return (true, streak, "Current streak retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving current streak: {ex.Message}");
            }
        }

        // Get longest streak ever achieved
        public async Task<(bool Success, JournalStreak? Streak, string Message)> GetLongestStreakAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var streaks = await _database.GetAllStreaksAsync(currentUser.UserId);
                var longestStreak = streaks.OrderByDescending(s => s.DayCount).FirstOrDefault();

                return (true, longestStreak, "Longest streak retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving longest streak: {ex.Message}");
            }
        }

        // Get all streaks (active and inactive)
        public async Task<(bool Success, List<JournalStreak>? Streaks, string Message)> GetAllStreaksAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var streaks = await _database.GetAllStreaksAsync(currentUser.UserId);
                return (true, streaks, "All streaks retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving streaks: {ex.Message}");
            }
        }

        // Update streak after a new entry is created
        public async Task<(bool Success, string Message)> UpdateStreakAfterEntryAsync(string entryDate)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                await CalculateAndSaveStreakAsync();
                return (true, "Streak updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating streak: {ex.Message}");
            }
        }

        // Calculate streak from all entries and save to database
        private async Task CalculateAndSaveStreakAsync()
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser == null)
                return;

            var allEntries = await _database.GetAllEntriesAsync(currentUser.UserId);

            if (!allEntries.Any())
                return;

            // Get distinct dates sorted descending
            var entryDates = allEntries
                .Select(e => DateTime.Parse(e.EntryDate))
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            var today = DateTime.Now.Date;
            var currentStreakCount = 0;
            var currentStreakStart = today;
            var streaks = new List<(DateTime Start, DateTime End, int Count)>();

            // Calculate current streak (must include today or yesterday)
            if (entryDates.Any())
            {
                var mostRecentEntry = entryDates.First();
                var daysSinceLastEntry = (today - mostRecentEntry).Days;

                if (daysSinceLastEntry <= 1) // Today or yesterday
                {
                    var checkDate = mostRecentEntry;
                    currentStreakStart = checkDate;
                    currentStreakCount = 1;

                    for (int i = 1; i < entryDates.Count; i++)
                    {
                        var previousDate = entryDates[i];
                        var daysDiff = (checkDate - previousDate).Days;

                        if (daysDiff == 1)
                        {
                            currentStreakCount++;
                            currentStreakStart = previousDate;
                            checkDate = previousDate;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // Deactivate old active streaks
            var oldActiveStreak = await _database.GetActiveStreakAsync(currentUser.UserId);
            if (oldActiveStreak != null)
            {
                oldActiveStreak.IsActive = false;
                oldActiveStreak.UpdatedAt = DateTime.Now;
                await _database.UpdateStreakAsync(oldActiveStreak);
            }

            // Save current streak if exists
            if (currentStreakCount > 0)
            {
                var newStreak = new JournalStreak
                {
                    UserId = currentUser.UserId,
                    StartDate = currentStreakStart.ToString("yyyy-MM-dd"),
                    EndDate = null,
                    DayCount = currentStreakCount,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _database.CreateStreakAsync(newStreak);
            }
        }

        // Manually recalculate all streaks (useful for data migration or fixes)
        public async Task<(bool Success, string Message)> RecalculateAllStreaksAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Deactivate all existing streaks
                var allStreaks = await _database.GetAllStreaksAsync(currentUser.UserId);
                foreach (var streak in allStreaks)
                {
                    await _database.DeleteStreakAsync(streak.StreakId);
                }

                // Recalculate from entries
                await CalculateAndSaveStreakAsync();

                return (true, "All streaks recalculated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error recalculating streaks: {ex.Message}");
            }
        }

        // Check if user has an active streak
        public async Task<bool> HasActiveStreakAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return false;

                var streak = await _database.GetActiveStreakAsync(currentUser.UserId);
                return streak != null && streak.DayCount > 0;
            }
            catch
            {
                return false;
            }
        }

        // Get streak statistics
        public async Task<(bool Success, StreakStats? Stats, string Message)> GetStreakStatisticsAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var allStreaks = await _database.GetAllStreaksAsync(currentUser.UserId);
                var currentStreak = await _database.GetActiveStreakAsync(currentUser.UserId);

                var stats = new StreakStats
                {
                    CurrentStreak = currentStreak?.DayCount ?? 0,
                    LongestStreak = allStreaks.Any() ? allStreaks.Max(s => s.DayCount) : 0,
                    TotalStreaks = allStreaks.Count,
                    AverageStreakLength = allStreaks.Any() ? (int)allStreaks.Average(s => s.DayCount) : 0,
                    IsActive = currentStreak?.IsActive ?? false
                };

                return (true, stats, "Streak statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving streak statistics: {ex.Message}");
            }
        }
    }

    // Helper class for streak statistics
    public class StreakStats
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalStreaks { get; set; }
        public int AverageStreakLength { get; set; }
        public bool IsActive { get; set; }
    }
}
