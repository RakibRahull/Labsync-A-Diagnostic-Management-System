using System.ComponentModel.DataAnnotations;

namespace LabSync.Models
{
    public class Report
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public int GeneratedBy { get; set; }

        public DateTime? GeneratedDate { get; set; }

        [StringLength(300)]
        public string? PdfPath { get; set; }

        public int ApprovalStatusId { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }


        // =====================================================
        // DELIVERY
        // =====================================================

        public bool IsDelivered { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public int? DeliveredBy { get; set; }


        // =====================================================
        // NAVIGATION PROPERTIES
        // =====================================================

        public Appointment Appointment { get; set; } = default!;

        public User GeneratedByUser { get; set; } = default!;

        public User? ApprovedByUser { get; set; }

        public ReportApprovalStatus ApprovalStatus { get; set; } = default!;
    }
}