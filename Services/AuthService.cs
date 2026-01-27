using Journal.Data;
using Journal.Models;
using System.Security.Cryptography;
using System.Text;


namespace Journal.Services
{
    // Authentication service - handles user login and registration
    public class AuthService
    {
        private readonly JournalDatabase _database;
        private User? _currentUser;

        public AuthService(JournalDatabase database)
        {
            _database = database;
        }

        // Get the currently logged-in user
        public User? CurrentUser => _currentUser;

        // Check if a user is logged in
        public bool IsAuthenticated => _currentUser != null;

        // Login with username and password
        public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Please enter both email and password");
                }

                // Get user from database
                var user = await _database.GetUserByUsernameAsync(username);

                if (user == null)
                {
                    return (false, "Invalid email or password");
                }

                // Hash the entered password and compare
                string hashedPassword;
                
                // Support for test data (simple "hash_" prefix)
                if (user.PasswordHash.StartsWith("hash_"))
                {
                    hashedPassword = "hash_" + password;
                }
                else
                {
                    // Standard SHA256 hashing for new users
                    hashedPassword = HashPassword(password);
                }

                if (user.PasswordHash != hashedPassword)
                {
                    return (false, "Invalid email or password");
                }

                // Login successful
                _currentUser = user;

                // Update last login time
                await _database.UpdateLastLoginAsync(user.UserId);

                // Auto-seed sample data if user has no entries
                var entryCount = await _database.GetEntryCountAsync(user.UserId);
                if (entryCount == 0)
                {
                    await _database.SeedSampleDataForUserAsync(user.UserId);
                }

                return (true, "Login successful!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Register a new user
        public async Task<(bool Success, string Message)> SignupAsync(string username, string password, string confirmPassword)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(username))
                {
                    return (false, "Email is required");
                }

                if (!IsValidEmail(username))
                {
                    return (false, "Please enter a valid email address");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Password is required");
                }

                if (password.Length < 6)
                {
                    return (false, "Password must be at least 6 characters");
                }

                if (password != confirmPassword)
                {
                    return (false, "Passwords do not match");
                }

                // Check if username already exists
                if (await _database.UsernameExistsAsync(username))
                {
                    return (false, "Email already registered");
                }

                // Create new user
                var newUser = new User
                {
                    Username = username,
                    PasswordHash = HashPassword(password),
                    CreatedAt = DateTime.Now,
                    LastLogin = null
                };

                // Save to database
                await _database.CreateUserAsync(newUser);

                return (true, "Account created successfully! Please login.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        // Logout the current user
        public void Logout()
        {
            _currentUser = null;
        }

        // Hash password using SHA256 (simple implementation for demo)
        // Note: In production, use BCrypt or similar
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        // Validate email format
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
