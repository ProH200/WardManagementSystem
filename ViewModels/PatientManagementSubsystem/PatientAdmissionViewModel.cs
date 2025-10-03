using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Wellness_Wardens_Project.ViewModels.PatientManagementSubsystem
{
    public class PatientAdmissionViewModel
    {
        public int? AdmissionId { get; set; }
        public int? PatientId { get; set; }
        public string PatientName { get; set; }

        [Required(ErrorMessage = "Please select a ward.")]
        public int? WardId { get; set; }

        [Required(ErrorMessage = "Please select a room.")]
        public int? RoomId { get; set; }

        [Required(ErrorMessage = "Please select a bed.")]
        public int? BedId { get; set; }

        [Required(ErrorMessage = "Admission date is required.")]
        [DataType(DataType.Date)]
        public DateTime? AdmissionDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string Reason { get; set; }

        // Dropdowns
        public IEnumerable<SelectListItem> Wards { get; set; }
        public IEnumerable<SelectListItem> Rooms { get; set; }
        public IEnumerable<SelectListItem> Beds { get; set; }
        public string? EmployeeId { get; set; }
        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
        public string AssignedEmployeeId { get; set; }
    }
}
