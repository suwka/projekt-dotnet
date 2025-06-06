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
                return RedirectToAction("Panel", "Receptionist");
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
                .Include(s => s.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (serviceOrder == null)
            {
                return NotFound();
            }

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

        private bool ServiceOrderExists(int id)
        {
            return _context.ServiceOrders.Any(e => e.Id == id);
        }
    }
}
