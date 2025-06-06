using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WorkshopManager.Data;
using WorkshopManager.Models;
using System.Linq;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Mechanik")]
    public class MechanicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MechanicController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Panel()
        {
            var userId = _userManager.GetUserId(User);
            var orders = _context.ServiceOrders
                .Where(o => o.AssignedMechanicId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
            return View(orders);
        }

        public IActionResult Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = _context.ServiceOrders
                .Where(o => o.Id == id && o.AssignedMechanicId == userId)
                .Select(o => new {
                    ServiceOrder = o,
                    Vehicle = o.Vehicle,
                    Customer = o.Vehicle.Customer
                })
                .FirstOrDefault();

            if (order == null)
            {
                return NotFound();
            }

            // Przekazujemy wszystko przez ViewBag dla prostoty
            ViewBag.Vehicle = order.Vehicle;
            ViewBag.Customer = order.Customer;
            return View(order.ServiceOrder);
        }
    }
}
