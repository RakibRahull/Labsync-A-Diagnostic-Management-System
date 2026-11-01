namespace LabSync.Models
{
    public class User
    {
        // =========================
        // ACCOUNT INFORMATION
        // =========================

        public int Id { get; set; }

        public string Email { get; set; } = default!;

        public string PasswordHash { get; set; } = default!;

        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }


        // =========================
        // NAVIGATION
        // =========================

        public Role Role { get; set; } = default!;

        public UserDetails UserDetails { get; set; } = default!;
    }
}