using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public DateTime? PaymentDate { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        [Column(TypeName = "money")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? TransactionNumber { get; set; }

        [StringLength(300)]
        public string? Remarks { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(100)]
        public string? MobileBanking { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        [ForeignKey(nameof(PaymentMethodId))]
        public PaymentMethod? PaymentMethod { get; set; }
    }
}
