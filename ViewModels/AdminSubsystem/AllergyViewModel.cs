using System.ComponentModel.DataAnnotations;

namespace Wellness_Wardens_Project.ViewModels.AdminSubsystem
{
    public class AllergyViewModel
    {
        public int AllergyId { get; set; }

        [Required(ErrorMessage = "Allergy name is required")]
        [StringLength(160, ErrorMessage = "Name cannot exceed 160 characters")]
        [Display(Name = "Allergy Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
    }
}