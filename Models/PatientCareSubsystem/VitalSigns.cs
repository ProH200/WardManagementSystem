using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.PatientCareSubsystem
{
    public class VitalSigns
    {
        [Key]
        public int VitalId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public double Temperature { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public int HeartRate { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public string BloodPressure { get; set; }
        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Employee))]
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
        [ForeignKey(nameof(Patient))] 
        public int? PatientId { get; set; }
        public virtual Patient Patient { get; set; }
        public DateTime RecordedDate { get; internal set; }
    }
}
