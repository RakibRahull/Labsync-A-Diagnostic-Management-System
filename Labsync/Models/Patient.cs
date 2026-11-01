using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Models
{
    [Table("Patients")]
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [StringLength(20)]
        public string? PatientCode { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        public int? CityId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(30)]
        public string? Religion { get; set; }

        [StringLength(20)]
        public string? MaritalStatus { get; set; }

        [StringLength(5)]
        public string? BloodGroup { get; set; }

        [StringLength(100)]
        public string? FatherName { get; set; }

        [StringLength(100)]
        public string? MotherName { get; set; }

        [StringLength(20)]
        public string? NID { get; set; }

        [StringLength(100)]
        public string? Occupation { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CreatedAt { get; set; }
    }
}
