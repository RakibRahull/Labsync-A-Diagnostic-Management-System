namespace LabSync.Models
{
    public class UserDetails
    {
        public int Id { get; set; }

        // =========================
        // RELATION
        // =========================

        public int UserId { get; set; }

        // =========================
        // EMPLOYMENT
        // =========================

        public int DesignationId { get; set; }

        public int DepartmentId { get; set; }

        public int EmployeeTypeId { get; set; }

        public int BranchId { get; set; }

        // =========================
        // PERSONAL INFORMATION
        // =========================

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public int? CityId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Religion { get; set; }

        public string? MaritalStatus { get; set; }

        public string? BloodGroup { get; set; }

        public string? FatherName { get; set; }

        public string? MotherName { get; set; }

        public string? NID { get; set; }

        public string? EmergencyContact { get; set; }

        public string? Education { get; set; }

        // =========================
        // EMPLOYMENT DETAILS
        // =========================

        public DateTime? JoiningDate { get; set; }

        public decimal? CurrentSalary { get; set; }

        // =========================
        // SYSTEM
        // =========================

        public DateTime? CreatedAt { get; set; }


        // =========================
        // NAVIGATION
        // =========================

        public User User { get; set; } = default!;
    }
}
