using Journal.Data;
using Journal.Models;

namespace Journal.Services
{
    public class MoodService
    {
        private readonly JournalDatabase _database;
        private readonly AuthService _authService;

        public MoodService(JournalDatabase database, AuthService authService)
        {
            _database = database;
            _authService = authService;
        }

        // Get all predefined moods
        public async Task<(bool Success, List<Mood>? Moods, string Message)> GetAllMoodsAsync()
        {
            try
            {
                var moods = await _database.GetAllMoodsAsync();
                return (true, moods, "Moods retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving moods: {ex.Message}");
            }
        }

        // Get a specific mood by ID
        public async Task<(bool Success, Mood? Mood, string Message)> GetMoodByIdAsync(int moodId)
        {
            try
            {
                var mood = await _database.GetMoodByIdAsync(moodId);

                if (mood == null)
                    return (false, null, "Mood not found");

                return (true, mood, "Mood retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving mood: {ex.Message}");
            }
        }

        // Add mood to an entry
        public async Task<(bool Success, string Message)> AddMoodToEntryAsync(int entryId, int moodId, int? intensity = null, bool isPrimary = false)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Verify entry belongs to user
                var entry = await _database.GetEntryByIdAsync(entryId);
                if (entry == null)
                    return (false, "Entry not found");

                if (entry.UserId != currentUser.UserId)
                    return (false, "Unauthorized access to entry");

                // Verify mood exists
                var mood = await _database.GetMoodByIdAsync(moodId);
                if (mood == null)
                    return (false, "Mood not found");

                // Validate intensity if provided
                if (intensity.HasValue && (intensity.Value < 1 || intensity.Value > 10))
                    return (false, "Intensity must be between 1 and 10");

                await _database.AddEntryMoodAsync(entryId, moodId, intensity, isPrimary);
                return (true, "Mood added to entry");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding mood to entry: {ex.Message}");
            }
        }

        // Remove mood from an entry
        public async Task<(bool Success, string Message)> RemoveMoodFromEntryAsync(int entryId, int moodId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                await _database.RemoveEntryMoodAsync(entryId, moodId);
                return (true, "Mood removed from entry");
            }
            catch (Exception ex)
            {
                return (false, $"Error removing mood from entry: {ex.Message}");
            }
        }

        // Get all moods for a specific entry with their intensities
        public async Task<(bool Success, List<EntryMood>? EntryMoods, string Message)> GetEntryMoodsAsync(int entryId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var entryMoods = await _database.GetMoodsByEntryAsync(entryId);
                return (true, entryMoods, "Entry moods retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving entry moods: {ex.Message}");
            }
        }

        // Get all entries with a specific mood
        public async Task<(bool Success, List<JournalEntry>? Entries, string Message)> GetEntriesByMoodAsync(int moodId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var entries = await _database.GetEntriesByMoodAsync(currentUser.UserId, moodId);
                return (true, entries, "Entries retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving entries by mood: {ex.Message}");
            }
        }

        // Get mood statistics for a user within a date range
        public async Task<(bool Success, Dictionary<string, int>? Statistics, string Message)> GetMoodStatisticsAsync(string startDate, string endDate)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var stats = await _database.GetMoodStatisticsAsync(currentUser.UserId, startDate, endDate);
                return (true, stats, "Mood statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving mood statistics: {ex.Message}");
            }
        }

        // Get mood trends (most frequent moods) for a user
        public async Task<(bool Success, List<(string MoodName, int Count)>? Trends, string Message)> GetMoodTrendsAsync(int daysBack = 30)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var startDate = DateTime.Now.AddDays(-daysBack).ToString("yyyy-MM-dd");
                var endDate = DateTime.Now.ToString("yyyy-MM-dd");

                var trends = await _database.GetMoodTrendsAsync(currentUser.UserId, startDate, endDate);
                return (true, trends, "Mood trends retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving mood trends: {ex.Message}");
            }
        }

        // Get dominant mood for an entry (most intense or first recorded)
        public async Task<(bool Success, Mood? DominantMood, string Message)> GetDominantMoodForEntryAsync(int entryId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var entryMoods = await _database.GetMoodsByEntryAsync(entryId);

                if (entryMoods == null || !entryMoods.Any())
                    return (false, null, "No moods found for this entry");

                // Get the mood with highest intensity
                var dominantEntryMood = entryMoods.OrderByDescending(em => em.Intensity ?? 0).First();
                var mood = await _database.GetMoodByIdAsync(dominantEntryMood.MoodId);

                return (true, mood, "Dominant mood retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving dominant mood: {ex.Message}");
            }
        }
    }
}
