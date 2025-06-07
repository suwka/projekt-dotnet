using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WorkshopManager.Data;
using WorkshopManager.Models;
using System.Linq;
using System.Threading.Tasks; // Dodano dla operacji asynchronicznych
using Microsoft.EntityFrameworkCore; // Dodano dla ToListAsync i FirstOrDefaultAsync

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

        public async Task<IActionResult> Panel() // Zmieniono na async Task
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.ServiceOrders
                .Where(o => o.AssignedMechanicId == userId)
                .Include(o => o.Vehicle) // Dodajemy Vehicle, aby móc wyświetlić np. markę/model
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(); // Zmieniono na ToListAsync
            return View(orders);
        }

        public async Task<IActionResult> Details(int id) // Zmieniono na async Task
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.ServiceOrders
                .Where(o => o.Id == id && o.AssignedMechanicId == userId)
                .Select(o => new {
                    ServiceOrder = o,
                    Vehicle = o.Vehicle,
                    Customer = o.Vehicle.Customer
                })
                .FirstOrDefaultAsync(); // Zmieniono na FirstOrDefaultAsync

            if (order == null)
            {
                return NotFound();
            }

            // Przekazujemy wszystko przez ViewBag dla prostoty
            ViewBag.Vehicle = order.Vehicle;
            ViewBag.Customer = order.Customer;
            return View(order.ServiceOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartOrder(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.ServiceOrders
                                .FirstOrDefaultAsync(o => o.Id == id && o.AssignedMechanicId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == ServiceOrderStatus.Nowe)
            {
                order.Status = ServiceOrderStatus.WTrakcie;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Rozpoczęto pracę nad zleceniem.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nie można rozpocząć pracy nad tym zleceniem (nieprawidłowy status).";
            }

            return RedirectToAction(nameof(Panel));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.ServiceOrders
                                .FirstOrDefaultAsync(o => o.Id == id && o.AssignedMechanicId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == ServiceOrderStatus.WTrakcie)
            {
                order.Status = ServiceOrderStatus.Zakonczone;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zakończono pracę nad zleceniem.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nie można zakończyć pracy nad tym zleceniem (nieprawidłowy status).";
            }

            return RedirectToAction(nameof(Panel));
        }
    }
}
