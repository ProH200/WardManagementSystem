using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class PatientMovement
    {
        [Key]
        public int MovementId { get; set; }

        public string FromLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        public string ToLocation { get; set; }  = string.Empty;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public string Reason { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public int? BedId { get; set; }
        public Bed Bed { get; set; }


        [ForeignKey(nameof(PatientAdmission))]
        public int? AdmissionId { get; set; }
        public virtual PatientAdmission PatientAdmission { get; set; }


    }
}
