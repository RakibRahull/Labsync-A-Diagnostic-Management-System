using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Models
{
    [Table("Appointments")]
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [StringLength(20)]
        public string? AppointmentCode { get; set; }

        [Required]
        public int PatientId { get; set; }

        public int? ReferralDoctorId { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        public int AppointmentStatusId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        [StringLength(300)]
        public string? Remarks { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Patient Relationship
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
    }
}