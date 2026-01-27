using SQLite;

namespace Journal.Models
{
    // Category model for organizing journal entries
    [Table("Categories")]
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
    }
}
