using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.DoctorPatientSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Employee : IdentityUser
    {
        [Required(ErrorMessage = "This field is required.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        public string Title {  get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        [DataType(DataType.Password)]
        [NotMapped]
        public string Password { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [NotMapped]
        public string ConfirmPassword { get; set; }

        public bool IsDeleted { get; set; } = false;
        public List<string> AvailableRoles { get; set; } = new List<string>();

        // Foreign key - optional assigned ward
        [ForeignKey("Ward")]
        public int? WardId { get; set; }

        // Navigation property
        public virtual Ward? Ward { get; set; }

        public virtual ICollection<Consumable> Consumables { get; set; }
        public virtual ICollection<Medication> Medications { get; set; }
        public virtual ICollection<Allergy> Allergies { get; set; }
        public virtual ICollection<PatientAdmission> PatientAdmissions { get; set; }
        public virtual ICollection<ScheduleVisit> ScheduleVisits { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; }
        public virtual ICollection<ConsumablesRequest> ConsumablesRequests { get; set; }
        public virtual ICollection<VitalSigns> VitalSigns { get; set; }
        public virtual ICollection<PatientAdmission> AssignedAdmissions { get; set; }

    }
}
