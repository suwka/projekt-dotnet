using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Dodaj do roli Klient
                    await _userManager.AddToRoleAsync(user, "Klient");

                    // Dodaj klienta do bazy
                    using (var scope = HttpContext.RequestServices.CreateScope())
                    {
                        var dbContext = (WorkshopManager.Data.ApplicationDbContext)scope.ServiceProvider.GetService(typeof(WorkshopManager.Data.ApplicationDbContext));
                        var customer = new WorkshopManager.Models.Customer
                        {
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Phone = model.Phone,
                            IdentityUserId = user.Id
                        };
                        dbContext.Customers.Add(customer);
                        dbContext.SaveChanges();
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Panel", "Client");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
    }
}

