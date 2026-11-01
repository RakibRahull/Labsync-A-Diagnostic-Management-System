namespace LabSync.Areas.LabTechnician.Models
{
    public class LabTechnicianAppointmentViewModel
    {
        public string? SearchAppointmentId { get; set; }

        public DateTime SelectedDate { get; set; }

        public int TotalAppointments { get; set; }

        public List<LabTechnicianAppointmentRowViewModel> Appointments { get; set; }
            = new();
    }


    public class LabTechnicianAppointmentRowViewModel
    {
        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public int PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = "";

        public string? Phone { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }

        public string TestName { get; set; }
            = "General Diagnostic";

        public string StatusName { get; set; }
            = "Pending";

        public string? Remarks { get; set; }
    }
}
