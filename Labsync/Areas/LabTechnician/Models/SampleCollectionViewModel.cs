namespace LabSync.Areas.LabTechnician.Models
{
    public class SampleCollectionViewModel
    {
        public int AppointmentTestId { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }

        public int PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = "";

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public int TestId { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; } = "";

        public decimal Price { get; set; }

        public string? Unit { get; set; }

        public string? TurnAroundTime { get; set; }

        public bool IsActive { get; set; }


        // =====================================================
        // SAMPLE TYPE
        // =====================================================

        public int SampleTypeId { get; set; }

        public string? SampleTypeName { get; set; }

        public string? SampleTypeDescription { get; set; }


        // =====================================================
        // SAMPLE STATUS
        // =====================================================

        public int SampleStatusId { get; set; }

        public string SampleStatusName { get; set; } = "Pending";
    }


    // =========================================================
    // SAMPLE LIST ROW
    // =========================================================

    public class SampleListRowViewModel
    {
        public int AppointmentTestId { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = "";

        public string? Phone { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; } = "";

        public int SampleTypeId { get; set; }

        public string? SampleTypeName { get; set; }

        public int SampleStatusId { get; set; }

        public string SampleStatusName { get; set; } = "Pending";

        public DateTime? CreatedAt { get; set; }
    }


    // =========================================================
    // SAMPLE STATUS OPTION
    // =========================================================

    public class SampleStatusOptionViewModel
    {
        public int Id { get; set; }

        public string StatusName { get; set; } = "";
    }
}
