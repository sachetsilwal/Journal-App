using SQLite;

namespace Journal.Models
{
    // User model - represents a user in the database
    [Table("Users")]
    public class User
    {
        // Unique identifier for the user
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int UserId { get; set; }

        // Email address used for login - must be unique
        [SQLite.Unique]
        public string Username { get; set; } = string.Empty;

        // Hashed password for security
        public string PasswordHash { get; set; } = string.Empty;

        // When the user account was created
        public DateTime CreatedAt { get; set; }

        // Last time the user logged in
        public DateTime? LastLogin { get; set; }
    }
}
