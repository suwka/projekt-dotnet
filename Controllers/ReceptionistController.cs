using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Dodano using
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using System.Linq; // Dodano using
using System.Threading.Tasks; // Dodano using

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Recepcjonista")]
    public class ReceptionistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; // Dodano UserManager
        private readonly RoleManager<IdentityRole> _roleManager; // Dodano RoleManager

        public ReceptionistController(ApplicationDbContext context, 
                                    UserManager<IdentityUser> userManager, 
                                    RoleManager<IdentityRole> roleManager) // Zaktualizowano konstruktor
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Panel()
        {
            var serviceOrders = await _context.ServiceOrders
                                        .Include(so => so.Vehicle)
                                            .ThenInclude(v => v.Customer)
                                        .OrderByDescending(so => so.CreatedAt) // Sortowanie od najnowszych
                                        .ToListAsync();
            ViewBag.ShowClientManagement = true; // Dodano, aby pokazać opcje zarządzania klientami
            return View(serviceOrders);
        }

        // GET: Receptionist/ListClients
        public async Task<IActionResult> ListClients(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var clientsQuery = _context.Customers.Include(c => c.IdentityUser).AsQueryable(); // Dodano Include i AsQueryable

            if (!String.IsNullOrEmpty(searchString))
            {
                clientsQuery = clientsQuery.Where(c => c.LastName.Contains(searchString) || 
                                                       c.FirstName.Contains(searchString) || 
                                                       c.Phone.Contains(searchString) || 
                                                       (c.IdentityUser != null && c.IdentityUser.Email.Contains(searchString))); // Sprawdzenie null dla IdentityUser
            }

            var clients = await clientsQuery.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(); // Dodano sortowanie
            return View(clients);
        }

        // GET: Receptionist/CreateClient
        public IActionResult CreateClient()
        {
            return View(new CreateClientViewModel());
        }

        // POST: Receptionist/CreateClient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClient(CreateClientViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Użytkownik z tym adresem email już istnieje.");
                    return View(model);
                }

                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true }; // EmailConfirmed = true dla uproszczenia
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Przypisanie roli "Klient"
                    string roleName = "Klient";
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                    await _userManager.AddToRoleAsync(user, roleName);

                    // Utworzenie rekordu Customer
                    var customer = new Customer
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Phone = model.PhoneNumber,
                        IdentityUserId = user.Id,
                        IdentityUser = user // Powiązanie IdentityUser
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();

                    // Przekierowanie do listy klientów
                    TempData["SuccessMessage"] = "Pomyślnie utworzono nowego klienta.";
                    return RedirectToAction(nameof(ListClients)); 
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Receptionist/ClientDetails/{id}
        public async Task<IActionResult> ClientDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.IdentityUser) // Dołączenie danych użytkownika (email)
                .Include(c => c.Vehicles) // Dołączenie pojazdów klienta
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

    }
}
