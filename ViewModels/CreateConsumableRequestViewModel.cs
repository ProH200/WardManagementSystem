using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Wellness_Wardens_Project.ViewModels
{
    public class CreateConsumableRequestViewModel
    {
        [Required(ErrorMessage = "Please select a consumable")]
        [Display(Name = "Consumable")]
        public int SelectedConsumableId { get; set; }

        [Required(ErrorMessage = "Please select a ward")]
        [Display(Name = "Ward")]
        public int SelectedWardId { get; set; }

        [Required(ErrorMessage = "Please enter quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity Requested")]
        public int QuantityRequested { get; set; }

        [Display(Name = "Requested Date")]
        public DateTime RequestedDate { get; set; }

        // Dropdown lists
        public List<SelectListItem> AvailableConsumables { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableWards { get; set; } = new List<SelectListItem>();

        // Low stock suggestions
        public List<LowStockSuggestionViewModel> LowStockSuggestions { get; set; } = new List<LowStockSuggestionViewModel>();
    }

    public class LowStockSuggestionViewModel
    {
        public int ConsumableId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public string WardName { get; set; } = string.Empty;
        public int WardId { get; set; }
        public int SuggestedQuantity { get; set; }
    }
}