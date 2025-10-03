using Wellness_Wardens_Project.Models.ConsumablesSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class PrescriptionMedication
    {
        public int PrescriptionId { get; set; }
        public virtual Prescription Prescription { get; set; }

        public int MedicationId { get; set; }
        public virtual Medication Medication { get; set; }

        // Optional: add dosage/frequency info here
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int DurationDays { get; set; }
    }

}
