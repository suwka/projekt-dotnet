using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Models;
using WorkshopManager.Data;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WorkshopManager.Controllers
{
    public class ServiceOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ServiceOrder/Create?vehicleId=5
        [HttpGet]
        public IActionResult Create(int vehicleId)
        {
            var serviceOrder = new ServiceOrder
            {
                VehicleId = vehicleId,
                CreatedAt = DateTime.Now,
                Status = WorkshopManager.Models.ServiceOrderStatus.Nowe,
                ServiceTasks = new List<ServiceTask>(),
                Comments = new List<Comment>()
            };
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
                return RedirectToAction("Panel", "Receptionist"); // Zmieniono na Receptionist
            }
            return View(serviceOrder);
        }
    }
}

