using Labsync.Models.Data;
using LabSync.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET: Payments
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.PaymentMethod)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(payments);
        }


        // ==========================================
        // GET: Payments/Details/5
        // ==========================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }


        // ==========================================
        // GET: Payments/Create
        // ==========================================

        public async Task<IActionResult> Create()
        {
            await LoadCreateData();

            return View();
        }


        // ==========================================
        // POST: Payments/Create
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (!ModelState.IsValid)
            {
                await LoadCreateData();

                return View(payment);
            }

            payment.PaymentDate ??= DateTime.Now;

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId);

            if (invoice == null)
            {
                ModelState.AddModelError(
                    "InvoiceId",
                    "Selected invoice was not found."
                );

                await LoadCreateData();

                return View(payment);
            }

            if (payment.Amount <= 0)
            {
                ModelState.AddModelError(
                    "Amount",
                    "Payment amount must be greater than zero."
                );

                await LoadCreateData();

                return View(payment);
            }

            var currentPaidAmount = invoice.PaidAmount ?? 0;

            var remainingAmount =
                invoice.NetAmount - currentPaidAmount;

            if (payment.Amount > remainingAmount)
            {
                ModelState.AddModelError(
                    "Amount",
                    "Payment amount cannot be greater than the due amount."
                );

                await LoadCreateData();

                return View(payment);
            }

            _context.Payments.Add(payment);

            // Update Invoice payment information
            invoice.PaidAmount =
                currentPaidAmount + payment.Amount;

            invoice.DueAmount =
                invoice.NetAmount - invoice.PaidAmount.Value;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: Payments/Edit/5
        // ==========================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            await LoadCreateData();

            return View(payment);
        }


        // ==========================================
        // POST: Payments/Edit/5
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateData();

                return View(payment);
            }

            try
            {
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingPayment == null)
                {
                    return NotFound();
                }

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i =>
                        i.Id == existingPayment.InvoiceId);

                if (invoice == null)
                {
                    return NotFound();
                }

                // Restore the old payment amount
                invoice.PaidAmount =
                    (invoice.PaidAmount ?? 0)
                    - existingPayment.Amount;

                // Check new amount
                if (payment.Amount <= 0)
                {
                    ModelState.AddModelError(
                        "Amount",
                        "Payment amount must be greater than zero."
                    );

                    await LoadCreateData();

                    return View(payment);
                }

                var remainingAmount =
                    invoice.NetAmount - invoice.PaidAmount.Value;

                if (payment.Amount > remainingAmount)
                {
                    ModelState.AddModelError(
                        "Amount",
                        "Payment amount cannot be greater than the due amount."
                    );

                    await LoadCreateData();

                    return View(payment);
                }

                existingPayment.PaymentMethodId =
                    payment.PaymentMethodId;

                existingPayment.PaymentDate =
                    payment.PaymentDate ?? DateTime.Now;

                existingPayment.Amount =
                    payment.Amount;

                existingPayment.TransactionNumber =
                    payment.TransactionNumber;

                existingPayment.Remarks =
                    payment.Remarks;

                // Apply the new payment amount
                invoice.PaidAmount =
                    invoice.PaidAmount.Value
                    + payment.Amount;

                invoice.DueAmount =
                    invoice.NetAmount - invoice.PaidAmount.Value;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(payment.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // GET: Payments/Delete/5
        // ==========================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.PaymentMethod)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }


        // ==========================================
        // POST: Payments/Delete/5
        // ==========================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment != null)
            {
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i =>
                        i.Id == payment.InvoiceId);

                if (invoice != null)
                {
                    invoice.PaidAmount =
                        (invoice.PaidAmount ?? 0)
                        - payment.Amount;

                    if (invoice.PaidAmount < 0)
                    {
                        invoice.PaidAmount = 0;
                    }

                    invoice.DueAmount =
                        invoice.NetAmount
                        - invoice.PaidAmount.Value;
                }

                _context.Payments.Remove(payment);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // PAYMENT METHODS
        // ==========================================

        public async Task<IActionResult> PaymentMethods()
        {
            var paymentMethods = await _context.PaymentMethods
                .OrderBy(p => p.MethodName)
                .ToListAsync();

            return View(paymentMethods);
        }


        // ==========================================
        // HELPER METHODS
        // ==========================================

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(p => p.Id == id);
        }


        private async Task LoadCreateData()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .ToListAsync();

            ViewBag.Invoices = new SelectList(
                invoices,
                "Id",
                "InvoiceNumber"
            );

            ViewBag.PaymentMethods = new SelectList(
                await _context.PaymentMethods.ToListAsync(),
                "Id",
                "MethodName"
            );
        }
    }
}