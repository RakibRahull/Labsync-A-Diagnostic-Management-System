namespace LabSync.Areas.LabTechnician.Models
{
    public class LabTechnicianTestsViewModel
    {
        // =====================================================
        // SEARCH
        // =====================================================

        public string? SearchAppointmentId { get; set; }

        public bool HasSearched { get; set; }


        // =====================================================
        // APPOINTMENT INFORMATION
        // =====================================================

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public int PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = "";

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }


        // =====================================================
        // TEST LIST
        // =====================================================

        public List<LabTechnicianTestRowViewModel> Tests { get; set; }
            = new();
    }


    public class LabTechnicianTestRowViewModel
    {
        // =====================================================
        // APPOINTMENT TEST
        // =====================================================

        public int AppointmentTestId { get; set; }

        public int AppointmentId { get; set; }


        // =====================================================
        // TEST INFORMATION
        // =====================================================

        public int TestId { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; } = "";

        public decimal Price { get; set; }

        public string? Unit { get; set; }

        public string? TurnAroundTime { get; set; }

        public bool IsActive { get; set; }


        // =====================================================
        // SAMPLE INFORMATION
        // =====================================================

        public int SampleStatusId { get; set; }

        public string? SampleStatusName { get; set; }


        // =====================================================
        // TECHNICIAN
        // =====================================================

        public int? TechnicianId { get; set; }


        // =====================================================
        // CREATED DATE
        // =====================================================

        public DateTime? CreatedAt { get; set; }
    }
}