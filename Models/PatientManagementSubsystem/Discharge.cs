using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class Discharge
    {
        [Key]
        public int DischargeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [RegularExpression(@"^\d{4}/\d{2}/\d{2}$")]
        public DateTime DischargeDate { get; set; }

        public string Description { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;


        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }

        public virtual Patient Patient { get; set; }

        [ForeignKey(nameof(PatientAdmission))]
        public int AdmissionId { get; set; }

        public virtual PatientAdmission PatientAdmission { get; set; }
    }
}
