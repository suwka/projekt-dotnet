using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public LoginController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    // Sprawdź, czy użytkownik jest adminem
                    var user = await _signInManager.UserManager.FindByEmailAsync(model.Email);
                    if (await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Panel", "Admin");
                    else if (await _signInManager.UserManager.IsInRoleAsync(user, "Mechanic"))
                        return RedirectToAction("Panel", "Mechanic");
                    else if (await _signInManager.UserManager.IsInRoleAsync(user, "Receptionist"))
                        return RedirectToAction("Panel", "Receptionist");
                    else
                        return RedirectToAction("Panel", "Client");
                }
                ModelState.AddModelError(string.Empty, "Nieprawidłowy login lub hasło.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}

