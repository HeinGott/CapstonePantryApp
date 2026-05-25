using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VendorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // show all vendors
        public async Task<IActionResult> Index()
        {
            var vendors = await _context.Vendors.ToListAsync();

            return View(vendors);
        }

        // show one vendor
        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorID == id);

            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // show add vendor page
        public IActionResult Create()
        {
            return View();
        }

        // save new vendor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(vendor);
        }

        // show edit page
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // save edits
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vendor vendor)
        {
            if (id != vendor.VendorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(vendor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = vendor.VendorID });
            }

            return View(vendor);
        }

        // delete vendor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
            {
                return NotFound();
            }

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}