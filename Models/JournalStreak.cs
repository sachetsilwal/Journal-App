using SQLite;

namespace Journal.Models
{
    [Table("Streaks")]
    public class JournalStreak
    {
        [PrimaryKey, AutoIncrement]
        public int StreakId { get; set; }

        public int UserId { get; set; }

        public string StartDate { get; set; } = string.Empty;

        public string? EndDate { get; set; }

        public int DayCount { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
