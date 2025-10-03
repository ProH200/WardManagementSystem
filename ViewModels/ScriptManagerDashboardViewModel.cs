using System.ComponentModel.DataAnnotations;
using Wellness_Wardens_Project.Models;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.ViewModels
{
    public class ScriptManagerDashboardViewModel
    {
        public string WelcomeName { get; set; }
        public List<PrescriptionViewModel> PendingPrescriptions { get; set; } = new List<PrescriptionViewModel>();
        public List<PrescriptionViewModel> RecentProcessed { get; set; } = new List<PrescriptionViewModel>();
        public int PendingCount { get; set; }
        public int ProcessedCount { get; set; }
    }

    public class PrescriptionViewModel
    {
        public int PrescriptionId { get; set; }
        public string Reference { get; set; }
        [Display(Name = "Date Written")]
        public DateTime DateWritten { get; set; }

        [Display(Name = "Doctor")]
        public string DoctorName { get; set; } = string.Empty;

        [Display(Name = "Medication Count")]
        public int MedicationCount { get; set; }

        public bool IsProcessed { get; set; }
        public bool IsDelivered { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Status Color")]
        public string StatusColor { get; set; } = "warning";

        public List<PrescriptionMedication> PrescriptionMedications { get; set; } = new List<PrescriptionMedication>();
    }

    public class StockManagerDashboardViewModel
    {
        public List<ConsumableViewModel> LowStockItems { get; set; } = new List<ConsumableViewModel>();
        public List<ConsumableRequestViewModel> PendingRequests { get; set; } = new List<ConsumableRequestViewModel>();
        public int LowStockCount { get; set; }
        public int PendingRequestsCount { get; set; }
        public int TotalConsumables { get; set; }
        public int CriticalStockItems { get; set; }
        public int OutOfStockItems { get; set; }
    }

    public class ConsumableViewModel
    {
        public int ConsumableId { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Quantity Available")]
        public int QuantityAvailable { get; set; }

        [Display(Name = "Ward")]
        public string WardName { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string StockStatus { get; set; } = "In Stock";

        [Display(Name = "Status Color")]
        public string StatusColor { get; set; } = "success";

        [Display(Name = "Is Critical")]
        public bool IsCritical { get; set; }
    }

    public class ConsumableRequestViewModel
    {
        public int RequestId { get; set; }

        [Display(Name = "Consumable")]
        public string ConsumableName { get; set; } = string.Empty;

        [Display(Name = "Quantity Requested")]
        public int QuantityRequested { get; set; }

        [Display(Name = "Requested Date")]
        public DateTime RequestedDate { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; } = string.Empty;

        [Display(Name = "Ward")] 
        public string WardName { get; set; } = string.Empty;
        public bool IsDelivered { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";
    }
}