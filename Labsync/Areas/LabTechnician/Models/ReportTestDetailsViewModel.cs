using System;

namespace LabSync.Areas.LabTechnician.Models
{
    public class ReportTestDetailsViewModel
    {
        // =====================================================
        // REPORT
        // =====================================================

        public int ReportId { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }


        // =====================================================
        // PATIENT
        // =====================================================

        public string? PatientCode { get; set; }

        public string PatientName { get; set; }
            = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }


        // =====================================================
        // APPOINTMENT
        // =====================================================

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }


        // =====================================================
        // TEST
        // =====================================================

        public int AppointmentTestId { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; }
            = string.Empty;

        public string? Unit { get; set; }


        // =====================================================
        // REFERENCE
        // =====================================================

        public string? ReferenceRange { get; set; }

        public string? ReferenceRangeDescription { get; set; }


        // =====================================================
        // RESULT
        // =====================================================

        public string? ResultValue { get; set; }

        public string? ResultInterpretation { get; set; }

        public string? Remarks { get; set; }

        public string? ResultStatusName { get; set; }

        public DateTime? ResultDate { get; set; }
    }
}