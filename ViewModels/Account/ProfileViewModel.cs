using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Wellness_Wardens_Project.ViewModels.Account
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "Email must be 100 characters or fewer.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 50 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public string Gender { get; set; } = string.Empty;

        public string Specialization { get; set; } // Optional

        public IList<string> Roles { get; set; } = new List<string>();
    }
}
