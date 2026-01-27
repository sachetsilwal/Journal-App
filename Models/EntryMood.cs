using SQLite;

namespace Journal.Models
{
    [Table("EntryMoods")]
    public class EntryMood
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int EntryId { get; set; }

        public int MoodId { get; set; }

        public int? Intensity { get; set; }
        
        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
