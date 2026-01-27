using Journal.Data;
using Journal.Models;

namespace Journal.Services
{
    public class TagService
    {
        private readonly JournalDatabase _database;
        private readonly AuthService _authService;

        public TagService(JournalDatabase database, AuthService authService)
        {
            _database = database;
            _authService = authService;
        }

        // Get all tags for the current user
        public async Task<(bool Success, List<Tag>? Tags, string Message)> GetUserTagsAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var tags = await _database.GetTagsByUserAsync(currentUser.UserId);
                return (true, tags, "Tags retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving tags: {ex.Message}");
            }
        }

        // Get a specific tag by ID
        public async Task<(bool Success, Tag? Tag, string Message)> GetTagByIdAsync(int tagId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var tag = await _database.GetTagByIdAsync(tagId);

                if (tag == null)
                    return (false, null, "Tag not found");

                // Verify tag belongs to current user
                if (tag.UserId != currentUser.UserId)
                    return (false, null, "Unauthorized access to tag");

                return (true, tag, "Tag retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving tag: {ex.Message}");
            }
        }

        // Create a new tag
        public async Task<(bool Success, string Message)> CreateTagAsync(string tagName, string? color = null)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Validate tag name
                if (string.IsNullOrWhiteSpace(tagName))
                    return (false, "Tag name cannot be empty");

                if (tagName.Length > 50)
                    return (false, "Tag name too long (max 50 characters)");

                // Check if tag already exists for this user
                var exists = await _database.TagExistsAsync(currentUser.UserId, tagName);
                if (exists)
                    return (false, "Tag with this name already exists");

                var tag = new Tag
                {
                    UserId = currentUser.UserId,
                    Name = tagName.Trim(),
                    Color = color,
                    CreatedAt = DateTime.Now
                };

                await _database.CreateTagAsync(tag);
                return (true, "Tag created successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error creating tag: {ex.Message}");
            }
        }

        // Update an existing tag
        public async Task<(bool Success, string Message)> UpdateTagAsync(int tagId, string tagName, string? color = null)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Validate tag name
                if (string.IsNullOrWhiteSpace(tagName))
                    return (false, "Tag name cannot be empty");

                if (tagName.Length > 50)
                    return (false, "Tag name too long (max 50 characters)");

                // Get existing tag
                var tag = await _database.GetTagByIdAsync(tagId);
                if (tag == null)
                    return (false, "Tag not found");

                // Verify ownership
                if (tag.UserId != currentUser.UserId)
                    return (false, "Unauthorized access to tag");

                // Check if new name conflicts with another tag
                if (tag.Name != tagName.Trim())
                {
                    var exists = await _database.TagExistsAsync(currentUser.UserId, tagName);
                    if (exists)
                        return (false, "Tag with this name already exists");
                }

                tag.Name = tagName.Trim();
                tag.Color = color;

                await _database.UpdateTagAsync(tag);
                return (true, "Tag updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating tag: {ex.Message}");
            }
        }

        // Delete a tag
        public async Task<(bool Success, string Message)> DeleteTagAsync(int tagId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Get existing tag
                var tag = await _database.GetTagByIdAsync(tagId);
                if (tag == null)
                    return (false, "Tag not found");

                // Verify ownership
                if (tag.UserId != currentUser.UserId)
                    return (false, "Unauthorized access to tag");

                await _database.DeleteTagAsync(tagId);
                return (true, "Tag deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting tag: {ex.Message}");
            }
        }

        // Add tag to an entry
        public async Task<(bool Success, string Message)> AddTagToEntryAsync(int entryId, int tagId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                // Verify tag belongs to user
                var tag = await _database.GetTagByIdAsync(tagId);
                if (tag == null)
                    return (false, "Tag not found");

                if (tag.UserId != currentUser.UserId)
                    return (false, "Unauthorized access to tag");

                // Verify entry belongs to user
                var entry = await _database.GetEntryByIdAsync(entryId);
                if (entry == null)
                    return (false, "Entry not found");

                if (entry.UserId != currentUser.UserId)
                    return (false, "Unauthorized access to entry");

                await _database.AddTagToEntryAsync(entryId, tagId);
                return (true, "Tag added to entry");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding tag to entry: {ex.Message}");
            }
        }

        // Remove tag from an entry
        public async Task<(bool Success, string Message)> RemoveTagFromEntryAsync(int entryId, int tagId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, "User not authenticated");

                await _database.RemoveTagFromEntryAsync(entryId, tagId);
                return (true, "Tag removed from entry");
            }
            catch (Exception ex)
            {
                return (false, $"Error removing tag from entry: {ex.Message}");
            }
        }

        // Get all tags for a specific entry
        public async Task<(bool Success, List<Tag>? Tags, string Message)> GetEntryTagsAsync(int entryId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var tags = await _database.GetTagsByEntryAsync(entryId);
                return (true, tags, "Entry tags retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving entry tags: {ex.Message}");
            }
        }

        // Get all entries with a specific tag
        public async Task<(bool Success, List<JournalEntry>? Entries, string Message)> GetEntriesByTagAsync(int tagId)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return (false, null, "User not authenticated");

                var entries = await _database.GetEntriesByTagAsync(currentUser.UserId, tagId);
                return (true, entries, "Entries retrieved successfully");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error retrieving entries by tag: {ex.Message}");
            }
        }

        // Check if tag name exists for user
        public async Task<bool> TagExistsAsync(string tagName)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                    return false;

                return await _database.TagExistsAsync(currentUser.UserId, tagName);
            }
            catch
            {
                return false;
            }
        }
    }
}
