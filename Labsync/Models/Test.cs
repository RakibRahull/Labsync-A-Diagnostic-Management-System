using System.ComponentModel.DataAnnotations;

namespace LabSync.Models
{
    public class Test
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public int SampleTypeId { get; set; }

        public int ReferenceRangeId { get; set; }

        [StringLength(20)]
        public string? TestCode { get; set; }

        [Required]
        [StringLength(150)]
        public string TestName { get; set; } = default!;

        public decimal Price { get; set; }

        public int? TurnAroundTime { get; set; }

        [StringLength(30)]
        public string? Unit { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}