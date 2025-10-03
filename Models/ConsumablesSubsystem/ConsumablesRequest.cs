using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;

namespace Wellness_Wardens_Project.Models.ConsumablesSubsystem
{
    public class ConsumablesRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public string ConsumableName { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        public int QuantityRequested { get; set; }


        [Required(ErrorMessage = "This field is required")]
        [DataType(DataType.Date)]
        [RegularExpression(@"^\d{4}/\d{2}/\d{2}$")]
        public DateTime RequestedDate { get; set; }

        [Required(ErrorMessage = "This field is required")]
        public bool IsDelivered { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
        [ForeignKey(nameof(Ward))]
        public int? WardId { get; set; }
        public virtual Ward Ward { get; set; }

        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }

        public virtual Employee Employee { get; set; }
        public virtual ICollection<Consumable> Consumables { get; set; }
    }
}
