using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionistController : Controller
    {
        public IActionResult Panel()
        {
            return View();
        }
    }
}

