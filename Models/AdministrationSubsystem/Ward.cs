using System.ComponentModel.DataAnnotations;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Ward
    {
        [Key]
        public int WardId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual ICollection<Consumable> Consumables { get; set; }
        public virtual ICollection<ConsumablesRequest> ConsumableRequests { get; set; }
    }
}
