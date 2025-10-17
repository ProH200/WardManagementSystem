using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wellness_Wardens_Project.Models.AdministrationSubsystem
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(50)]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(50)]
        public string RoomType { get; set; } 

        public bool IsDeleted { get; set; } = false;

        [ForeignKey("Ward")]
        [Required(ErrorMessage = "Please select a ward.")]
        public int WardId { get; set; }

        public virtual Ward Ward { get; set; }

        public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
    }
}
