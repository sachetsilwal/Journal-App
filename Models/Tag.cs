using SQLite;

namespace Journal.Models
{
    [Table("Tags")]
    public class Tag
    {
        [PrimaryKey, AutoIncrement]
        public int TagId { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Color { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
