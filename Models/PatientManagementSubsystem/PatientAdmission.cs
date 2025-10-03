using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class PatientAdmission
    {
        [Key]
        public int AdmissionId { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [DataType(DataType.Date)]
        [RegularExpression(@"^\d{4}/\d{2}/\d{2}$")]
        public DateTime? AdmissionDate { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }
        
        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }

        [ForeignKey(nameof(AssignedEmployee))]
        public string? AssignedEmployeeId { get; set; }
        public virtual Employee AssignedEmployee { get; set; }

        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Bed))]
        public int? BedId { get; set; }

        public virtual ICollection<PatientMovement> PatientMovements { get; set; }
        public virtual ICollection<Discharge> Discharges { get; set; }

        public virtual Patient Patient { get; set; }
        public virtual Employee Employee { get; set; }
        public virtual Bed Bed { get; set; }
    }
}
