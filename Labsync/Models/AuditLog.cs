using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityName { get; set; }

        [Required]
        public int EntityId { get; set; }

        public DateTime? Timestamp { get; set; }

        public string? Details { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}