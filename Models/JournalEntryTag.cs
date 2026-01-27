using SQLite;

namespace Journal.Models
{
    [Table("EntryTags")]
    public class JournalEntryTag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int EntryId { get; set; }

        public int TagId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
