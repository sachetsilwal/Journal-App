using Journal.Data;
using Journal.Models;

namespace Journal.Services
{
    public class SettingsService
    {
        private readonly JournalDatabase _database;
        private readonly AuthService _authService;

        public SettingsService(JournalDatabase database, AuthService authService)
        {
            _database = database;
            _authService = authService;
        }

        // Get a specific setting value
        public async Task<(bool Success, string? Value, string Message)> GetSettingAsync(string settingKey)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var setting = await _database.GetSettingByKeyAsync(currentUser.UserId, settingKey);

                if (setting == null)
                    return (false, null, $"Setting '{settingKey}' not found");

                return (true, setting.SettingValue, "Setting retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving setting: {ex.Message}");
            }
        }

        // Update or create a setting
        public async Task<(bool Success, string Message)> UpdateSettingAsync(string settingKey, string settingValue)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                if (string.IsNullOrWhiteSpace(settingKey))
                    return (false, "Setting key cannot be empty");

                var existingSetting = await _database.GetSettingByKeyAsync(currentUser.UserId, settingKey);

                if (existingSetting != null)
                {
                    // Update existing setting
                    existingSetting.SettingValue = settingValue;
                    existingSetting.UpdatedAt = DateTime.Now;
                    await _database.UpdateSettingAsync(existingSetting);
                }
                else
                {
                    // Create new setting
                    var newSetting = new UserSetting
                    {
                        UserId = currentUser.UserId,
                        SettingKey = settingKey,
                        SettingValue = settingValue,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await _database.CreateSettingAsync(newSetting);
                }

                return (true, "Setting updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating setting: {ex.Message}");
            }
        }

        // Get all settings for the current user
        public async Task<(bool Success, List<UserSetting>? Settings, string Message)> GetAllSettingsAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var settings = await _database.GetAllUserSettingsAsync(currentUser.UserId);
                return (true, settings, "Settings retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving settings: {ex.Message}");
            }
        }

        // Initialize default settings for a user
        public async Task<(bool Success, string Message)> InitializeDefaultSettingsAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Check if settings already exist
                var existingSettings = await _database.GetAllUserSettingsAsync(currentUser.UserId);
                if (existingSettings.Any())
                    return (true, "Settings already initialized");

                // Create default settings
                var defaultSettings = new List<UserSetting>
                {
                    new UserSetting { UserId = currentUser.UserId, SettingKey = "theme", SettingValue = "light", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = currentUser.UserId, SettingKey = "notifications", SettingValue = "enabled", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = currentUser.UserId, SettingKey = "reminderTime", SettingValue = "20:00", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new UserSetting { UserId = currentUser.UserId, SettingKey = "wordGoal", SettingValue = "200", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };

                foreach (var setting in defaultSettings)
                {
                    await _database.CreateSettingAsync(setting);
                }

                return (true, "Default settings initialized successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error initializing settings: {ex.Message}");
            }
        }

        // Theme-specific methods
        public async Task<string> GetThemeAsync()
        {
            var result = await GetSettingAsync("theme");
            return result.Success ? result.Value ?? "light" : "light";
        }

        public async Task<(bool Success, string Message)> UpdateThemeAsync(string theme)
        {
            if (theme != "light" && theme != "dark" && theme != "auto")
                return (false, "Invalid theme. Must be 'light', 'dark', or 'auto'");

            return await UpdateSettingAsync("theme", theme);
        }

        // Notifications-specific methods
        public async Task<bool> GetNotificationsEnabledAsync()
        {
            var result = await GetSettingAsync("notifications");
            return result.Success && result.Value == "enabled";
        }

        public async Task<(bool Success, string Message)> UpdateNotificationsAsync(bool enabled)
        {
            return await UpdateSettingAsync("notifications", enabled ? "enabled" : "disabled");
        }

        // Reminder time-specific methods
        public async Task<string> GetReminderTimeAsync()
        {
            var result = await GetSettingAsync("reminderTime");
            return result.Success ? result.Value ?? "20:00" : "20:00";
        }

        public async Task<(bool Success, string Message)> UpdateReminderTimeAsync(string time)
        {
            // Validate time format (HH:mm)
            if (!TimeSpan.TryParse(time, out _))
                return (false, "Invalid time format. Use HH:mm (e.g., 20:00)");

            return await UpdateSettingAsync("reminderTime", time);
        }

        // Word goal-specific methods
        public async Task<int> GetWordGoalAsync()
        {
            var result = await GetSettingAsync("wordGoal");
            if (result.Success && int.TryParse(result.Value, out int goal))
                return goal;
            return 200; // Default
        }

        public async Task<(bool Success, string Message)> UpdateWordGoalAsync(int goal)
        {
            if (goal < 0)
                return (false, "Word goal must be a positive number");

            if (goal > 10000)
                return (false, "Word goal too high (max 10,000 words)");

            return await UpdateSettingAsync("wordGoal", goal.ToString());
        }

        // Delete a setting
        public async Task<(bool Success, string Message)> DeleteSettingAsync(string settingKey)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                await _database.DeleteSettingAsync(currentUser.UserId, settingKey);
                return (true, "Setting deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting setting: {ex.Message}");
            }
        }

        // Get settings as dictionary
        public async Task<Dictionary<string, string>> GetSettingsDictionaryAsync()
        {
            var result = await GetAllSettingsAsync();
            if (!result.Success || result.Settings == null)
                return new Dictionary<string, string>();

            return result.Settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
        }
    }
}
