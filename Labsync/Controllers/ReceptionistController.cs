using Microsoft.AspNetCore.Mvc;

namespace LabSync.Controllers
{
    public class ReceptionistController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Patients");
        }
    }
}