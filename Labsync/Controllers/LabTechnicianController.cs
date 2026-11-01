using Microsoft.AspNetCore.Mvc;

namespace LabSync.Controllers
{
    public class LabTechnicianController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}