using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class VisitNote
    {
        [Key]
        public int VisitNoteId { get; set; }

        [Required]
        [ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        [Required]
        [ForeignKey(nameof(Doctor))]
        public string DoctorId { get; set; }
        public virtual Employee Doctor { get; set; }

        [Required]
        public DateTime VisitDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(500)]
        public string Subjective { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Objective { get; set; } = string.Empty; 

        [Required]
        [StringLength(500)]
        public string Assessment { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Plan { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}