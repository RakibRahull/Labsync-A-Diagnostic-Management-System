namespace LabSync.Models
{
    public class RecentActivityQueryResult
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? ActivityDate { get; set; }

        public string Icon { get; set; } = string.Empty;

        public string IconClass { get; set; } = string.Empty;
    }
}
