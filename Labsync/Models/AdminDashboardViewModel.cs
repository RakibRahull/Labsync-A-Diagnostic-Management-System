namespace LabSync.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TodayAppointments { get; set; }
        public int PendingReports { get; set; }
        public int CompletedTests { get; set; }

        public List<TodayAppointmentViewModel> TodayAppointmentsList { get; set; }
            = new List<TodayAppointmentViewModel>();

        public List<RecentActivityViewModel> RecentActivities { get; set; }
            = new List<RecentActivityViewModel>();
    }


}
