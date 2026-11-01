namespace LabSync.Models
{
    public class AdminAnalyticsViewModel
    {
        public int TotalPatients { get; set; }

        public int TotalAppointments { get; set; }

        public int TotalReports { get; set; }

        public int ApprovedReports { get; set; }

        public int PendingReports { get; set; }

        public int RejectedReports { get; set; }

        public int CompletedTests { get; set; }
    }
}
