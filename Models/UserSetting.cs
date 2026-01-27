using SQLite;

namespace Journal.Models
{
    [Table("UserSettings")]
    public class UserSetting
    {
        [PrimaryKey, AutoIncrement]
        public int SettingId { get; set; }

        public int UserId { get; set; }

        public string SettingKey { get; set; } = string.Empty;

        public string SettingValue { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
