using System;
using System.Collections.Generic;

namespace LabSync.Models
{
    public class AdminAppointmentDetailsViewModel
    {
        // Selected date
        public DateTime SelectedDate { get; set; }

        // Patient ID search text
        public string? PatientIdSearch { get; set; }

        // Total appointments for selected date
        public int TotalAppointments { get; set; }

        // Appointment list
        public List<AdminAppointmentRowViewModel> Appointments { get; set; }
            = new List<AdminAppointmentRowViewModel>();
    }


    public class AdminAppointmentRowViewModel
    {
        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public string? PatientCode { get; set; }

        public string? PatientName { get; set; }

        public string? Phone { get; set; }

        public string? Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public TimeSpan AppointmentTime { get; set; }

        public string? TestName { get; set; }

        public string? StatusName { get; set; }

        public string? Remarks { get; set; }
    }
}
