using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Bed
    {
        [Key]
        public int BedId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(20)]
        public string BedNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty; // e.g., Occupied, Available

        public bool IsDeleted { get; set; } = false;

        // Foreign key
        [ForeignKey("Room")]
        public int? RoomId { get; set; }

        // Navigation property
        public virtual Room Room { get; set; }
        public virtual ICollection<PatientAdmission> PatientAdmissions { get; set; }
        public ICollection<PatientMovement> PatientMovements { get; set; }
    }
}
