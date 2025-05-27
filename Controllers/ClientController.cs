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

            // Pobierz pojazdy klienta
            var allVehicles = _context.Vehicles.Where(v => v.CustomerId == customer.Id).ToList();
            // Pobierz aktywne zlecenia serwisowe (Nowe lub WTrakcie)
            var activeOrders = _context.ServiceOrders
                .Where(so => (so.Status == ServiceOrderStatus.Nowe || so.Status == ServiceOrderStatus.WTrakcie)
                    && allVehicles.Select(v => v.Id).Contains(so.VehicleId))
                .ToList();
            // Pojazdy z aktywnym zleceniem
            var vehiclesWithActiveOrder = allVehicles.Where(v => activeOrders.Any(so => so.VehicleId == v.Id)).ToList();
            // Pojazdy bez aktywnego zlecenia
            var vehiclesWithoutActiveOrder = allVehicles.Where(v => !vehiclesWithActiveOrder.Contains(v)).ToList();

            ViewBag.VehiclesWithActiveOrder = vehiclesWithActiveOrder;
            ViewBag.ActiveOrders = activeOrders;
            return View(vehiclesWithoutActiveOrder);
        }
    }
}

