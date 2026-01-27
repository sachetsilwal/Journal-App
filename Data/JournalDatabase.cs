using SQLite;
using Journal.Models;

namespace Journal.Data
{
    // Database service - handles all database operations
    public class JournalDatabase
    {
        private SQLiteAsyncConnection? _database;
        private bool _isSchemaVerified = false;

        // Initialize database connection
        public async Task InitAsync()
        {
            if (_database != null)
            {
                if (!_isSchemaVerified)
                {
                    await CheckAndRepairSchemaAsync();
                }
                return;
            }

            // Get the path to store the database
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "journals.db3");

            // Create connection to SQLite database
            _database = new SQLiteAsyncConnection(dbPath);

            // Create all required tables if they don't exist
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Category>();
            await _database.CreateTableAsync<JournalEntry>();
            await _database.CreateTableAsync<Tag>();
            await _database.CreateTableAsync<JournalEntryTag>();
            await _database.CreateTableAsync<Mood>();
            await _database.CreateTableAsync<EntryMood>();
            await _database.CreateTableAsync<JournalStreak>();
            await _database.CreateTableAsync<UserSetting>();

            await CheckAndRepairSchemaAsync();
        }

        private async Task CheckAndRepairSchemaAsync()
        {
            if (_database == null) return;

            try
            {
                _isSchemaVerified = true;

                // 1. Fix EntryTags if it references Tags_Old
                var schema = await _database.ExecuteScalarAsync<string>(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='EntryTags'");
                
                if (!string.IsNullOrEmpty(schema) && schema.Contains("Tags_Old"))
                {
                    // Rename bad table
                    await _database.ExecuteAsync("ALTER TABLE EntryTags RENAME TO EntryTags_Bad");
                    
                    // Recreate correct table
                    await _database.CreateTableAsync<JournalEntryTag>();
                    
                    // Copy data
                    await _database.ExecuteAsync(
                         "INSERT INTO EntryTags (EntryId, TagId, CreatedAt) " +
                         "SELECT EntryId, TagId, CreatedAt FROM EntryTags_Bad");
                         
                    // Drop bad table
                    await _database.ExecuteAsync("DROP TABLE EntryTags_Bad");
                }
                
                // 2. Fix EntryMoods if it has mismatched columns (missing Intensity/CreatedAt)
                var moodSchema = await _database.ExecuteScalarAsync<string>(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='EntryMoods'");
                
                if (!string.IsNullOrEmpty(moodSchema) && !moodSchema.Contains("Intensity"))
                {
                    // Rename bad table
                    await _database.ExecuteAsync("ALTER TABLE EntryMoods RENAME TO EntryMoods_Bad");
                    
                    // Recreate correct table
                    await _database.CreateTableAsync<EntryMood>();
                    
                    // Copy data (Map EntryMoodId -> Id, Default Intensity=5, CreatedAt=Now)
                    // Note: SQLite copying requires matching types.
                    var nowTicks = DateTime.Now.Ticks;
                    await _database.ExecuteAsync(
                         "INSERT INTO EntryMoods (Id, EntryId, MoodId, Intensity, IsPrimary, CreatedAt) " +
                         $"SELECT EntryMoodId, EntryId, MoodId, 5, IsPrimary, {nowTicks} FROM EntryMoods_Bad");
                         
                    // Drop bad table
                    await _database.ExecuteAsync("DROP TABLE EntryMoods_Bad");
                }

                // 3. Robust UserSettings Fix: Ensure UserId is NOT unique, allowing multiple settings per user.
                var settingsSchema = await _database.ExecuteScalarAsync<string>(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='UserSettings'");
                
                if (!string.IsNullOrEmpty(settingsSchema))
                {
                    // If the schema shows UserId with UNIQUE or as part of PRIMARY KEY (besides SettingId), recreate.
                    // We check for "UNIQUE" or if it lacks the standard SettingId primary key start.
                    bool isBroken = settingsSchema.Contains("UNIQUE") || 
                                    (settingsSchema.Contains("\"UserId\"") && settingsSchema.Contains("PRIMARY KEY") && !settingsSchema.Contains("\"SettingId\" INTEGER PRIMARY KEY"));

                    if (isBroken)
                    {
                        await _database.ExecuteAsync("DROP TABLE IF EXISTS UserSettings");
                        await _database.CreateTableAsync<UserSetting>();
                    }
                }

                // 4. Cleanup phantom tables
                await _database.ExecuteAsync("DROP TABLE IF EXISTS EntryTags_Bad");
                await _database.ExecuteAsync("DROP TABLE IF EXISTS Tags_Old");
                await _database.ExecuteAsync("DROP TABLE IF EXISTS \"User\""); // Drop singular User table
                await _database.ExecuteAsync("DROP TABLE IF EXISTS \"JournalEntryTags\""); // Drop unused table
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Schema repair failed: {ex.Message}");
            }

            await SeedReferenceDataAsync();
        }

        // Get user by username (email)
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            await InitAsync();

            // Query the database for a user with matching username
            var user = await _database!.Table<User>()
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();

            return user;
        }

        // Create a new user account
        public async Task<int> CreateUserAsync(User user)
        {
            await InitAsync();

            // Insert the user into the database
            return await _database!.InsertAsync(user);
        }

        // Update user's last login time
        public async Task UpdateLastLoginAsync(int userId)
        {
            await InitAsync();

            // Get the user
            var user = await _database!.Table<User>()
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                // Update last login time
                user.LastLogin = DateTime.Now;
                await _database!.UpdateAsync(user);
            }
        }

        // Check if username already exists
        public async Task<bool> UsernameExistsAsync(string username)
        {
            await InitAsync();

            // Count users with this username
            var count = await _database!.Table<User>()
                .Where(u => u.Username == username)
                .CountAsync();

            return count > 0;
        }

        // ========== JOURNAL ENTRY OPERATIONS ==========

        // Get all journal entries for a user
        public async Task<List<JournalEntry>> GetAllEntriesAsync(int userId)
        {
            await InitAsync();

            // Get all entries for this user, ordered by date (newest first)
            var entries = await _database!.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            return entries;
        }

        // Get paged journal entries for a user
        public async Task<List<JournalEntry>> GetEntriesAsync(int userId, int skip, int take)
        {
            await InitAsync();

            // Get paged entries for this user, ordered by date (newest first)
            var entries = await _database!.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EntryDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return entries;
        }

        // Get journal entry for a specific date
        public async Task<JournalEntry?> GetEntryByDateAsync(int userId, string date)
        {
            await InitAsync();

            // Get entry for this user and date
            var entry = await _database!.Table<JournalEntry>()
                .Where(e => e.UserId == userId && e.EntryDate == date)
                .FirstOrDefaultAsync();

            return entry;
        }

        public async Task<JournalEntry?> GetEntryByIdAsync(int entryId)
        {
            await InitAsync();

            var entry = await _database!.Table<JournalEntry>()
                .Where(e => e.EntryId == entryId)
                .FirstOrDefaultAsync();

            return entry;
        }

        public async Task<int> CreateEntryAsync(JournalEntry entry)
        {
            await InitAsync();

            // Set timestamps
            entry.CreatedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;

            // Calculate word count
            entry.WordCount = CountWords(entry.Content);

            // Insert the entry
            return await _database!.InsertAsync(entry);
        }

        public async Task<int> UpdateEntryAsync(JournalEntry entry)
        {
            await InitAsync();

            // Update timestamp
            entry.UpdatedAt = DateTime.Now;

            // Recalculate word count
            entry.WordCount = CountWords(entry.Content);

            // Update the entry
            return await _database!.UpdateAsync(entry);
        }

        public async Task<int> DeleteEntryAsync(int entryId)
        {
            await InitAsync();

            try
            {
                return await _database!.DeleteAsync<JournalEntry>(entryId);
            }
            catch (Exception ex) when (ex.Message.Contains("no such table"))
            {
                // If deletion fails due to schema issues, force a re-check and try again
                _isSchemaVerified = false; // Force check
                await InitAsync();
                return await _database!.DeleteAsync<JournalEntry>(entryId);
            }
        }

        public async Task<List<JournalEntry>> GetEntriesByMonthAsync(int userId, int year, int month)
        {
            await InitAsync();

            // Format: YYYY-MM
            var monthPrefix = $"{year:0000}-{month:00}";

            // Get all entries for this month
            var entries = await _database!.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .ToListAsync();

            // Filter by month (SQLite doesn't support LIKE in WHERE clause easily)
            return entries.Where(e => e.EntryDate.StartsWith(monthPrefix)).ToList();
        }

        public async Task<int> GetEntryCountAsync(int userId)
        {
            await InitAsync();

            return await _database!.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .CountAsync();
        }


        // Get all categories
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            await InitAsync();

            return await _database!.Table<Category>().ToListAsync();
        }

