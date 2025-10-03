using System.ComponentModel.DataAnnotations;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class TreatmentMedication
    {
        [Key]
        public int TreatmentMedicationId { get; set; }

        [Required]
        public int TreatmentId { get; set; }
        public virtual Treatment Treatment { get; set; }

        [Required]
        public int? MedicationId { get; set; }
        public virtual Medication Medication { get; set; }

        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int DurationDays { get; set; }
    }

}
