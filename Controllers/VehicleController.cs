using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

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
            var vehiclePhoto = Request.Form.Files["VehiclePhoto"];
            
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

            // Sprawdzenie przesłanego zdjęcia
            if (vehiclePhoto != null && vehiclePhoto.Length > 0)
            {
                // Sprawdzenie rozmiaru pliku
                if (vehiclePhoto.Length > MaxFileSize)
                {
                    ModelState.AddModelError("VehiclePhoto", "Plik jest za duży. Maksymalny rozmiar to 5MB.");
                    errorList["VehiclePhoto"] = "Plik jest za duży. Maksymalny rozmiar to 5MB.";
                    isValid = false;
                }

                // Sprawdzenie rozszerzenia pliku
                var fileExtension = Path.GetExtension(vehiclePhoto.FileName).ToLower();
                if (!_allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("VehiclePhoto", "Niedozwolony format pliku. Akceptowane formaty: jpg, jpeg, png, gif.");
                    errorList["VehiclePhoto"] = "Niedozwolony format pliku. Akceptowane formaty: jpg, jpeg, png, gif.";
                    isValid = false;
                }

                // Jeśli przesłano zdjęcie, będziemy używać go zamiast domyślnego URL
                imageUrl = null;
            }
            else if (!string.IsNullOrWhiteSpace(imageUrl))
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
                // Określ ID klienta do którego należy pojazd
                int customerId;
                if (User.IsInRole("Recepcjonista"))
                {
                    if (!int.TryParse(customerIdString, out customerId) || customerId <= 0)
                    {
                        ModelState.AddModelError("CustomerId", "Wybierz klienta");
                        errorList["CustomerId"] = "Wybierz klienta";
                        var customers = await _context.Customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
                        ViewBag.Customers = customers;
                        return View();
                    }
                }
                else
                {
                    // Jeśli użytkownik jest klientem, pobierz jego ID
                    var userId = _userManager.GetUserId(User);
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);
                    
                    if (customer == null)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    
                    customerId = customer.Id;
                }
                
                // Przetwarzanie przesłanego zdjęcia
                string photoPath = null;
                if (vehiclePhoto != null && vehiclePhoto.Length > 0)
                {
                    string uploadsFolder = Path.Combine("wwwroot", "uploads", "vehicles");
                    string uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(vehiclePhoto.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await vehiclePhoto.CopyToAsync(fileStream);
                    }
                    
                    // Relatywna ścieżka do przechowania w bazie danych
                    photoPath = $"/uploads/vehicles/{uniqueFileName}";
                }

                // Tworzenie nowego pojazdu
                var vehicle = new Vehicle
                {
                    Brand = brand,
                    Model = model,
                    Vin = vin,
                    RegistrationNumber = registrationNumber,
                    Year = year,
                    CustomerId = customerId,
                    // Używamy ścieżki do zdjęcia, jeśli zostało przesłane, w przeciwnym razie używamy domyślnego URL
                    ImageUrl = photoPath ?? imageUrl
                };

                // Dodanie pojazdu do bazy danych
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();
                
                // Przekierowanie w zależności od roli
                if (User.IsInRole("Recepcjonista"))
                {
                    return RedirectToAction("ListClients", "Receptionist");
                }
                else
                {
                    return RedirectToAction("Panel", "Client");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania pojazdu");
                ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania danych. Spróbuj ponownie.");
                
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
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Customer)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            // Sprawdź, czy użytkownik ma uprawnienia do edycji tego pojazdu
            if (!User.IsInRole("Recepcjonista"))
            {
                // Jeśli nie jest recepcjonistą, sprawdź czy pojazd należy do tego klienta
                var userId = _userManager.GetUserId(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);
                
                if (customer == null || vehicle.CustomerId != customer.Id)
                {
                    return Forbid();
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
        public async Task<IActionResult> Edit(int id, Vehicle vehicle, IFormFile VehiclePhoto)
        {
            if (id != vehicle.Id)
            {
                return NotFound();
            }

            // Sprawdź, czy użytkownik ma uprawnienia do edycji tego pojazdu
            if (!User.IsInRole("Recepcjonista"))
            {
                // Jeśli nie jest recepcjonistą, sprawdź czy pojazd należy do tego klienta
                var userId = _userManager.GetUserId(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);
                
                if (customer == null || vehicle.CustomerId != customer.Id)
                {
                    return Forbid();
                }
            }

            // Pobierz oryginalny pojazd z bazy, aby zachować dane, które nie są edytowane
            var existingVehicle = await _context.Vehicles
                .Include(v => v.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);

            if (existingVehicle == null)
            {
                return NotFound();
            }

            // Walidacja ręczna
            var isValid = true;
            var errorList = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(vehicle.Brand))
            {
                ModelState.AddModelError("Brand", "Marka jest wymagana");
                errorList["Brand"] = "Marka jest wymagana";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(vehicle.Model))
            {
                ModelState.AddModelError("Model", "Model jest wymagany");
                errorList["Model"] = "Model jest wymagany";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(vehicle.Vin) || vehicle.Vin.Length != 17)
            {
                ModelState.AddModelError("Vin", "VIN jest wymagany i musi mieć 17 znaków");
                errorList["Vin"] = "VIN jest wymagany i musi mieć 17 znaków";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(vehicle.RegistrationNumber))
            {
                ModelState.AddModelError("RegistrationNumber", "Numer rejestracyjny jest wymagany");
                errorList["RegistrationNumber"] = "Numer rejestracyjny jest wymagany";
                isValid = false;
            }

            if (vehicle.Year <= 0)
            {
                ModelState.AddModelError("Year", "Rok jest wymagany");
                errorList["Year"] = "Rok jest wymagany i musi być większy od zera";
                isValid = false;
            }

            // Sprawdzenie przesłanego zdjęcia
            if (VehiclePhoto != null && VehiclePhoto.Length > 0)
            {
                // Sprawdzenie rozmiaru pliku
                if (VehiclePhoto.Length > MaxFileSize)
                {
                    ModelState.AddModelError("VehiclePhoto", "Plik jest za duży. Maksymalny rozmiar to 5MB.");
                    errorList["VehiclePhoto"] = "Plik jest za duży. Maksymalny rozmiar to 5MB.";
                    isValid = false;
                }

                // Sprawdzenie rozszerzenia pliku
                var fileExtension = Path.GetExtension(VehiclePhoto.FileName).ToLower();
                if (!_allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("VehiclePhoto", "Niedozwolony format pliku. Akceptowane formaty: jpg, jpeg, png, gif.");
                    errorList["VehiclePhoto"] = "Niedozwolony format pliku. Akceptowane formaty: jpg, jpeg, png, gif.";
                    isValid = false;
                }
            }

            ViewData["Errors"] = errorList;

            if (!isValid)
            {
                if (existingVehicle.Customer != null)
                {
                    ViewBag.CustomerName = $"{existingVehicle.Customer.FirstName} {existingVehicle.Customer.LastName}";
                }
                return View(vehicle);
            }

            try
            {
                // Przetwarzanie przesłanego zdjęcia
                if (VehiclePhoto != null && VehiclePhoto.Length > 0)
                {
                    string uploadsFolder = Path.Combine("wwwroot", "uploads", "vehicles");
                    string uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(VehiclePhoto.FileName)}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await VehiclePhoto.CopyToAsync(fileStream);
                    }
                    
                    // Jeżeli jest nowe zdjęcie, zaktualizuj ścieżkę
                    vehicle.ImageUrl = $"/uploads/vehicles/{uniqueFileName}";
                }
                else if (string.IsNullOrWhiteSpace(vehicle.ImageUrl))
                {
                    // Jeśli nie ma nowego zdjęcia i nie podano URL, zachowaj istniejący URL
                    vehicle.ImageUrl = existingVehicle.ImageUrl;
                }

                // Aktualizacja pojazdu w bazie danych
                _context.Update(vehicle);
                await _context.SaveChangesAsync();
                
                // Przekierowanie w zależności od roli
                if (User.IsInRole("Recepcjonista"))
                {
                    return RedirectToAction("ClientDetails", "Receptionist", new { id = vehicle.CustomerId });
                }
                else
                {
                    return RedirectToAction("Panel", "Client");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(vehicle.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Błąd aktualizacji pojazdu - konflikt współbieżności");
                    ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania zmian. Pojazd został zmodyfikowany przez kogoś innego.");
                    
                    if (existingVehicle.Customer != null)
                    {
                        ViewBag.CustomerName = $"{existingVehicle.Customer.FirstName} {existingVehicle.Customer.LastName}";
                    }
                    return View(vehicle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas edycji pojazdu");
                ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania danych. Spróbuj ponownie.");
                
                if (existingVehicle.Customer != null)
                {
                    ViewBag.CustomerName = $"{existingVehicle.Customer.FirstName} {existingVehicle.Customer.LastName}";
                }
                return View(vehicle);
            }
        }
        
        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}
