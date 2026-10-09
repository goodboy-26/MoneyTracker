namespace MoneyTracker.ViewModels
{
    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsLocked { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}