using System.ComponentModel.DataAnnotations;

namespace LabSync.Models
{
    public class ReportApprovalStatus
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; } = default!;
    }
}
