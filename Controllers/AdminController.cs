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

        public async Task<IActionResult> Panel(string role = "Wszyscy")
        {
            var allUsers = _userManager.Users.ToList();
            var filteredUsers = new List<IdentityUser>();
            if (role == "Wszyscy")
            {
                filteredUsers = allUsers;
            }
            else
            {
                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains(role))
                        filteredUsers.Add(user);
                }
            }
            ViewBag.SelectedRole = role;
            ViewBag.Roles = new List<string> { "Wszyscy", "Admin", "Mechanik", "Recepcjonista", "Klient" };
            return View(filteredUsers);
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

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Panel");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel // Zmieniono na EditUserViewModel
            {
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? ""
            };
            ViewBag.UserId = user.Id;
            ViewBag.Roles = new List<string> { "Admin", "Mechanik", "Recepcjonista", "Klient" };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model) // Zmieniono na EditUserViewModel
        {
            if (!ModelState.IsValid)
            {
                ViewBag.UserId = id;
                ViewBag.Roles = new List<string> { "Admin", "Mechanik", "Recepcjonista", "Klient" };
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.UserId = id;
                ViewBag.Roles = new List<string> { "Admin", "Mechanik", "Recepcjonista", "Klient" };
                return View(model);
            }
            user.Email = model.Email;
            user.UserName = model.Email; // UserName nadal jest ustawiany jako Email
            await _userManager.UpdateAsync(user);
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }
            await _userManager.AddToRoleAsync(user, model.Role);
            return RedirectToAction("Panel");
        }
    }
}
