using SQLite;

namespace Journal.Models
{
    // Journal Entry model - represents a single journal entry
    [Table("JournalEntries")]
    public class JournalEntry
    {
        // Unique identifier for the entry
        [PrimaryKey, AutoIncrement]
        public int EntryId { get; set; }

        // User who created this entry
        public int UserId { get; set; }

        // Title of the entry (optional)
        public string? Title { get; set; }

        // Main content of the journal entry
        public string Content { get; set; } = string.Empty;

        // Date of the entry (YYYY-MM-DD format)
        public string EntryDate { get; set; } = string.Empty;

        // Category ID (optional)
        public int? CategoryId { get; set; }

        // Word count
        public int WordCount { get; set; }

        // When the entry was created
        public DateTime CreatedAt { get; set; }

        // When the entry was last updated
        public DateTime UpdatedAt { get; set; }
    }
}
