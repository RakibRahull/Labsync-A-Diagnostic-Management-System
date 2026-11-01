namespace LabSync.Models
{
    public class Role
    {
        public int Id { get; set; }

        public string RoleName { get; set; } = default!;

        public string Description { get; set; } = default!;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
