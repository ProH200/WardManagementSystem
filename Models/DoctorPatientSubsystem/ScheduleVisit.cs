using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.DoctorPatientSubsystem
{
    public class ScheduleVisit
    {
        [Key]
        public int ScheduleVisitId { get; set; }

        [Required(ErrorMessage = "Event title is required")]
        public string Title { get; set; } = "Scheduled Visit";

        [Required(ErrorMessage = "Visit date is required")]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0); 

        [Required(ErrorMessage = "End time is required")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; } = new TimeSpan(10, 0, 0); 

        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }

        public virtual Employee Employee { get; set; }
    }
}