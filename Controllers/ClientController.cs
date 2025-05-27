using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Klient")]
    public class ClientController : Controller
    {
        public IActionResult Panel()
        {
            return View();
        }
    }
}

