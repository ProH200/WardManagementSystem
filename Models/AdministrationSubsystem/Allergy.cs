using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Allergy
    {
        [Key]
        public int AllergyId { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }

        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }

        public virtual Patient Patient { get; set; }
        public virtual Employee Employee { get; set; }
    }
}
