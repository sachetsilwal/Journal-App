using SQLite;

namespace Journal.Models
{
    [Table("Moods")]
    public class Mood
    {
        [PrimaryKey, AutoIncrement]
        public int MoodId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public string? Color { get; set; }

        public int Intensity { get; set; }

        [Ignore]
        public bool IsPrimary { get; set; }
    }
}
