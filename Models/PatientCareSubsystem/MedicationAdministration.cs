using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.PatientCareSubsystem
{
    public class MedicationAdministration
    {
        [Key]
        public int AdministrationId { get; set; }

        [Required, ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        [Required, ForeignKey(nameof(Medication))]
        public int MedicationId { get; set; }
        public virtual Medication Medication { get; set; }

        [Required, ForeignKey(nameof(Employee))]
        public string AdministeredById { get; set; }
        public virtual Employee Employee { get; set; }

        [Required]
        public DateTime AdministrationDate { get; set; } = DateTime.Now;

        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
    }
}

