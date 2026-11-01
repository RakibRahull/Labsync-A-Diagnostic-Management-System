using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Models
{
    [Table("InvoiceItems")]
    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int TestId { get; set; }

        public int? Quantity { get; set; }

        [Column(TypeName = "money")]
        public decimal UnitPrice { get; set; }

        // Database-এর Computed Column
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "money")]
        public decimal? TotalPrice { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        [ForeignKey(nameof(TestId))]
        public Test? Test { get; set; }
    }
}