using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class MedicalCondition
    {
        [Key]
        public int MedicalConditionId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsDeleted { get; set; } = false;

        //[ForeignKey(nameof(Patient))]
        //public int? PatientId { get; set; }

        //public virtual Patient Patient { get; set; }
        public virtual ICollection<PatientMedicalCondition> PatientMedicalConditions { get; set; } = new List<PatientMedicalCondition>();
    }
}
