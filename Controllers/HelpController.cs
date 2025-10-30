using Microsoft.AspNetCore.Mvc;

namespace Wellness_Wardens_Project.Controllers
{
    [Route("grp-03-20/Help")]
    public class HelpController : Controller
    {
        [Route("Help")]
        public IActionResult Help()
        {
            return View();
        }9

        [Route("FAQ")]
        public IActionResult FAQ()
        {
            return View();
        }

        [Route("Support")]
        public IActionResult Support()
        {
            return View();
        }
    }
}
