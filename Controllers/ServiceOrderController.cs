using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Models;
using WorkshopManager.Data;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace WorkshopManager.Controllers
{
    public class ServiceOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ServiceOrderController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /ServiceOrder/Create?vehicleId=5
        [HttpGet]
        public async Task<IActionResult> Create(int vehicleId)
        {
            var serviceOrder = new ServiceOrder
            {
                VehicleId = vehicleId,
                CreatedAt = DateTime.Now,
                Status = WorkshopManager.Models.ServiceOrderStatus.Nowe,
                ServiceTasks = new List<ServiceTask>(),
                Comments = new List<Comment>()
            };

            // Pobieramy listę mechaników do wyboru
            var mechanics = await _userManager.GetUsersInRoleAsync("Mechanik");
            ViewBag.Mechanics = new SelectList(mechanics, "Id", "UserName");

            return View(serviceOrder);
        }

        // POST: /ServiceOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceOrder serviceOrder)
        {
            if (serviceOrder.ServiceTasks == null)
                serviceOrder.ServiceTasks = new List<ServiceTask>();
            if (serviceOrder.Comments == null)
                serviceOrder.Comments = new List<Comment>();
            if (ModelState.IsValid)
            {
                serviceOrder.CreatedAt = DateTime.Now;
                serviceOrder.Status = WorkshopManager.Models.ServiceOrderStatus.Nowe;
                _context.ServiceOrders.Add(serviceOrder);
                await _context.SaveChangesAsync();
                // Sprawdź rolę użytkownika i przekieruj odpowiednio
                if (User.IsInRole("Recepcjonista"))
                {
                    return RedirectToAction("Panel", "Receptionist");
                }
                else if (User.IsInRole("Klient"))
                {
                    return RedirectToAction("Panel", "Client");
                }
                else
                {
                    // domyślne przekierowanie, np. do strony głównej
                    return RedirectToAction("Index", "Home");
                }
            }
            
            // Jeśli model jest nieprawidłowy, ponownie pobieramy dane do list wyboru
            var mechanics = await _userManager.GetUsersInRoleAsync("Mechanik");
            ViewBag.Mechanics = new SelectList(mechanics, "Id", "UserName", serviceOrder.AssignedMechanicId);
            
            return View(serviceOrder);
        }

        // GET: ServiceOrder/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Customer)
                .Include(s => s.AssignedMechanic)
                .Include(s => s.ServiceTasks)
                    .ThenInclude(st => st.UsedParts)
                        .ThenInclude(up => up.Part)
                .Include(s => s.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (serviceOrder == null)
            {
                return NotFound();
            }

            // Najprostsze rozwiązanie: słownik AuthorId => rola (pierwsza rola)
            var commentRoles = new Dictionary<string, string>();
            foreach (var comment in serviceOrder.Comments)
            {
                if (comment.AuthorId != null && !commentRoles.ContainsKey(comment.AuthorId))
                {
                    var user = await _userManager.FindByIdAsync(comment.AuthorId);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        commentRoles[comment.AuthorId] = roles.FirstOrDefault() ?? "Brak roli";
                    }
                    else
                    {
                        commentRoles[comment.AuthorId] = "Brak użytkownika";
                    }
                }
            }
            ViewBag.CommentRoles = commentRoles;

            return View(serviceOrder);
        }

        // GET: ServiceOrder/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Customer)
                .Include(s => s.AssignedMechanic)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (serviceOrder == null)
            {
                return NotFound();
            }

            // Pobieramy listę mechaników do wyboru
            var mechanics = await _userManager.GetUsersInRoleAsync("Mechanik");
            ViewBag.Mechanics = new SelectList(mechanics, "Id", "UserName", serviceOrder.AssignedMechanicId);
            
            // Dodajemy listę wartości enum StatusOrder do wyboru
            ViewBag.Statuses = Enum.GetValues(typeof(ServiceOrderStatus))
                .Cast<ServiceOrderStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = s == serviceOrder.Status
                });

            return View(serviceOrder);
        }

        // POST: ServiceOrder/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Status,AssignedMechanicId,VehicleId,CreatedAt,ClosedAt,ProblemDescription")] ServiceOrder serviceOrder)
        {
            if (id != serviceOrder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Jeśli status zmienił się na Zakonczone, ustawiamy datę zamknięcia
                    if (serviceOrder.Status == ServiceOrderStatus.Zakonczone && !serviceOrder.ClosedAt.HasValue)
                    {
                        serviceOrder.ClosedAt = DateTime.Now;
                    }
                    // Jeśli status zmienił się z Zakonczone, usuwamy datę zamknięcia
                    else if (serviceOrder.Status != ServiceOrderStatus.Zakonczone)
                    {
                        serviceOrder.ClosedAt = null;
                    }

                    _context.Update(serviceOrder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceOrderExists(serviceOrder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Panel", "Receptionist");
            }

            // Jeśli ModelState jest nieprawidłowy, ponownie pobieramy dane do list wyboru
            var mechanics = await _userManager.GetUsersInRoleAsync("Mechanik");
            ViewBag.Mechanics = new SelectList(mechanics, "Id", "UserName", serviceOrder.AssignedMechanicId);
            
            ViewBag.Statuses = Enum.GetValues(typeof(ServiceOrderStatus))
                .Cast<ServiceOrderStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = s == serviceOrder.Status
                });

            return View(serviceOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int orderId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Details", new { id = orderId });
            }
            var user = await _userManager.GetUserAsync(User);
            var comment = new Comment
            {
                Content = content,
                AuthorId = user.Id,
                Timestamp = DateTime.Now,
                ServiceOrderId = orderId
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = orderId });
        }

        [HttpGet]
        public async Task<IActionResult> EditComment(int id)
        {
            var comment = await _context.Comments.Include(c => c.ServiceOrder).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (comment.AuthorId != user.Id) return Forbid();
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int id, string content)
        {
            var comment = await _context.Comments.Include(c => c.ServiceOrder).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (comment.AuthorId != user.Id) return Forbid();
            if (!string.IsNullOrWhiteSpace(content))
            {
                comment.Content = content;
                comment.ModifiedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = comment.ServiceOrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.Include(c => c.ServiceOrder).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (comment.AuthorId != user.Id) return Forbid();
            int orderId = comment.ServiceOrderId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = orderId });
        }

        [HttpGet]
        public async Task<IActionResult> AssignPart(int id, string search)
        {
            var partsQuery = _context.Parts.Where(p => p.Quantity > 0);
            if (!string.IsNullOrWhiteSpace(search))
            {
                partsQuery = partsQuery.Where(p => p.Name.Contains(search) || p.Manufacturer.Contains(search) || p.CatalogNumber.Contains(search));
            }
            var parts = await partsQuery.ToListAsync();

            // Pobierz już przypisane części do tego zlecenia wraz z ilościami
            var usedParts = await _context.ServiceOrders
                .Where(o => o.Id == id)
                .SelectMany(o => o.ServiceTasks)
                .SelectMany(t => t.UsedParts)
                .Include(up => up.Part)
                .Select(up => new { up.PartId, up.Quantity, up.ServiceCost })
                .ToListAsync();
            
            var usedPartIds = usedParts.Select(up => up.PartId).ToList();
            var usedPartQuantities = usedParts.ToDictionary(up => up.PartId, up => up.Quantity);
            var usedPartServiceCosts = usedParts.ToDictionary(up => up.PartId, up => up.ServiceCost);
            
            ViewBag.OrderId = id;
            ViewBag.UsedPartIds = usedPartIds;
            ViewBag.UsedPartQuantities = usedPartQuantities;
            ViewBag.UsedPartServiceCosts = usedPartServiceCosts;
            
            return View(parts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPartToOrder(int orderId)
        {
            // Diagnostyka - sprawdzanie co przychodzi w Request.Form
            System.Diagnostics.Debug.WriteLine($"Form zawiera {Request.Form.Count} pól");
            foreach (var key in Request.Form.Keys)
            {
                System.Diagnostics.Debug.WriteLine($"Key: {key}, Value: {Request.Form[key]}");
            }
            
            // Pobierz wybrane części z Request.Form i bezpiecznie sparsuj do listy int
            var selectedParts = new List<int>();
            if (Request.Form["selectedParts"].Count > 0)
            {
                foreach (var val in Request.Form["selectedParts"])
                {
                    if (int.TryParse(val, out int id))
                    {
                        selectedParts.Add(id);
                        System.Diagnostics.Debug.WriteLine($"Dodano selectedPart: {id}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Liczba wybranych części: {selectedParts.Count}");

            // Parsowanie quantities
            var quantities = new Dictionary<int, int>();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("quantities[") && key.EndsWith("]"))
                {
                    // Wyciągnij tylko numer ID z nazwy pola
                    var partIdStr = key.Substring(11, key.Length - 12);
                    if (int.TryParse(partIdStr, out int partId))
                    {
                        if (int.TryParse(Request.Form[key], out int qty))
                        {
                            quantities[partId] = qty;
                            System.Diagnostics.Debug.WriteLine($"Dodano quantity dla partId: {partId}, value: {qty}");
                        }
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Liczba quantities: {quantities.Count}");

            // Parsowanie serviceCosts (jeśli są używane w formularzu)
            var serviceCosts = new Dictionary<int, decimal>();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("serviceCosts[") && key.EndsWith("]"))
                {
                    var partIdStr = key.Substring(13, key.Length - 14);
                    if (int.TryParse(partIdStr, out int partId))
                    {
                        if (decimal.TryParse(Request.Form[key], out decimal cost))
                        {
                            serviceCosts[partId] = cost;
                        }
                    }
                }
            }

            // Pobierz zlecenie wraz z czynnościami serwisowymi i użytymi częściami
            var serviceOrder = await _context.ServiceOrders
                .Include(o => o.ServiceTasks)
                    .ThenInclude(t => t.UsedParts)
                        .ThenInclude(up => up.Part)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            
            if (serviceOrder == null)
                return NotFound();

            var serviceTask = serviceOrder.ServiceTasks.FirstOrDefault();
            if (serviceTask == null)
            {
                serviceTask = new ServiceTask
                {
                    Description = "Części przypisane do zlecenia",
                    LaborCost = 0,
                    ServiceOrderId = orderId,
                    UsedParts = new List<UsedPart>()
                };
                _context.ServiceTasks.Add(serviceTask);
                serviceOrder.ServiceTasks.Add(serviceTask);
                // Zapisz ServiceTask aby otrzymał Id
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Utworzono nowy ServiceTask z Id: {serviceTask.Id}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Istnieje ServiceTask z Id: {serviceTask.Id}");
            }

            // Jeśli nie wybrano żadnych części, usuń wszystkie powiązania
            if (!selectedParts.Any())
            {
                foreach (var usedPart in serviceTask.UsedParts.ToList())
                {
                    if (usedPart.Part != null)
                    {
                        usedPart.Part.Quantity += usedPart.Quantity;
                    }
                    _context.UsedParts.Remove(usedPart);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = orderId });
            }

            // Usuwanie części odznaczonych
            var usedPartsToRemove = serviceTask.UsedParts
                .Where(up => !selectedParts.Contains(up.PartId))
                .ToList();
            
            foreach (var usedPart in usedPartsToRemove)
            {
                if (usedPart.Part != null)
                {
                    usedPart.Part.Quantity += usedPart.Quantity;
                }
                _context.UsedParts.Remove(usedPart);
                System.Diagnostics.Debug.WriteLine($"Usunięto UsedPart dla PartId: {usedPart.PartId}");
            }

            // Dodaj lub aktualizuj powiązania dla zaznaczonych części
            foreach (var partId in selectedParts)
            {
                // Sprawdz czy mamy quantity dla tej części
                if (!quantities.ContainsKey(partId) || quantities[partId] < 1)
                {
                    System.Diagnostics.Debug.WriteLine($"Brak poprawnego quantity dla PartId: {partId}");
                    continue;
                }
                
                int requestedQuantity = quantities[partId];
                
                var part = await _context.Parts.FirstOrDefaultAsync(p => p.Id == partId);
                if (part == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Nie znaleziono części o Id: {partId} w bazie");
                    continue;
                }
                
                var existingUsedPart = serviceTask.UsedParts.FirstOrDefault(up => up.PartId == partId);
                
                if (existingUsedPart != null)
                {
                    // Aktualizacja istniejącej używanej części
                    System.Diagnostics.Debug.WriteLine($"Aktualizacja istniejącego UsedPart dla PartId: {partId}");
                    int difference = requestedQuantity - existingUsedPart.Quantity;
                    
                    if (difference > 0)
                    {
                        if (part.Quantity < difference)
                        {
                            System.Diagnostics.Debug.WriteLine($"Niewystarczająca ilość części w magazynie");
                            continue;
                        }
                        
                        part.Quantity -= difference;
                    }
                    else if (difference < 0)
                    {
                        part.Quantity += Math.Abs(difference);
                    }
                    
                    existingUsedPart.Quantity = requestedQuantity;
                    
                    if (serviceCosts != null && serviceCosts.ContainsKey(partId))
                    {
                        existingUsedPart.ServiceCost = serviceCosts[partId];
                    }
                }
                else
                {
                    // Dodawanie nowej używanej części
                    System.Diagnostics.Debug.WriteLine($"Tworzenie nowego UsedPart dla PartId: {partId}, Quantity: {requestedQuantity}");
                    
                    if (part.Quantity < requestedQuantity)
                    {
                        System.Diagnostics.Debug.WriteLine($"Niewystarczająca ilość części w magazynie");
                        continue;
                    }
                    
                    part.Quantity -= requestedQuantity;
                    
                    var usedPart = new UsedPart
                    {
                        PartId = partId,
                        Quantity = requestedQuantity,
                        ServiceTaskId = serviceTask.Id,
                        ServiceTask = serviceTask,
                        ServiceCost = serviceCosts != null && serviceCosts.ContainsKey(partId) ? serviceCosts[partId] : 0
                    };
                    
                    serviceTask.UsedParts.Add(usedPart);
                    _context.UsedParts.Add(usedPart);
                    System.Diagnostics.Debug.WriteLine($"Dodano UsedPart do kontekstu");
                }
            }
            
            System.Diagnostics.Debug.WriteLine("Zapisywanie zmian do bazy...");
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine("Zmiany zapisane");
            
            return RedirectToAction("Details", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateServiceCost(int usedPartId, int orderId, decimal serviceCost)
        {
            // Sprawdzenie czy użytkownik ma odpowiednie uprawnienia
            if (!User.IsInRole("Mechanik") && !User.IsInRole("Recepcjonista"))
            {
                return Forbid();
            }
            
            // Pobierz UsedPart z bazy
            var usedPart = await _context.UsedParts.FindAsync(usedPartId);
            if (usedPart == null)
            {
                return NotFound();
            }
            
            // Aktualizacja kosztu usługi
            usedPart.ServiceCost = serviceCost;
            await _context.SaveChangesAsync();
            
            // Przekierowanie z powrotem do strony szczegółów zlecenia
            return RedirectToAction("Details", new { id = orderId });
        }

        private bool ServiceOrderExists(int id)
        {
            return _context.ServiceOrders.Any(e => e.Id == id);
        }
    }
}
