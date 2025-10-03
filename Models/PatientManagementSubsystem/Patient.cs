using System.ComponentModel.DataAnnotations;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required(ErrorMessage ="This field is required"),StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required"), StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "This field is required"), StringLength(13)]
        public string IdentityNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required"), StringLength(15)]
        public string EmergencyContact { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required"), StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required"), StringLength(100)]
        public string HomeAddress { get; set; } = string.Empty;

        public string? ChronicCondition { get; set; } = string.Empty;

        public string? ChronicMedication { get; set; } = string.Empty;
        public string? MedicalHistory {  get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
        public string? EmployeeId { get; set; } 
        public virtual ICollection<PatientMedicalHistory> MedicalHistories { get; set; } = new List<PatientMedicalHistory>();
        public virtual ICollection<MedicalCondition> MedicalConditions { get; set; }
        public virtual ICollection<Allergy> Allergies { get; set; }
        public virtual ICollection<Discharge> Discharges { get; set; }
        public virtual ICollection<PatientAdmission> PatientAdmissions { get; set; }
        public  virtual ICollection<Treatment> Treatments { get; set; }
        public virtual ICollection<DoctorVisit> DoctorVisits { get; set; }
        public virtual ICollection<Prescription> Prescriptions { get; set; }
        public virtual ICollection<VitalSigns> VitalSigns { get; set; }
    }
}