using System;

namespace LabSync.Areas.Receptionist.Models
{
    public class ReportTestDetailsViewModel
    {
        public int AppointmentTestId { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int? TurnAroundTime { get; set; }

        public string? Unit { get; set; }

        public string? ReferenceRange { get; set; }

        public string? ReferenceRangeDescription { get; set; }

        public string? ResultValue { get; set; }

        public string? ResultInterpretation { get; set; }

        public string? Remarks { get; set; }

        public string? ResultStatusName { get; set; }

        public DateTime? ResultDate { get; set; }
    }
}