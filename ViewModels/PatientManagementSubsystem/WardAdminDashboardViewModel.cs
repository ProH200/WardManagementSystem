using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem
{
    public class WardAdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalAdmissions { get; set; }
        public int ActiveAdmissions { get; set; }
        public int TotalDischarges { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }

    public class RecentActivity
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
