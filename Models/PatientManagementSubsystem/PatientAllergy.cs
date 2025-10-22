using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class PatientAllergy
    {
        [Key]
        public int PatientAllergyId { get; set; }

        [Required]
        [ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }

        [Required]
        [ForeignKey(nameof(Allergy))]
        public int AllergyId { get; set; }

        public DateTime DiagnosedDate { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        public virtual Patient Patient { get; set; }
        public virtual Allergy Allergy { get; set; }
    }
}
