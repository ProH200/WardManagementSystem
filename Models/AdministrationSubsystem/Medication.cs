using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Medication
    {
        [Key]
        public int MedicationId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int QuantityAvailable { get; set; }
        public int ScheduleLevel { get; set; }
        public bool IsScheduledMedication { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public virtual ICollection<TreatmentMedication> TreatmentMedications { get; set; } = new List<TreatmentMedication>();

        // Many-to-many with Prescription
        public virtual ICollection<PrescriptionMedication> PrescriptionMedications { get; set; } = new List<PrescriptionMedication>();
    }

}
