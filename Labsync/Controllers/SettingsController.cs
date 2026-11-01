using Microsoft.AspNetCore.Mvc;

namespace Labsync.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