        // Get all tags for a user
        public async Task<List<Tag>> GetTagsByUserAsync(int userId)
        {
            await InitAsync();

            return await _database!.Table<Tag>()
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        // Get a tag by ID
        public async Task<Tag?> GetTagByIdAsync(int tagId)
        {
            await InitAsync();

            return await _database!.Table<Tag>()
                .Where(t => t.TagId == tagId)
                .FirstOrDefaultAsync();
        }

        // Create a new tag
        public async Task<int> CreateTagAsync(Tag tag)
        {
            await InitAsync();

            return await _database!.InsertAsync(tag);
        }

        // Update a tag
        public async Task<int> UpdateTagAsync(Tag tag)
        {
            await InitAsync();

            return await _database!.UpdateAsync(tag);
        }

        // Delete a tag (also deletes all entry-tag associations)
        public async Task<int> DeleteTagAsync(int tagId)
        {
            await InitAsync();

            // Delete all entry-tag associations first
            await _database!.ExecuteAsync("DELETE FROM EntryTags WHERE TagId = ?", tagId);

            // Delete the tag
            return await _database!.DeleteAsync<Tag>(tagId);
        }

        // Check if tag name exists for a user
        public async Task<bool> TagExistsAsync(int userId, string tagName)
        {
            await InitAsync();

            var count = await _database!.Table<Tag>()
                .Where(t => t.UserId == userId && t.Name == tagName)
                .CountAsync();

            return count > 0;
        }

        // Get all tags for a specific entry
        public async Task<List<Tag>> GetTagsByEntryAsync(int entryId)
        {
            await InitAsync();

            var query = @"
                SELECT t.* 
                FROM Tags t
                INNER JOIN EntryTags jet ON t.TagId = jet.TagId
                WHERE jet.EntryId = ?
                ORDER BY t.Name";

            return await _database!.QueryAsync<Tag>(query, entryId);
        }

        // Add a tag to an entry
        public async Task<int> AddTagToEntryAsync(int entryId, int tagId)
        {
            await InitAsync();

            // Check if association already exists
            var existing = await _database!.Table<JournalEntryTag>()
                .Where(jet => jet.EntryId == entryId && jet.TagId == tagId)
                .FirstOrDefaultAsync();

            if (existing != null)
                return 0; // Already exists

            var entryTag = new JournalEntryTag
            {
                EntryId = entryId,
                TagId = tagId,
                CreatedAt = DateTime.Now
            };

            return await _database!.InsertAsync(entryTag);
        }

        // Remove a tag from an entry
        public async Task<int> RemoveTagFromEntryAsync(int entryId, int tagId)
        {
            await InitAsync();

            return await _database!.ExecuteAsync(
                "DELETE FROM EntryTags WHERE EntryId = ? AND TagId = ?",
                entryId, tagId);
        }

        // Get all entries with a specific tag
        public async Task<List<JournalEntry>> GetEntriesByTagAsync(int userId, int tagId)
        {
            await InitAsync();

            var query = @"
                SELECT e.* 
                FROM JournalEntries e
                INNER JOIN EntryTags jet ON e.EntryId = jet.EntryId
                WHERE e.UserId = ? AND jet.TagId = ?
                ORDER BY e.EntryDate DESC";

            return await _database!.QueryAsync<JournalEntry>(query, userId, tagId);
        }

        // ========== MOOD OPERATIONS ==========

        // Get all predefined moods
        public async Task<List<Mood>> GetAllMoodsAsync()
        {
            await InitAsync();

            return await _database!.Table<Mood>()
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        // Get a mood by ID
        public async Task<Mood?> GetMoodByIdAsync(int moodId)
        {
            await InitAsync();

            return await _database!.Table<Mood>()
                .Where(m => m.MoodId == moodId)
                .FirstOrDefaultAsync();
        }

        // Get all moods for a specific entry (with intensity info)
        public async Task<List<EntryMood>> GetMoodsByEntryAsync(int entryId)
        {
            await InitAsync();

            return await _database!.Table<EntryMood>()
                .Where(em => em.EntryId == entryId)
                .ToListAsync();
        }

        // Add a mood to an entry
        public async Task<int> AddEntryMoodAsync(int entryId, int moodId, int? intensity = null, bool isPrimary = false)
        {
            await InitAsync();

            // Check if association already exists
            var existing = await _database!.Table<EntryMood>()
                .Where(em => em.EntryId == entryId && em.MoodId == moodId)
                .FirstOrDefaultAsync();

            if (existing != null)
                return 0; // Already exists

            var entryMood = new EntryMood
            {
                EntryId = entryId,
                MoodId = moodId,
                Intensity = intensity,
                IsPrimary = isPrimary,
                CreatedAt = DateTime.Now
            };

            return await _database!.InsertAsync(entryMood);
        }

        // Remove a mood from an entry
        public async Task<int> RemoveEntryMoodAsync(int entryId, int moodId)
        {
            await InitAsync();

            return await _database!.ExecuteAsync(
                "DELETE FROM EntryMoods WHERE EntryId = ? AND MoodId = ?",
                entryId, moodId);
        }

        // Get all entries with a specific mood
        public async Task<List<JournalEntry>> GetEntriesByMoodAsync(int userId, int moodId)
        {
            await InitAsync();

            var query = @"
                SELECT e.* 
                FROM JournalEntries e
                INNER JOIN EntryMoods em ON e.EntryId = em.EntryId
                WHERE e.UserId = ? AND em.MoodId = ?
                ORDER BY e.EntryDate DESC";

            return await _database!.QueryAsync<JournalEntry>(query, userId, moodId);
        }

        // Get mood statistics for a user within a date range
        public async Task<Dictionary<string, int>> GetMoodStatisticsAsync(int userId, string startDate, string endDate)
        {
            await InitAsync();

            var query = @"
                SELECT m.Name, COUNT(*) as Count
                FROM Moods m
                INNER JOIN EntryMoods em ON m.MoodId = em.MoodId
                INNER JOIN JournalEntries e ON em.EntryId = e.EntryId
                WHERE e.UserId = ? AND e.EntryDate >= ? AND e.EntryDate <= ?
                GROUP BY m.Name
                ORDER BY Count DESC";

            var results = await _database!.QueryAsync<MoodStatResult>(query, userId, startDate, endDate);

            return results.ToDictionary(r => r.Name, r => r.Count);
        }

        // Get mood trends (most frequent moods with counts)
        public async Task<List<(string MoodName, int Count)>> GetMoodTrendsAsync(int userId, string startDate, string endDate)
        {
            await InitAsync();

            var query = @"
                SELECT m.Name, COUNT(*) as Count
                FROM Moods m
                INNER JOIN EntryMoods em ON m.MoodId = em.MoodId
                INNER JOIN JournalEntries e ON em.EntryId = e.EntryId
                WHERE e.UserId = ? AND e.EntryDate >= ? AND e.EntryDate <= ?
                GROUP BY m.Name
                ORDER BY Count DESC
                LIMIT 5";

            var results = await _database!.QueryAsync<MoodStatResult>(query, userId, startDate, endDate);

            return results.Select(r => (r.Name, r.Count)).ToList();
        }

        // Helper class for mood statistics queries
        private class MoodStatResult
        {
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        // ========== STREAK OPERATIONS ==========

        // Get active streak for a user
        public async Task<JournalStreak?> GetActiveStreakAsync(int userId)
        {
            await InitAsync();

            return await _database!.Table<JournalStreak>()
                .Where(s => s.UserId == userId && s.IsActive == true)
                .FirstOrDefaultAsync();
        }

        // Get all streaks for a user (active and inactive)
        public async Task<List<JournalStreak>> GetAllStreaksAsync(int userId)
        {
            await InitAsync();

            return await _database!.Table<JournalStreak>()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        // Create a new streak
        public async Task<int> CreateStreakAsync(JournalStreak streak)
        {
            await InitAsync();

            return await _database!.InsertAsync(streak);
        }

        // Update an existing streak
        public async Task<int> UpdateStreakAsync(JournalStreak streak)
        {
            await InitAsync();

            return await _database!.UpdateAsync(streak);
        }

        // Delete a streak
        public async Task<int> DeleteStreakAsync(int streakId)
        {
            await InitAsync();

            return await _database!.DeleteAsync<JournalStreak>(streakId);
        }

        // ========== USER SETTINGS OPERATIONS ==========

        // Get a specific setting by key for a user
        public async Task<UserSetting?> GetSettingByKeyAsync(int userId, string settingKey)
        {
            await InitAsync();

            return await _database!.Table<UserSetting>()
                .Where(s => s.UserId == userId && s.SettingKey == settingKey)
                .FirstOrDefaultAsync();
        }

        // Get all settings for a user
        public async Task<List<UserSetting>> GetAllUserSettingsAsync(int userId)
        {
            await InitAsync();

            return await _database!.Table<UserSetting>()
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.SettingKey)
                .ToListAsync();
        }

        // Create a new setting
        public async Task<int> CreateSettingAsync(UserSetting setting)
        {
            await InitAsync();

            return await _database!.InsertAsync(setting);
        }

        // Update an existing setting
        public async Task<int> UpdateSettingAsync(UserSetting setting)
        {
            await InitAsync();

            return await _database!.UpdateAsync(setting);
        }

        // Delete a setting
        public async Task<int> DeleteSettingAsync(int userId, string settingKey)
        {
            await InitAsync();

            return await _database!.ExecuteAsync(
                "DELETE FROM UserSettings WHERE UserId = ? AND SettingKey = ?",
                userId, settingKey);
        }

        // ========== SEARCH OPERATIONS ==========

        // Advanced search for journal entries
        public async Task<List<JournalEntry>> SearchEntriesAsync(
            int userId,
            string? searchText = null,
            List<int>? tagIds = null,
            List<int>? moodIds = null,
            int? categoryId = null,
            string? startDate = null,
            string? endDate = null,
            int? skip = null,
            int? take = null)
        {
            await InitAsync();

            var query = "SELECT DISTINCT e.* FROM JournalEntries e WHERE e.UserId = ?";
            var parameters = new List<object> { userId };

            // Full-text search in title and content
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query += " AND (e.Title LIKE ? OR e.Content LIKE ?)";
                var searchPattern = $"%{searchText}%";
                parameters.Add(searchPattern);
                parameters.Add(searchPattern);
            }

            // Filter by tags
            if (tagIds != null && tagIds.Any())
            {
                var tagPlaceholders = string.Join(",", tagIds.Select(_ => "?"));
                query += $" AND e.EntryId IN (SELECT EntryId FROM EntryTags WHERE TagId IN ({tagPlaceholders}))";
                parameters.AddRange(tagIds.Cast<object>());
            }

            // Filter by moods
            if (moodIds != null && moodIds.Any())
            {
                var moodPlaceholders = string.Join(",", moodIds.Select(_ => "?"));
                query += $" AND e.EntryId IN (SELECT EntryId FROM EntryMoods WHERE MoodId IN ({moodPlaceholders}))";
                parameters.AddRange(moodIds.Cast<object>());
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query += " AND e.CategoryId = ?";
                parameters.Add(categoryId.Value);
            }

            // Filter by date range
            if (!string.IsNullOrWhiteSpace(startDate))
            {
                query += " AND e.EntryDate >= ?";
                parameters.Add(startDate);
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                query += " AND e.EntryDate <= ?";
                parameters.Add(endDate);
            }

            query += " ORDER BY e.EntryDate DESC";

            // Note: Count doesn't use LIMIT/OFFSET


            return await _database!.QueryAsync<JournalEntry>(query, parameters.ToArray());
        }

        public async Task<int> CountEntriesAsync(
            int userId,
            string? searchText = null,
            List<int>? tagIds = null,
            List<int>? moodIds = null,
            int? categoryId = null,
            string? startDate = null,
            string? endDate = null)
        {
            await InitAsync();

            var query = "SELECT COUNT(DISTINCT e.EntryId) FROM JournalEntries e WHERE e.UserId = ?";
            var parameters = new List<object> { userId };

            // Full-text search in title and content
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query += " AND (e.Title LIKE ? OR e.Content LIKE ?)";
                var searchPattern = $"%{searchText}%";
                parameters.Add(searchPattern);
                parameters.Add(searchPattern);
            }

            // Filter by tags
            if (tagIds != null && tagIds.Any())
            {
                var tagPlaceholders = string.Join(",", tagIds.Select(_ => "?"));
                query += $" AND e.EntryId IN (SELECT EntryId FROM EntryTags WHERE TagId IN ({tagPlaceholders}))";
                parameters.AddRange(tagIds.Cast<object>());
            }

            // Filter by moods
            if (moodIds != null && moodIds.Any())
            {
                var moodPlaceholders = string.Join(",", moodIds.Select(_ => "?"));
                query += $" AND e.EntryId IN (SELECT EntryId FROM EntryMoods WHERE MoodId IN ({moodPlaceholders}))";
                parameters.AddRange(moodIds.Cast<object>());
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query += " AND e.CategoryId = ?";
                parameters.Add(categoryId.Value);
            }

            // Filter by date range
            if (!string.IsNullOrWhiteSpace(startDate))
            {
                query += " AND e.EntryDate >= ?";
                parameters.Add(startDate);
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                query += " AND e.EntryDate <= ?";
                parameters.Add(endDate);
            }

            return await _database!.ExecuteScalarAsync<int>(query, parameters.ToArray());
        }

        // ========== ANALYTICS OPERATIONS ==========

        // Get word count statistics for a user within a date range
        public async Task<WordCountStats> GetWordCountStatisticsAsync(int userId, string? startDate = null, string? endDate = null)
        {
            await InitAsync();

            var query = "SELECT * FROM JournalEntries WHERE UserId = ?";
            var parameters = new List<object> { userId };

            if (!string.IsNullOrWhiteSpace(startDate))
            {
                query += " AND EntryDate >= ?";
                parameters.Add(startDate);
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                query += " AND EntryDate <= ?";
                parameters.Add(endDate);
            }

            var entries = await _database!.QueryAsync<JournalEntry>(query, parameters.ToArray());

            var stats = new WordCountStats
            {
                TotalWords = entries.Sum(e => e.WordCount),
                AverageWords = entries.Any() ? (int)entries.Average(e => e.WordCount) : 0,
                MinWords = entries.Any() ? entries.Min(e => e.WordCount) : 0,
                MaxWords = entries.Any() ? entries.Max(e => e.WordCount) : 0,
                TotalEntries = entries.Count
            };

            return stats;
        }

        // Get entries count grouped by month for a specific year
        public async Task<List<MonthlyEntryCount>> GetEntriesCountByMonthAsync(int userId, int year)
        {
            await InitAsync();

            var query = @"
                SELECT 
                    CAST(strftime('%m', EntryDate) AS INTEGER) as Month,
                    COUNT(*) as Count
                FROM JournalEntries
                WHERE UserId = ? AND strftime('%Y', EntryDate) = ?
                GROUP BY strftime('%m', EntryDate)
                ORDER BY Month";

            return await _database!.QueryAsync<MonthlyEntryCount>(query, userId, year.ToString());
        }

        // Get entries count grouped by category
        public async Task<List<CategoryEntryCount>> GetEntriesCountByCategoryAsync(int userId)
        {
            await InitAsync();

            var query = @"
                SELECT 
                    COALESCE(c.Name, 'Uncategorized') as CategoryName,
                    COUNT(e.EntryId) as Count
                FROM JournalEntries e
                LEFT JOIN Categories c ON e.CategoryId = c.CategoryId
                WHERE e.UserId = ?
                GROUP BY c.Name
                ORDER BY Count DESC";

            return await _database!.QueryAsync<CategoryEntryCount>(query, userId);
        }

        // Get tag usage statistics (tag name with usage count)
        public async Task<List<TagUsageStats>> GetTagUsageStatisticsAsync(int userId)
        {
            await InitAsync();

            var query = @"
                SELECT 
                    t.TagId,
                    t.Name,
                    t.Color,
                    COUNT(jet.EntryId) as UsageCount
                FROM Tags t
                LEFT JOIN EntryTags jet ON t.TagId = jet.TagId
                WHERE t.UserId = ?
                GROUP BY t.TagId, t.Name, t.Color
                ORDER BY UsageCount DESC, t.Name";

            return await _database!.QueryAsync<TagUsageStats>(query, userId);
        }

        // Helper classes for analytics queries
        public class WordCountStats
        {
            public int TotalWords { get; set; }
            public int AverageWords { get; set; }
            public int MinWords { get; set; }
            public int MaxWords { get; set; }
            public int TotalEntries { get; set; }
        }

        public class MonthlyEntryCount
        {
            public int Month { get; set; }
            public int Count { get; set; }
        }

        public class CategoryEntryCount
        {
            public string CategoryName { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        public class TagUsageStats
        {
            public int TagId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Color { get; set; }
            public int UsageCount { get; set; }
        }

        // ========== HELPER METHODS ==========

        // Seed database with test data
        public async Task SeedTestDataAsync()
        {
            await InitAsync();

            // Check if test data already exists
            var existingUsers = await _database!.Table<User>().CountAsync();
            if (existingUsers > 0)
                return; // Already seeded

            // Create test users with hashed passwords
            var testUsers = new List<User>
            {
                new User
                {
                    Username = "john@email.com",
                    PasswordHash = HashPassword("Password123"),
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    Username = "sarah@email.com",
                    PasswordHash = HashPassword("Welcome456"),
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    Username = "mike@email.com",
                    PasswordHash = HashPassword("Journal789"),
                    CreatedAt = DateTime.Now
                }
            };

            foreach (var user in testUsers)
            {
                await _database.InsertAsync(user);
            }

            // Create test categories
            var testCategories = new List<Category>
            {
                new Category { Name = "Personal", Description = "Personal thoughts and reflections" },
                new Category { Name = "Work", Description = "Work-related entries" },
                new Category { Name = "Health", Description = "Health and wellness" },
                new Category { Name = "Travel", Description = "Travel experiences" }
            };

            foreach (var category in testCategories)
            {
                await _database.InsertAsync(category);
            }

            // Create predefined moods
            var testMoods = new List<Mood>
            {
                new Mood { Name = "Happy", Icon = "😊", Color = "#FFD700", Intensity = 9 },
                new Mood { Name = "Sad", Icon = "😢", Color = "#4682B4", Intensity = 3 },
                new Mood { Name = "Anxious", Icon = "😰", Color = "#FF6B6B", Intensity = 4 },
                new Mood { Name = "Calm", Icon = "😌", Color = "#87CEEB", Intensity = 7 },
                new Mood { Name = "Excited", Icon = "🤩", Color = "#FF69B4", Intensity = 8 },
                new Mood { Name = "Grateful", Icon = "🙏", Color = "#D4AF37", Intensity = 9 },
                new Mood { Name = "Stressed", Icon = "😫", Color = "#8B4513", Intensity = 3 },
                new Mood { Name = "Energetic", Icon = "⚡", Color = "#FFA500", Intensity = 8 },
                new Mood { Name = "Tired", Icon = "😴", Color = "#808080", Intensity = 4 },
                new Mood { Name = "Content", Icon = "😊", Color = "#90EE90", Intensity = 7 }
            };

            foreach (var mood in testMoods)
            {
                await _database.InsertAsync(mood);
            }

            // Create 15 days of journal entries for john@email.com (userId = 1)
            var johnUser = await _database.Table<User>().FirstOrDefaultAsync(u => u.Username == "john@email.com");
            if (johnUser != null)
            {
                var entryTitles = new[]
                {
                    "Morning reflections",
                    "Today's journey",
                    "Personal growth",
                    "Grateful for",
                    "Lessons learned",
                    "New perspectives",
                    "Day's highlights",
                    "Peaceful moments",
                    "Inspiration found",
                    "Progress notes",
                    "Evening thoughts",
                    "Creative ideas",
                    "Wellness update",
                    "Life updates",
                    "Mindful observations"
                };

                var entryContents = new[]
                {
                    "Today was a productive day. I completed several important tasks and felt a sense of accomplishment. The weather was beautiful, and I managed to take a walk in the afternoon which helped clear my mind. Looking forward to tomorrow.",
                    "Started the day with some meditation and journaling. It really helps set a positive tone for the entire day. Had a great conversation with a friend about future plans. Feeling hopeful and energized.",
                    "Reflecting on my progress over the past weeks. I've made significant strides in my personal development. Challenges are still present, but I'm learning to face them with more resilience and confidence. Grateful for the support around me.",
                    "Today I'm grateful for my health, my family, and the opportunities I have. Small moments of joy made this day special. A warm cup of coffee in the morning, a good book in the afternoon, meaningful conversations in the evening.",
                    "Learned some valuable lessons today. Failure is not the end but a stepping stone to success. I realized that patience and persistence are key to achieving my goals. Every setback is a setup for a comeback.",
                    "New perspectives on old problems. Sometimes we need to step back and look at things from a different angle. Today I approached a challenging situation differently and found a better solution. Growth happens when we're willing to adapt.",
                    "Highlights of today: achieved my goals, connected with loved ones, and found time for self-care. It's important to celebrate these moments, no matter how small they seem. Each day is a gift and I'm making the most of it.",
                    "Peaceful moments today brought me joy and clarity. Spent time in nature, listened to my favorite music, and enjoyed some quiet reflection. These moments recharge my soul and remind me of what's truly important in life.",
                    "Found inspiration in unexpected places today. A random conversation, an article I read, and a piece of music all sparked new ideas and perspectives. Inspiration is everywhere if we're open to seeing it.",
                    "Progress notes: feeling stronger mentally and physically. The habits I've been building are paying off. Consistency is key, and I'm committed to maintaining these positive changes. Every day is an opportunity to become a better version of myself.",
                    "Evening thoughts as I reflect on the day. What went well? What could be improved? This reflection helps me learn and grow. I'm feeling positive about the direction my life is heading and excited about future possibilities.",
                    "Creative ideas flowing today. Spent time brainstorming and exploring new concepts. This creative energy is motivating and inspiring. Can't wait to start implementing some of these ideas and see where they lead.",
                    "Wellness update: feeling balanced and healthy. Got good sleep, exercised, and ate nutritious meals. Taking care of my physical health directly impacts my mental well-being. I'm committed to maintaining this wellness routine.",
                    "Life updates: things are progressing well. Working towards my goals while enjoying the present moment. It's about finding the balance between ambition and contentment. Grateful for where I am while excited about where I'm going.",
                    "Mindful observations today. Noticed the little things that make life beautiful. A smile from a stranger, the sound of birds in the morning, the warmth of the sun. Being present and mindful enriches every moment of my life."
                };

                var categoryIds = new[] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3 };

                for (int i = 0; i < 15; i++)
                {
                    var entryDate = DateTime.Now.AddDays(-i);
                    var entry = new JournalEntry
                    {
                        UserId = johnUser.UserId,
                        Title = entryTitles[i],
                        Content = entryContents[i],
                        EntryDate = entryDate.ToString("yyyy-MM-dd"),
                        CategoryId = categoryIds[i],
                        WordCount = CountWords(entryContents[i]),
                        CreatedAt = entryDate,
                        UpdatedAt = entryDate
                    };

                    await _database.InsertAsync(entry);
                }

                // Create tags for john@email.com
                var testTags = new List<Tag>
                {
                    new Tag { UserId = johnUser.UserId, Name = "motivation", Color = "#D4AF37", CreatedAt = DateTime.Now },
                    new Tag { UserId = johnUser.UserId, Name = "goals", Color = "#FF6B6B", CreatedAt = DateTime.Now },
                    new Tag { UserId = johnUser.UserId, Name = "gratitude", Color = "#87CEEB", CreatedAt = DateTime.Now },
                    new Tag { UserId = johnUser.UserId, Name = "reflection", Color = "#90EE90", CreatedAt = DateTime.Now },
                    new Tag { UserId = johnUser.UserId, Name = "mindfulness", Color = "#DDA0DD", CreatedAt = DateTime.Now }
                };

                foreach (var tag in testTags)
                {
                    await _database.InsertAsync(tag);
                }

                // Get all entries for tagging and mood assignment
                var allEntries = await _database.Table<JournalEntry>()
                    .Where(e => e.UserId == johnUser.UserId)
                    .ToListAsync();

                // Add tags to first 10 entries
                var tagAssignments = new[] { 1, 2, 3, 4, 5, 1, 2, 3, 4, 5 };
                for (int i = 0; i < Math.Min(10, allEntries.Count); i++)
                {
                    var entryTag = new JournalEntryTag
                    {
                        EntryId = allEntries[i].EntryId,
                        TagId = tagAssignments[i],
                        CreatedAt = DateTime.Now
                    };
                    await _database.InsertAsync(entryTag);
                }

                // Add moods to all 15 entries
                var moodAssignments = new[] { 1, 4, 6, 1, 5, 6, 4, 1, 6, 7, 4, 8, 1, 6, 4 };
                for (int i = 0; i < Math.Min(15, allEntries.Count); i++)
                {
                    var entryMood = new EntryMood
                    {
                        EntryId = allEntries[i].EntryId,
                        MoodId = moodAssignments[i],
                        Intensity = 7 + (i % 3),
                        CreatedAt = DateTime.Now
                    };
                    await _database.InsertAsync(entryMood);
                }

                // Create active 15-day streak
                var streak = new JournalStreak
                {
                    UserId = johnUser.UserId,
                    StartDate = DateTime.Now.AddDays(-14).ToString("yyyy-MM-dd"),
                    EndDate = null,
                    DayCount = 15,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-14),
                    UpdatedAt = DateTime.Now
                };
                await _database.InsertAsync(streak);

                // Create default user settings
                var settings = new List<UserSetting>
                {
                    new UserSetting { UserId = johnUser.UserId, SettingKey = "theme", SettingValue = "dark", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = johnUser.UserId, SettingKey = "notifications", SettingValue = "enabled", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = johnUser.UserId, SettingKey = "reminderTime", SettingValue = "20:00", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = johnUser.UserId, SettingKey = "wordGoal", SettingValue = "200", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };

                foreach (var setting in settings)
                {
                    await _database.InsertAsync(setting);
                }
            }
        }

        // Seed sample data for a specific user when they have an empty journal
        public async Task<(bool Seeded, string Message)> SeedSampleDataForUserAsync(int userId)
        {
            await InitAsync();

            var user = await _database!.Table<User>().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return (false, "User not found");

            var existingEntries = await _database.Table<JournalEntry>()
                .Where(e => e.UserId == userId)
                .CountAsync();

            if (existingEntries > 0)
                return (false, "You already have journal entries. No sample data added.");

            var existingCategories = await _database.Table<Category>().ToListAsync();
            if (!existingCategories.Any())
            {
                var defaults = new List<Category>
                {
                    new Category { Name = "Personal", Description = "Personal thoughts and reflections" },
                    new Category { Name = "Work", Description = "Work-related entries" },
                    new Category { Name = "Health", Description = "Health and wellness" },
                    new Category { Name = "Travel", Description = "Travel experiences" }
                };

                foreach (var category in defaults)
                {
                    await _database.InsertAsync(category);
                }

                existingCategories = await _database.Table<Category>().ToListAsync();
            }

            var existingMoods = await _database.Table<Mood>().ToListAsync();
            if (!existingMoods.Any())
            {
                var defaults = new List<Mood>
                {
                    new Mood { Name = "Happy", Icon = "😊", Color = "#FFD700", Intensity = 9 },
                    new Mood { Name = "Sad", Icon = "😢", Color = "#4682B4", Intensity = 3 },
                    new Mood { Name = "Anxious", Icon = "😰", Color = "#FF6B6B", Intensity = 4 },
                    new Mood { Name = "Calm", Icon = "😌", Color = "#87CEEB", Intensity = 7 },
                    new Mood { Name = "Excited", Icon = "🤩", Color = "#FF69B4", Intensity = 8 },
                    new Mood { Name = "Grateful", Icon = "🙏", Color = "#D4AF37", Intensity = 9 },
                    new Mood { Name = "Stressed", Icon = "😫", Color = "#8B4513", Intensity = 3 },
                    new Mood { Name = "Energetic", Icon = "⚡", Color = "#FFA500", Intensity = 8 },
                    new Mood { Name = "Tired", Icon = "😴", Color = "#808080", Intensity = 4 },
                    new Mood { Name = "Content", Icon = "😊", Color = "#90EE90", Intensity = 7 }
                };

                foreach (var mood in defaults)
                {
                    await _database.InsertAsync(mood);
                }

                existingMoods = await _database.Table<Mood>().ToListAsync();
            }
            // Seed initial tags
            var userTags = await _database.Table<Tag>()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (!userTags.Any())
            {
                var defaultNames = GetDefaultTagNames();
                var tagsToInsert = new List<Tag>();
                
                var colors = new[] { "#87CEEB", "#90EE90", "#FFA500", "#FF6B6B", "#D4AF37", "#9370DB", "#20B2AA" };
                int colorIdx = 0;

                foreach (var name in defaultNames)
                {
                    tagsToInsert.Add(new Tag 
                    { 
                        UserId = userId, 
                        Name = name, 
                        Color = colors[colorIdx % colors.Length], 
                        CreatedAt = DateTime.Now 
                    });
                    colorIdx++;
                }

                await _database.InsertAllAsync(tagsToInsert);

                userTags = await _database.Table<Tag>()
                    .Where(t => t.UserId == userId)
                    .ToListAsync();
            }

            var entryTitles = new[]
            {
                "Settling into the day",
                "Work wins and setbacks",
                "Evening gratitude",
                "Walk around the neighborhood",
                "Refocus and reset",
                "Midweek motivation",
                "Weekend planning",
                "Small joys log",
                "Energy check-in",
                "Mindful breathing session",
                "Creative spark",
                "Progress reflections",
                "Sleep and recovery",
                "Closing the week"
            };

            var entryContents = new[]
            {
                "Started slow with coffee and a quiet moment. Sketched the priorities for the day and felt calmer once things were listed out.",
                "Wrapped a tricky task at work. One blocker remains, but it's clearer now. Took short breaks to stay focused.",
                "Listened to a podcast about gratitude. Jotted down three things I appreciated today and felt my mood lift.",
                "Took a 20-minute walk after lunch. Noticed the trees changing color and felt more present.",
                "Reset the desk space, cleared emails, and planned tomorrow. A tidy area makes it easier to think.",
                "Needed extra motivation. Re-read goals and picked one small action: drafted an outline for a new idea.",
                "Outlined weekend plans: groceries, a workout, and calling family. Keeping it realistic so it feels doable.",
                "Captured small joys: good playlist, warm sunlight, and finishing a chapter of the book.",
                "Energy dipped mid-afternoon. Chose a short stretch and water break instead of more coffee.",
                "Practiced box breathing for five minutes. Heart rate slowed, and focus returned for the next task.",
                "Brainstormed a few creative angles for a side project. Nothing final, but ideas are flowing.",
                "Reflected on habits that stuck this month and the ones that slipped. Decided on one tweak for tomorrow.",
                "Tracked sleep and noticed improvement after cutting screens before bed. Want to keep that streak going.",
                "Closed out the week by reviewing wins and misses. Feeling ready to start fresh on Monday."
            };

            var entries = new List<JournalEntry>();
            for (int i = 0; i < entryTitles.Length; i++)
            {
                var entryDate = DateTime.Now.AddDays(-i);
                var category = existingCategories[i % existingCategories.Count];

                var entry = new JournalEntry
                {
                    UserId = userId,
                    Title = entryTitles[i],
                    Content = entryContents[i],
                    EntryDate = entryDate.ToString("yyyy-MM-dd"),
                    CategoryId = category.CategoryId,
                    WordCount = CountWords(entryContents[i]),
                    CreatedAt = entryDate,
                    UpdatedAt = entryDate
                };

                await _database.InsertAsync(entry);
                entries.Add(entry);
            }

            var moodLookup = existingMoods.ToDictionary(m => m.Name, m => m.MoodId);
            var moodSequence = new[]
            {
                "Calm",
                "Energetic",
                "Grateful",
                "Content",
                "Anxious",
                "Happy",
                "Calm",
                "Content",
                "Tired",
                "Calm",
                "Excited",
                "Grateful",
                "Content",
                "Happy"
            };

            for (int i = 0; i < entries.Count; i++)
            {
                var moodName = moodSequence[i % moodSequence.Length];
                if (!moodLookup.TryGetValue(moodName, out var moodId))
                {
                    moodId = existingMoods.First().MoodId;
                }

                await AddEntryMoodAsync(entries[i].EntryId, moodId, 6 + (i % 3));
            }

            for (int i = 0; i < entries.Count && userTags.Any(); i++)
            {
                var tag = userTags[i % userTags.Count];
                await AddTagToEntryAsync(entries[i].EntryId, tag.TagId);
            }

            var existingStreak = await _database.Table<JournalStreak>()
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (existingStreak == null)
            {
                var streak = new JournalStreak
                {
                    UserId = userId,
                    StartDate = DateTime.Now.AddDays(-(entries.Count - 1)).ToString("yyyy-MM-dd"),
                    EndDate = null,
                    DayCount = entries.Count,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-(entries.Count - 1)),
                    UpdatedAt = DateTime.Now
                };

                await _database.InsertAsync(streak);
            }

            var userSettings = await _database.Table<UserSetting>()
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (!userSettings.Any())
            {
                var settings = new List<UserSetting>
                {
                    new UserSetting { UserId = userId, SettingKey = "theme", SettingValue = "dark", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = userId, SettingKey = "notifications", SettingValue = "enabled", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = userId, SettingKey = "reminderTime", SettingValue = "20:00", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = userId, SettingKey = "wordGoal", SettingValue = "200", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };

                foreach (var setting in settings)
                {
                    await _database.InsertAsync(setting);
                }
            }

            return (true, "Sample data added: entries, moods, tags, streak, and settings.");
        }

        // Count words in a string
        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Split by whitespace and count
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Length;
        }

        // Hash password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return System.Convert.ToBase64String(hashedBytes);
            }
        }
        // Seed reference data (Moods & Tags) if they don't exist
        private async Task SeedReferenceDataAsync()
        {
            // --- Moods ---
            var moodCount = await _database!.Table<Mood>().CountAsync();
            if (moodCount == 0)
            {
                var moods = new List<Mood>
                {
                    // Positive
                    new Mood { Name = "Happy", Icon = "😊", Color = "#FFEB3B", Intensity = 8, IsPrimary = true },
                    new Mood { Name = "Excited", Icon = "🤩", Color = "#FFC107", Intensity = 9, IsPrimary = true },
                    new Mood { Name = "Relaxed", Icon = "😌", Color = "#8BC34A", Intensity = 6, IsPrimary = true },
                    new Mood { Name = "Grateful", Icon = "🙏", Color = "#4CAF50", Intensity = 7, IsPrimary = true },
                    new Mood { Name = "Confident", Icon = "😎", Color = "#2196F3", Intensity = 8, IsPrimary = true },
                    // Neutral
                    new Mood { Name = "Calm", Icon = "😐", Color = "#9E9E9E", Intensity = 5, IsPrimary = true },
                    new Mood { Name = "Thoughtful", Icon = "🤔", Color = "#795548", Intensity = 5, IsPrimary = true },
                    new Mood { Name = "Curious", Icon = "🧐", Color = "#607D8B", Intensity = 6, IsPrimary = true },
                    new Mood { Name = "Nostalgic", Icon = "🌇", Color = "#E91E63", Intensity = 5, IsPrimary = false },
                    new Mood { Name = "Bored", Icon = "🥱", Color = "#BDBDBD", Intensity = 3, IsPrimary = false },
                    // Negative
                    new Mood { Name = "Sad", Icon = "😢", Color = "#2196F3", Intensity = 3, IsPrimary = true },
                    new Mood { Name = "Angry", Icon = "😠", Color = "#F44336", Intensity = 2, IsPrimary = true },
                    new Mood { Name = "Stressed", Icon = "😫", Color = "#FF5722", Intensity = 2, IsPrimary = true },
                    new Mood { Name = "Lonely", Icon = "🥺", Color = "#3F51B5", Intensity = 2, IsPrimary = false },
                    new Mood { Name = "Anxious", Icon = "😰", Color = "#9C27B0", Intensity = 2, IsPrimary = true }
                };
                await _database.InsertAllAsync(moods);
            }
        }

        // Get default tags list
        public List<string> GetDefaultTagNames()
        {
            return new List<string> 
            { 
               "Work", "Career", "Studies", "Family", "Friends", "Relationships",
               "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies", "Travel", "Nature",
               "Finance", "Spirituality", "Birthday", "Holiday", "Vacation", "Celebration", "Exercise",
               "Reading", "Writing", "Cooking", "Meditation", "Yoga", "Music", "Shopping",
               "Parenting", "Projects", "Planning", "Reflection"
            };
        }
    }
}
