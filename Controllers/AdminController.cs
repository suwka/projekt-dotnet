using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Panel()
        {
            var allUsers = _userManager.Users.ToList();
            var mechanics = new List<IdentityUser>();
            var receptionists = new List<IdentityUser>();
            var admins = new List<IdentityUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Mechanik"))
                    mechanics.Add(user);
                if (roles.Contains("Recepcjonista"))
                    receptionists.Add(user);
                if (roles.Contains("Admin"))
                    admins.Add(user);
            }
            return View(Tuple.Create(allUsers, mechanics, receptionists, admins));
        }

        [HttpGet]
        public IActionResult RegisterUser()
        {
            return View(new RegisterUserViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Użytkownik z tym adresem email już istnieje.");
                return View(model);
            }

            // Login (UserName) ustawiamy jako email
            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(user, model.Role);
                ViewBag.Message = $"Użytkownik {model.Email} został utworzony i przypisany do roli {model.Role}.";
                return View(new RegisterUserViewModel());
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
    }
}
