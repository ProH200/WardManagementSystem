using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class PatientMedication
    {
        [Key]
        public int PatientMedicationId { get; set; }

        // Foreign key to Patient
        [ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        // Foreign key to Medication
        [ForeignKey(nameof(Medication))]
        public int MedicationId { get; set; }
        public virtual Medication Medication { get; set; }

        // Foreign key to Employee who assigned the medication
        [ForeignKey(nameof(Employee))]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Assignment details - only what's needed for tracking
        public DateTime AssignmentDate { get; set; }

        [Required, StringLength(50)]
        public string Dosage { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Frequency { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityAssigned { get; set; }

        public int DurationDays { get; set; }

        [StringLength(500)]
        public string AdministrationNotes { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
    }
}
