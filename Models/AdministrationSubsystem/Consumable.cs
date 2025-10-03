using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Consumable
    {
        [Key]
        public int ConsumableId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "This field is required.")]
        public int QuantityAvailable { get; set; }

        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(Employee))]
        public string? EmployeeId { get; set; }

        [ForeignKey(nameof(ConsumablesRequest))]
        public int? RequestId { get; set; }

        [ForeignKey(nameof(Ward))]
        public int? WardId { get; set; }

        public virtual Employee Employee { get; set; }  
        public virtual ConsumablesRequest ConsumablesRequest { get; set; }
        public virtual Ward Ward { get; set; }
    }
}
