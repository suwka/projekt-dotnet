using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkshopManager.Data;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Klient")]
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<VehicleController> _logger;

        public VehicleController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager,
            ILogger<VehicleController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Add()
        {
            // Przekazujemy pusty ViewBag zamiast modelu
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(IFormCollection form)
        {
            // Pobierz wartości bezpośrednio z formularza
            var brand = form["Brand"].ToString();
            var model = form["Model"].ToString();
            var vin = form["Vin"].ToString();
            var registrationNumber = form["RegistrationNumber"].ToString();
            var yearString = form["Year"].ToString();
            var imageUrl = form["ImageUrl"].ToString();

            // Walidacja formularza ręcznie
            bool isValid = true;
            var errorList = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(brand))
            {
                ModelState.AddModelError("Brand", "Marka jest wymagana");
                errorList["Brand"] = "Marka jest wymagana";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                ModelState.AddModelError("Model", "Model jest wymagany");
                errorList["Model"] = "Model jest wymagany";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            {
                ModelState.AddModelError("Vin", "VIN jest wymagany i musi mieć 17 znaków");
                errorList["Vin"] = "VIN jest wymagany i musi mieć 17 znaków";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(registrationNumber))
            {
                ModelState.AddModelError("RegistrationNumber", "Numer rejestracyjny jest wymagany");
                errorList["RegistrationNumber"] = "Numer rejestracyjny jest wymagany";
                isValid = false;
            }

            int year = 0;
            if (string.IsNullOrWhiteSpace(yearString) || !int.TryParse(yearString, out year))
            {
                ModelState.AddModelError("Year", "Rok jest wymagany");
                errorList["Year"] = "Rok jest wymagany";
                isValid = false;
            }

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                ViewData["ImageUrl"] = imageUrl;
            }
            else
            {
                imageUrl = "https://via.placeholder.com/150";
            }

            // Zapisujemy dane formularza do ViewData, aby zachować je w przypadku błędów
            ViewData["Brand"] = brand;
            ViewData["Model"] = model;
            ViewData["Vin"] = vin;
            ViewData["RegistrationNumber"] = registrationNumber;
            ViewData["Year"] = yearString;
            ViewData["ImageUrl"] = imageUrl;
            ViewData["Errors"] = errorList;

            if (!isValid)
            {
                return View();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ModelState.AddModelError("", "Nie można zidentyfikować użytkownika.");
                    return View();
                }
                
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);
                if (customer == null)
                {
                    ModelState.AddModelError("", "Nie można znaleźć powiązanego profilu klienta.");
                    return View();
                }
                
                var vehicle = new Vehicle
                {
                    Brand = brand,
                    Model = model,
                    Vin = vin,
                    RegistrationNumber = registrationNumber,
                    Year = year,
                    ImageUrl = string.IsNullOrEmpty(imageUrl) ? "https://via.placeholder.com/150" : imageUrl,
                    CustomerId = customer.Id
                };

                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Pomyślnie dodano pojazd.";
                return RedirectToAction("Panel", "Client");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił nieoczekiwany błąd: {ex.Message}");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var customer = await _context.Customers.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);
            if (customer == null) return NotFound("Nie znaleziono profilu klienta.");

            return View(customer.Vehicles ?? new List<Vehicle>());
        }
    }
}
