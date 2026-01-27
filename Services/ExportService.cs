using Journal.Data;
using Journal.Models;
using System.Text;
using System.Text.Json;

namespace Journal.Services
{
    public class ExportService
    {
        private readonly JournalDatabase _database;
        private readonly AuthService _authService;
        private readonly TagService _tagService;
        private readonly MoodService _moodService;

        public ExportService(JournalDatabase database, AuthService authService, TagService tagService, MoodService moodService)
        {
            _database = database;
            _authService = authService;
            _tagService = tagService;
            _moodService = moodService;
        }

        public async Task<(bool Success, string Message)> SavePdfToDownloadsAsync(byte[] pdfData, string fileName)
        {
            try
            {
                // On MacCatalyst/iOS, direct file system access outside the sandbox is restricted.
                // The most reliable cross-platform way to "export" a file is using Microsoft.Maui.ApplicationModel.DataTransfer.Share
                
                string tempDir = FileSystem.Current.CacheDirectory;
                string filePath = Path.Combine(tempDir, fileName);
                
                await File.WriteAllBytesAsync(filePath, pdfData);

                // Share the file - this allows the user to "Save to Files" or "Download"
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Export Journal PDF",
                    File = new ShareFile(filePath)
                });

                return (true, "PDF ready for saving.");
            }
            catch (Exception ex)
            {
                return (false, $"Error saving PDF: {ex.Message}");
            }
        }

        // Get export statistics
        public async Task<(int TotalEntries, int TotalTags, int TotalMoods, DateTime? FirstEntry, DateTime? LastEntry)> GetExportStatsAsync()
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser == null)
                return (0, 0, 0, null, null);

            var entries = await _database.GetAllEntriesAsync(currentUser.UserId);
            var tagsResult = await _tagService.GetUserTagsAsync();
            var totalTags = tagsResult.Success && tagsResult.Tags != null ? tagsResult.Tags.Count : 0;

            var moodsResult = await _moodService.GetAllMoodsAsync();
            var totalMoods = moodsResult.Success && moodsResult.Moods != null ? moodsResult.Moods.Count : 0;

            DateTime? firstEntry = null;
            DateTime? lastEntry = null;

            if (entries.Any())
            {
                firstEntry = entries.Min(e => DateTime.Parse(e.EntryDate));
                lastEntry = entries.Max(e => DateTime.Parse(e.EntryDate));
            }

            return (entries.Count, totalTags, totalMoods, firstEntry, lastEntry);
        }
    }
}
