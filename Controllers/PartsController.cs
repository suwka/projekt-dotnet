using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Mechanik,Recepcjonista,Admin")]
    public class PartsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Parts
        public async Task<IActionResult> Index()
        {
            var parts = await _context.Parts.ToListAsync();
            return View(parts);
        }

        // GET: Parts/Add
        [Authorize(Roles = "Admin,Recepcjonista")]
        public IActionResult Add()
        {
            return View();
        }

        // POST: Parts/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Recepcjonista")]
        public async Task<IActionResult> Add([Bind("Name,Manufacturer,CatalogNumber,Quantity,UnitPrice,Description")] Part part)
        {
            ViewBag.DebugValues = System.Text.Json.JsonSerializer.Serialize(part);
            
            // Inicjalizacja pustej kolekcji UsedParts
            part.UsedParts = new List<UsedPart>();
            
            if (!ModelState.IsValid)
            {
                TempData["AddPartError"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(part);
            }

            try
            {
                _context.Parts.Add(part);
                await _context.SaveChangesAsync();
                TempData["AddPartSuccess"] = "Część została dodana poprawnie.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["AddPartError"] = $"Błąd podczas zapisu do bazy: {ex.Message}";
                return View(part);
            }
        }

        // GET: Parts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound();
            return View(part);
        }

        // POST: Parts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Part part)
        {
            if (id != part.Id)
                return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(part);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PartExists(part.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(part);
        }

        // GET: Parts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var part = await _context.Parts.FirstOrDefaultAsync(m => m.Id == id);
            if (part == null)
                return NotFound();
            return View(part);
        }

        // GET: Parts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var part = await _context.Parts.FirstOrDefaultAsync(m => m.Id == id);
            if (part == null)
                return NotFound();
            return View(part);
        }

        // POST: Parts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part != null)
            {
                _context.Parts.Remove(part);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Parts/OrderParts
        public IActionResult OrderParts()
        {
            return View();
        }

        // POST: Parts/OrderParts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderParts([Bind("Brand,Model,PartNumber,Quantity,Description")] PartOrder partOrder)
        {
            if (!ModelState.IsValid)
            {
                return View(partOrder);
            }
            partOrder.CreatedAt = DateTime.Now;
            _context.PartOrders.Add(partOrder);
            await _context.SaveChangesAsync();
            TempData["OrderPartSuccess"] = "Zamówienie zostało złożone.";
            return RedirectToAction("Panel", "Mechanic");
        }

        // GET: Parts/PartOrders
        public async Task<IActionResult> PartOrders()
        {
            var orders = await _context.PartOrders.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        // GET: Parts/DeletePartOrder/5
        [Authorize(Roles = "Admin,Recepcjonista")]
        public async Task<IActionResult> DeletePartOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var partOrder = await _context.PartOrders.FirstOrDefaultAsync(p => p.Id == id);
            if (partOrder == null)
            {
                return NotFound();
            }
            return View(partOrder);
        }

        // POST: Parts/DeletePartOrder/5
        [HttpPost, ActionName("DeletePartOrder")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Recepcjonista")]
        public async Task<IActionResult> DeletePartOrderConfirmed(int id)
        {
            var partOrder = await _context.PartOrders.FindAsync(id);
            if (partOrder != null)
            {
                _context.PartOrders.Remove(partOrder);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("PartOrders");
        }

        private bool PartExists(int id)
        {
            return _context.Parts.Any(e => e.Id == id);
        }
    }
}
