using System.ComponentModel.DataAnnotations;

namespace Wellness_Wardens_Project.Models.PatientManagementSubsystem
{
    public class PatientMedicalHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int? PatientId { get; set; }
        public Patient Patient { get; set; }

        [Required]
        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public string? EmployeeId { get; set; } 
    }
}