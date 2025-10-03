using System.ComponentModel.DataAnnotations;

namespace Wellness_Wardens_Project.ViewModels.Account
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
