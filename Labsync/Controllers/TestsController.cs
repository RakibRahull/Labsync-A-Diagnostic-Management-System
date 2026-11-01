using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    public class TestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tests
        public async Task<IActionResult> Index()
        {
            var tests = await _context.Tests
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tests);
        }

        // GET: Tests/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Test test)
        {
            if (!ModelState.IsValid)
            {
                return View(test);
            }

            test.CreatedAt = DateTime.Now;

            if (test.IsActive == null)
            {
                test.IsActive = true;
            }

            _context.Tests.Add(test);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Test created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Tests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var test = await _context.Tests.FindAsync(id);

            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        // POST: Tests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Test test)
        {
            if (id != test.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(test);
            }

            try
            {
                _context.Update(test);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Test updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TestExists(test.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Tests/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var test = await _context.Tests.FindAsync(id);

            if (test == null)
            {
                TempData["Error"] = "Test not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Tests.Remove(test);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Test deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Tests/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var test = await _context.Tests.FindAsync(id);

            if (test == null)
            {
                TempData["Error"] = "Test not found.";
                return RedirectToAction(nameof(Index));
            }

            test.IsActive = !(test.IsActive ?? false);

            await _context.SaveChangesAsync();

            TempData["Success"] = test.IsActive == true
                ? "Test activated successfully."
                : "Test deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private bool TestExists(int id)
        {
            return _context.Tests.Any(t => t.Id == id);
        }
    }
}
