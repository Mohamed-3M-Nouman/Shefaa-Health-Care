using Microsoft.AspNetCore.Mvc;

namespace ShefaaHealthCare.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
