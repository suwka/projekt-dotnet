using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Mechanic")]
    public class MechanicController : Controller
    {
        public IActionResult Panel()
        {
            return View();
        }
    }
}

