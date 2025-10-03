using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.ConsumablesSubsystem
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime DateWritten { get; set; }
        public string? Instructions { get; set; }
        public bool IsProcessed { get; set; } = false;
        public bool IsDelivered { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateDelivered { get; set; }
        [Required]
        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        public virtual ICollection<PrescriptionMedication> PrescriptionMedications { get; set; } = new List<PrescriptionMedication>();
    }

}
