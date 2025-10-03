using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.PatientCareSubsystem
{
    public class DoctorVisit
    {
        [Key]
        public int VisitId { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [DataType(DataType.Date)]
        [RegularExpression(@"^\d{4}/\d{2}/\d{2}$")]
        public DateTime VisitDate { get; set; }

        public string Instructions { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }

        public virtual Patient Patient { get; set; }

    }
}
