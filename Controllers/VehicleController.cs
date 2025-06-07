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
    [Authorize(Roles = "Klient, Recepcjonista")]
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
        public async Task<IActionResult> Add(int? customerId = null)
        {
            // Jeśli przekazano ID klienta, pobierz dane klienta
            if (customerId.HasValue)
            {
                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    ViewBag.CustomerId = customer.Id;
                    ViewBag.CustomerName = $"{customer.FirstName} {customer.LastName}";
                }
            }
            
            // Dla recepcjonisty, jeśli nie podano customerId, wyświetl listę klientów do wyboru
            if (User.IsInRole("Recepcjonista") && !customerId.HasValue)
            {
                var customers = await _context.Customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
                ViewBag.Customers = customers;
            }
            
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
            var customerIdString = form["CustomerId"].ToString();
            
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
                // Jeśli jest recepcjonista, pobierz listę klientów
                if (User.IsInRole("Recepcjonista"))
                {
                    if (!string.IsNullOrEmpty(customerIdString) && int.TryParse(customerIdString, out int customerId))
                    {
                        var customer = await _context.Customers.FindAsync(customerId);
                        if (customer != null)
                        {
                            ViewBag.CustomerId = customer.Id;
                            ViewBag.CustomerName = $"{customer.FirstName} {customer.LastName}";
                        }
                    }
                    else
                    {
                        var customers = await _context.Customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
                        ViewBag.Customers = customers;
                    }
                }
                return View();
            }

            try
            {
                Customer customer = null;
                
                // Sprawdź, czy działa recepcjonista czy klient
                if (User.IsInRole("Recepcjonista"))
                {
                    // Recepcjonista dodaje pojazd dla konkretnego klienta
                    if (!string.IsNullOrEmpty(customerIdString) && int.TryParse(customerIdString, out int customerId))
                    {
                        customer = await _context.Customers.FindAsync(customerId);
                        if (customer == null)
                        {
                            ModelState.AddModelError("", "Nie można znaleźć wybranego klienta.");
                            var customers = await _context.Customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
                            ViewBag.Customers = customers;
                            return View();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Musisz wybrać klienta.");
                        var customers = await _context.Customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
                        ViewBag.Customers = customers;
                        return View();
                    }
                }
                else
                {
                    // Klient dodaje pojazd do własnego konta
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        ModelState.AddModelError("", "Nie można zidentyfikować użytkownika.");
                        return View();
                    }
                    
                    customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);
                    if (customer == null)
                    {
                        ModelState.AddModelError("", "Nie można znaleźć powiązanego profilu klienta.");
                        return View();
                    }
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
                
                // Przekieruj w zależności od roli
                if (User.IsInRole("Recepcjonista"))
                {
                    return RedirectToAction("ClientDetails", "Receptionist", new { id = customer.Id });
                }
                else
                {
                    return RedirectToAction("Panel", "Client");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Wystąpił nieoczekiwany błąd: {ex.Message}");
                
                // Jeśli jest recepcjonista, pobierz listę klientów
                if (User.IsInRole("Recepcjonista"))
                {
                    var customers = await _context.Customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
                    ViewBag.Customers = customers;
                }
                
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _context.Vehicles.Include(v => v.Customer).FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            // Sprawdzenie uprawnień
            if (User.IsInRole("Klient"))
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);
                if (vehicle.CustomerId != customer.Id)
                {
                    return Forbid(); // Klient próbuje edytować nie swój pojazd
                }
            }
            
            if (vehicle.Customer != null)
            {
                ViewBag.CustomerName = $"{vehicle.Customer.FirstName} {vehicle.Customer.LastName}";
            }

            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormCollection form)
        {
            var vehicleToUpdate = await _context.Vehicles.Include(v => v.Customer).FirstOrDefaultAsync(v => v.Id == id);

            if (vehicleToUpdate == null)
            {
                return NotFound();
            }

            // Sprawdzenie uprawnień
            if (User.IsInRole("Klient"))
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);
                if (vehicleToUpdate.CustomerId != customer.Id)
                {
                    return Forbid();
                }
            }

            var brand = form["Brand"].ToString();
            var model = form["Model"].ToString();
            var vin = form["Vin"].ToString();
            var registrationNumber = form["RegistrationNumber"].ToString();
            var yearString = form["Year"].ToString();
            var imageUrl = form["ImageUrl"].ToString();
            
            var errorList = new Dictionary<string, string>();
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(brand))
            {
                errorList["Brand"] = "Marka jest wymagana";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(model))
            {
                errorList["Model"] = "Model jest wymagany";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
            {
                errorList["Vin"] = "VIN jest wymagany i musi mieć 17 znaków";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(registrationNumber))
            {
                errorList["RegistrationNumber"] = "Numer rejestracyjny jest wymagany";
                isValid = false;
            }
            int year = 0;
            if (string.IsNullOrWhiteSpace(yearString) || !int.TryParse(yearString, out year))
            {
                errorList["Year"] = "Rok jest wymagany";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                imageUrl = "https://via.placeholder.com/150";
            }

            ViewData["Errors"] = errorList;

            if (!isValid)
            {
                // Przekazanie danych z powrotem do widoku w przypadku błędu
                vehicleToUpdate.Brand = brand;
                vehicleToUpdate.Model = model;
                vehicleToUpdate.Vin = vin;
                vehicleToUpdate.RegistrationNumber = registrationNumber;
                if(int.TryParse(yearString, out int parsedYear)) vehicleToUpdate.Year = parsedYear;
                vehicleToUpdate.ImageUrl = imageUrl;
                if (vehicleToUpdate.Customer != null)
                {
                    ViewBag.CustomerName = $"{vehicleToUpdate.Customer.FirstName} {vehicleToUpdate.Customer.LastName}";
                }
                return View(vehicleToUpdate);
            }

            vehicleToUpdate.Brand = brand;
            vehicleToUpdate.Model = model;
            vehicleToUpdate.Vin = vin;
            vehicleToUpdate.RegistrationNumber = registrationNumber;
            vehicleToUpdate.Year = year;
            vehicleToUpdate.ImageUrl = imageUrl;

            if (ModelState.IsValid) // Dodatkowa walidacja modelu, jeśli używasz atrybutów
            {
                try
                {
                    _context.Update(vehicleToUpdate);
                    await _context.SaveChangesAsync();

                    if (User.IsInRole("Recepcjonista"))
                    {
                        return RedirectToAction("ClientDetails", "Receptionist", new { id = vehicleToUpdate.CustomerId });
                    }
                    return RedirectToAction("Panel", "Client");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VehicleExists(vehicleToUpdate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating vehicle");
                    ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji pojazdu.");
                }
            }
            
            // Jeśli ModelState nie jest ważny lub wystąpił inny błąd, wróć do widoku edycji
            if (vehicleToUpdate.Customer != null)
            {
                ViewBag.CustomerName = $"{vehicleToUpdate.Customer.FirstName} {vehicleToUpdate.Customer.LastName}";
            }
            return View(vehicleToUpdate);
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}
