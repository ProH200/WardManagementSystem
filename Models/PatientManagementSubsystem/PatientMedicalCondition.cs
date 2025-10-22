using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class PatientMedicalCondition
    {
        [Key]
        public int PatientMedicalConditionId { get; set; }

        [Required]
        [ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }

        [Required]
        [ForeignKey(nameof(MedicalCondition))]
        public int MedicalConditionId { get; set; }

        // Additional fields for the relationship
        public DateTime DiagnosedDate { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation Properties
        public virtual Patient Patient { get; set; }
        public virtual MedicalCondition MedicalCondition { get; set; }
    }
}
