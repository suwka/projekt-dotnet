using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WorkshopManager.Data;
using WorkshopManager.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Klient")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClientController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Panel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Index", "Home");
            var customer = _context.Customers.FirstOrDefault(c => c.IdentityUserId == user.Id);
            if (customer == null)
                return RedirectToAction("Index", "Home");
            ViewBag.FirstName = customer.FirstName;
            ViewBag.LastName = customer.LastName;
            var vehicles = _context.Vehicles.Where(v => v.CustomerId == customer.Id).ToList();
            return View(vehicles);
        }
    }
}

