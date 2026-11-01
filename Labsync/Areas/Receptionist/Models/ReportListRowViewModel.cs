
using System;

namespace LabSync.Areas.Receptionist.Models
{
    public class ReportListRowViewModel
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        // User IDs are integers
        public int GeneratedBy { get; set; }

        public string? GeneratedByEmail { get; set; }

        public DateTime GeneratedDate { get; set; }

        public string? PdfPath { get; set; }

        public int ApprovalStatusId { get; set; }

        public string? ApprovalStatusName { get; set; }

        public int? ApprovedBy { get; set; }

        public string? ApprovedByEmail { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public bool IsDelivered { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public int? DeliveredBy { get; set; }
    }
}