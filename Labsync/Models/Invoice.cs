using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Models
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [StringLength(30)]
        public string? InvoiceNumber { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Column(TypeName = "money")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "money")]
        public decimal? Discount { get; set; }

        [Column(TypeName = "money")]
        public decimal? Tax { get; set; }

        [Column(TypeName = "money")]
        public decimal NetAmount { get; set; }

        [Column(TypeName = "money")]
        public decimal? PaidAmount { get; set; }

        [Column(TypeName = "money")]
        public decimal DueAmount { get; set; }

        [Required]
        public int PaymentStatusId { get; set; }

        public DateTime? InvoiceDate { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        [ForeignKey(nameof(AppointmentId))]
        public Appointment? Appointment { get; set; }

        public ICollection<InvoiceItem> InvoiceItems { get; set; }
            = new List<InvoiceItem>();
    }
}
