using Microsoft.AspNetCore.Mvc;

namespace Wellness_Wardens_Project.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult Help()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Support()
        {
            return View();
        }
    }
}
