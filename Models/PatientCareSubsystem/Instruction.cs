using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;

namespace Wellness_Wardens_Project.Models.PatientCareSubsystem
{
    public class Instruction
    {
        [Key]
        public int InstructionId { get; set; }

        [Required]
        [ForeignKey(nameof(Doctor))]
        public string DoctorId { get; set; }
        public virtual Employee Doctor { get; set; }

        [ForeignKey(nameof(Patient))]
        public int? PatientId { get; set; }
        public virtual Patient Patient { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; } 

        [Required]
        public string Priority { get; set; } = "Normal"; 

        [Required]
        public string InstructionType { get; set; } = "General";

        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedDate { get; set; }

        [ForeignKey(nameof(CompletedBy))]
        public string? CompletedById { get; set; }
        public virtual Employee? CompletedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}