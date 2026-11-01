using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET: Invoices
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Test)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            return View(invoices);
        }


        // ==========================================
        // GET: Invoices/Details/5
        // ==========================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Test)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }


        // 

        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.Id)
                .ToListAsync();

            ViewBag.Appointments = await _context.Appointments
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            ViewBag.Tests = await _context.Tests
                .Where(t => t.IsActive == true || t.IsActive == null)
                .OrderBy(t => t.TestName)
                .ToListAsync();

            return View();
        }


        // 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Invoice invoice,
            int[] testIds,
            int[] quantities)
        {
            if (testIds == null || testIds.Length == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one test."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateData();
                return View(invoice);
            }

            // Generate Invoice Number
            invoice.InvoiceNumber =
                "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            invoice.InvoiceDate = DateTime.Now;

            invoice.Discount ??= 0;
            invoice.Tax ??= 0;
            invoice.PaidAmount ??= 0;

            invoice.Subtotal = 0;

            var selectedTests = await _context.Tests
                .Where(t => testIds.Contains(t.Id))
                .ToListAsync();

            var invoiceItems = new List<InvoiceItem>();

            for (int i = 0; i < testIds.Length; i++)
            {
                var test = selectedTests
                    .FirstOrDefault(t => t.Id == testIds[i]);

                if (test == null)
                {
                    continue;
                }

                int quantity = 1;

                if (quantities != null &&
                    i < quantities.Length &&
                    quantities[i] > 0)
                {
                    quantity = quantities[i];
                }

                var item = new InvoiceItem
                {
                    TestId = test.Id,
                    Quantity = quantity,
                    UnitPrice = test.Price
                };

                // Subtotal হিসাব
                invoice.Subtotal += test.Price * quantity;

                invoiceItems.Add(item);
            }

            invoice.NetAmount =
                invoice.Subtotal
                - (invoice.Discount ?? 0)
                + (invoice.Tax ?? 0);

            invoice.DueAmount =
                invoice.NetAmount
                - (invoice.PaidAmount ?? 0);

            invoice.InvoiceItems = invoiceItems;

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // 

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            await LoadCreateData();

            return View(invoice);
        }


        // 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateData();
                return View(invoice);
            }

            try
            {
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (existingInvoice == null)
                {
                    return NotFound();
                }

                existingInvoice.PatientId = invoice.PatientId;
                existingInvoice.AppointmentId = invoice.AppointmentId;
                existingInvoice.Discount = invoice.Discount ?? 0;
                existingInvoice.Tax = invoice.Tax ?? 0;
                existingInvoice.PaidAmount = invoice.PaidAmount ?? 0;

                existingInvoice.NetAmount =
                    existingInvoice.Subtotal
                    - (existingInvoice.Discount ?? 0)
                    + (existingInvoice.Tax ?? 0);

                existingInvoice.DueAmount =
                    existingInvoice.NetAmount
                    - (existingInvoice.PaidAmount ?? 0);

                _context.Update(existingInvoice);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(invoice.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }


        // 

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }


        // ==========================================
        // POST: Invoices/Delete/5
        // ==========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // PAYMENT STATUS
        // ==========================================

        public async Task<IActionResult> PaymentStatus()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            return View(invoices);
        }


        // ==========================================
        // HELPER METHODS
        // ==========================================

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(i => i.Id == id);
        }


        private async Task LoadCreateData()
        {
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.Id)
                .ToListAsync();

            ViewBag.Appointments = await _context.Appointments
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            ViewBag.Tests = await _context.Tests
                .Where(t => t.IsActive == true || t.IsActive == null)
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }
    }
}