using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabSync.Areas.Receptionist.Controllers
{
    [Area("Receptionist")]
    [Authorize(Roles = "Receptionist")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // INDEX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? paymentId,
            DateTime? paymentDate)
        {
            IQueryable<Payment> query =
                _context.Payments
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Patient);

            // -----------------------------------------------------
            // PAYMENT ID SEARCH
            // Supports PAT1 or 1
            // -----------------------------------------------------
            if (!string.IsNullOrWhiteSpace(paymentId))
            {
                string search = paymentId.Trim();

                if (search.StartsWith(
                    "PAT",
                    StringComparison.OrdinalIgnoreCase))
                {
                    search = search.Substring(3);
                }

                if (int.TryParse(
                    search,
                    out int parsedId))
                {
                    query = query.Where(
                        p => p.Id == parsedId);
                }
            }

            // -----------------------------------------------------
            // PAYMENT DATE SEARCH
            // -----------------------------------------------------
            if (paymentDate.HasValue)
            {
                DateTime startDate =
                    paymentDate.Value.Date;

                DateTime endDate =
                    startDate.AddDays(1);

                query = query.Where(
                    p =>
                        p.PaymentDate.HasValue &&
                        p.PaymentDate.Value >= startDate &&
                        p.PaymentDate.Value < endDate);
            }

            // -----------------------------------------------------
            // LOAD PAYMENTS
            // -----------------------------------------------------
            var payments =
                await query
                    .OrderByDescending(
                        p => p.Id)
                    .ToListAsync();

            // -----------------------------------------------------
            // PAYMENT STATUS DICTIONARY
            // Status is stored in Invoice.PaymentStatusId
            // -----------------------------------------------------
            var paymentStatuses =
                await _context.PaymentStatuses
                    .ToDictionaryAsync(
                        s => s.Id,
                        s => s.StatusName);

            ViewBag.PaymentStatuses =
                paymentStatuses;

            ViewBag.PaymentId =
                paymentId;

            ViewBag.PaymentDate =
                paymentDate.HasValue
                    ? paymentDate.Value.ToString(
                        "yyyy-MM-dd")
                    : null;

            return View(payments);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadPaymentMethods();

            var payment =
                new Payment
                {
                    PaymentDate =
                        DateTime.Now
                };

            return View(payment);
        }


        // =========================================================
        // FIND INVOICE - AJAX
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> FindInvoice(
            string invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(
                invoiceNumber))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Please enter invoice number."
                });
            }

            invoiceNumber =
                invoiceNumber.Trim();

            // -----------------------------------------------------
            // FIND INVOICE
            // -----------------------------------------------------
            var invoice =
                await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(
                        i =>
                            i.InvoiceNumber ==
                            invoiceNumber);

            if (invoice == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Invoice not found."
                });
            }

           
            // EXISTING PAID AMOUNT
           
            decimal paidAmount =
                await _context.Payments
                    .Where(
                        p =>
                            p.InvoiceId ==
                            invoice.Id)
                    .Select(
                        p =>
                            (decimal?)p.Amount)
                    .SumAsync()
                ?? 0m;

            // -----------------------------------------------------
            // NET AMOUNT
            // Safe for decimal OR decimal?
            // -----------------------------------------------------
            decimal netAmount =
                SafeDecimal(
                    invoice.NetAmount);

 
            // DUE
            
            decimal dueAmount =
                netAmount -
                paidAmount;
            // IF Due Negative
            if (dueAmount < 0m)
            {
                dueAmount = 0m;
            }

            // -----------------------------------------------------
            // PAYMENT STATUS
            // -----------------------------------------------------
            var paymentStatus =
                await _context.PaymentStatuses
                    .FirstOrDefaultAsync(
                        s =>
                            s.Id ==
                            invoice.PaymentStatusId);

            // -----------------------------------------------------
            // If FULLY PAID
        
            if (dueAmount <= 0m)
            {
                return Json(new
                {
                    success = false,
                    alreadyPaid = true,
                    message =
                        "This invoice is already fully paid."
                });
            }

            // -----------------------------------------------------
            // INVOICE DATE
            // -----------------------------------------------------
            string invoiceDate =
                invoice.InvoiceDate.HasValue
                    ? invoice.InvoiceDate.Value
                        .ToString("dd MMM yyyy")
                    : "N/A";

            // -----------------------------------------------------
            // APPOINTMENT DATE
            // -----------------------------------------------------
            string appointmentDate =
                invoice.Appointment != null
                    ? invoice.Appointment
                        .AppointmentDate
                        .ToString("dd MMM yyyy")
                    : "N/A";

            // -----------------------------------------------------
            // APPOINTMENT TIME
            // -----------------------------------------------------
            string appointmentTime =
                invoice.Appointment != null
                    ? DateTime.Today
                        .Add(
                            invoice.Appointment
                                .AppointmentTime)
                        .ToString("hh:mm tt")
                    : "N/A";

            // -----------------------------------------------------
            // RETURN JSON
            // -----------------------------------------------------
            return Json(new
            {
                success = true,

                invoiceId =
                    invoice.Id,

                invoiceNumber =
                    invoice.InvoiceNumber,

                invoiceDate =
                    invoiceDate,

                subtotal =
                    SafeDecimal(
                        invoice.Subtotal),

                discount =
                    SafeDecimal(
                        invoice.Discount),

                tax =
                    SafeDecimal(
                        invoice.Tax),

                totalAmount =
                    netAmount,

                paidAmount =
                    paidAmount,

                dueAmount =
                    dueAmount,

                paymentStatusName =
                    paymentStatus?.StatusName
                    ?? "Unpaid",

                patientId =
                    invoice.PatientId,

                patientCode =
                    invoice.Patient?.PatientCode
                    ?? "N/A",

                patientName =
                    invoice.Patient != null
                        ? $"{invoice.Patient.FirstName} {invoice.Patient.LastName}"
                        : "N/A",

                patientPhone =
                    invoice.Patient?.Phone
                    ?? "N/A",

                patientEmail =
                    invoice.Patient?.Email
                    ?? "N/A",

                appointmentId =
                    invoice.AppointmentId,

                appointmentCode =
                    invoice.Appointment
                        ?.AppointmentCode
                    ?? "N/A",

                appointmentDate =
                    appointmentDate,

                appointmentTime =
                    appointmentTime
            });
        }


        // =========================================================
        // CREATE - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Payment payment)
        {
            // -----------------------------------------------------
            // Navigation properties
            // -----------------------------------------------------
            ModelState.Remove(
                nameof(Payment.Invoice));

            ModelState.Remove(
                nameof(Payment.PaymentMethod));

            // IMPORTANT:
            // TransactionNumber is NOT removed because
            // non-cash payment requires user input.


            // =====================================================
            // PAYMENT DATE
            // =====================================================
            if (!payment.PaymentDate.HasValue)
            {
                payment.PaymentDate =
                    DateTime.Now;
            }


            // =====================================================
            // PAYMENT AMOUNT
            // Payment.Amount may be decimal?
            // =====================================================
            decimal newPaymentAmount =
                SafeDecimal(
                    payment.Amount);

            if (newPaymentAmount <= 0m)
            {
                ModelState.AddModelError(
                    nameof(Payment.Amount),
                    "Payment amount must be greater than 0.");
            }


            // =====================================================
            // FIND INVOICE
            // =====================================================
            var invoice =
                await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(
                        i =>
                            i.Id ==
                            payment.InvoiceId);

            if (invoice == null)
            {
                ModelState.AddModelError(
                    nameof(Payment.InvoiceId),
                    "Invoice not found.");
            }


            // =====================================================
            // FIND PAYMENT METHOD
            // =====================================================
            var paymentMethod =
                await _context.PaymentMethods
                    .FirstOrDefaultAsync(
                        pm =>
                            pm.Id ==
                            payment.PaymentMethodId);

            if (paymentMethod == null)
            {
                ModelState.AddModelError(
                    nameof(Payment.PaymentMethodId),
                    "Please select a valid payment method.");
            }


            // =====================================================
            // CASH CHECK
            // =====================================================
            bool isCash =
                paymentMethod != null &&
                paymentMethod.MethodName
                    .Trim()
                    .Equals(
                        "Cash",
                        StringComparison.OrdinalIgnoreCase);


            // =====================================================
            // PAYMENT METHOD DETAILS
            // =====================================================

            string methodNameForValidation =
                paymentMethod?.MethodName?.Trim() ?? string.Empty;

            bool isMobileBanking =
                methodNameForValidation.Equals("Mobile Banking", StringComparison.OrdinalIgnoreCase)
                || methodNameForValidation.Contains("mobile", StringComparison.OrdinalIgnoreCase);

            bool isBank =
                !isMobileBanking &&
                (methodNameForValidation.Equals("Bank", StringComparison.OrdinalIgnoreCase)
                || methodNameForValidation.Equals("Bank Payment", StringComparison.OrdinalIgnoreCase)
                || methodNameForValidation.Equals("Bank Transfer", StringComparison.OrdinalIgnoreCase));

            if (isCash)
            {
                payment.TransactionNumber = null;
                payment.BankName = null;
                payment.MobileBanking = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(payment.TransactionNumber))
                {
                    ModelState.AddModelError(
                        nameof(Payment.TransactionNumber),
                        "Transaction number is required for non-cash payment.");
                }
                else
                {
                    payment.TransactionNumber = payment.TransactionNumber.Trim();
                }

                if (isBank)
                {
                    payment.MobileBanking = null;

                    if (string.IsNullOrWhiteSpace(payment.BankName))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.BankName),
                            "Please select a bank.");
                    }
                    else if (!IsAllowedBank(payment.BankName))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.BankName),
                            "Invalid bank selected.");
                    }
                }
                else if (isMobileBanking)
                {
                    payment.BankName = null;

                    if (string.IsNullOrWhiteSpace(payment.MobileBanking))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.MobileBanking),
                            "Please select a mobile banking service.");
                    }
                    else if (!IsAllowedMobileBanking(payment.MobileBanking))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.MobileBanking),
                            "Invalid mobile banking service selected.");
                    }
                }
                else
                {
                    payment.BankName = null;
                    payment.MobileBanking = null;
                }
            }


            // VALIDATION FAILED
            // =====================================================

            if (!ModelState.IsValid)
            {
                await LoadPaymentMethods(
                    payment.PaymentMethodId);

                return View(payment);
            }


            // =====================================================
            // EXISTING PAID AMOUNT
            // =====================================================

            decimal existingPaid =
                await _context.Payments
                    .Where(
                        p =>
                            p.InvoiceId ==
                            payment.InvoiceId)
                    .Select(
                        p =>
                            (decimal?)p.Amount)
                    .SumAsync()
                ?? 0m;


            // =====================================================
            // INVOICE NET AMOUNT
            // =====================================================

            decimal netAmount =
                SafeDecimal(
                    invoice!.NetAmount);


            // =====================================================
            // CURRENT DUE
            // =====================================================

            decimal currentDue =
                netAmount -
                existingPaid;


            if (currentDue < 0m)
            {
                currentDue = 0m;
            }


            // =====================================================
            // OVERPAYMENT
            // =====================================================

            if (newPaymentAmount >
                currentDue)
            {
                ModelState.AddModelError(
                    nameof(Payment.Amount),
                    $"Payment cannot be greater than due amount. Maximum allowed is BDT {currentDue:N2}.");

                await LoadPaymentMethods(
                    payment.PaymentMethodId);

                return View(payment);
            }


            // =====================================================
            // MAKE SURE PAYMENT AMOUNT IS STORED
            // =====================================================

            payment.Amount =
                newPaymentAmount;


            // =====================================================
            // SAVE PAYMENT
            // =====================================================

            _context.Payments.Add(
                payment);

            await _context.SaveChangesAsync();


            // =====================================================
            // NEW PAID
            // =====================================================

            decimal totalPaid =
                existingPaid +
                newPaymentAmount;


            // =====================================================
            // NEW DUE
            // =====================================================

            decimal totalDue =
                netAmount -
                totalPaid;


            if (totalDue < 0m)
            {
                totalDue = 0m;
            }


            // =====================================================
            // UPDATE INVOICE
            // =====================================================

            invoice.PaidAmount =
                totalPaid;

            invoice.DueAmount =
                totalDue;

            invoice.PaymentStatusId =
                await GetPaymentStatusId(
                    totalPaid,
                    netAmount);


            _context.Invoices.Update(
                invoice);


            await _context.SaveChangesAsync();


            // =====================================================
            // OPEN PAYMENT PDF
            // =====================================================

            return RedirectToAction(
                nameof(GeneratePdf),
                new
                {
                    id = payment.Id
                });
        }


        // =========================================================
        // DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var payment =
                await _context.Payments
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Patient)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Appointment)
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(
                "~/Areas/Receptionist/Views/Payments/Details.cshtml",
                payment);
        }


        // =========================================================
        // EDIT - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var payment =
                await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Patient)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Appointment)
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            await LoadPaymentMethods(
                payment.PaymentMethodId);

            return View(payment);
        }


        // =========================================================
        // EDIT - POST
        // =========================================================
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

            // -----------------------------------------------------
            // Navigation properties
            // -----------------------------------------------------
            ModelState.Remove(
                nameof(Payment.Invoice));

            ModelState.Remove(
                nameof(Payment.PaymentMethod));


            // =====================================================
            // DATE
            // =====================================================
            if (!payment.PaymentDate.HasValue)
            {
                payment.PaymentDate =
                    DateTime.Now;
            }


            // =====================================================
            // AMOUNT
            // =====================================================
            decimal newAmount =
                SafeDecimal(
                    payment.Amount);

            if (newAmount <= 0m)
            {
                ModelState.AddModelError(
                    nameof(Payment.Amount),
                    "Payment amount must be greater than 0.");
            }


            // =====================================================
            // PAYMENT METHOD
            // =====================================================
            var selectedMethod =
                await _context.PaymentMethods
                    .FirstOrDefaultAsync(
                        pm =>
                            pm.Id ==
                            payment.PaymentMethodId);

            if (selectedMethod == null)
            {
                ModelState.AddModelError(
                    nameof(Payment.PaymentMethodId),
                    "Please select a valid payment method.");
            }


            // =====================================================
            // CASH CHECK
            // =====================================================
            bool isCash =
                selectedMethod != null &&
                selectedMethod.MethodName
                    .Trim()
                    .Equals(
                        "Cash",
                        StringComparison.OrdinalIgnoreCase);


            // =====================================================
            // PAYMENT METHOD DETAILS
            // =====================================================

            string editMethodName =
                selectedMethod?.MethodName?.Trim() ?? string.Empty;

            bool isMobileBankingEdit =
                editMethodName.Equals("Mobile Banking", StringComparison.OrdinalIgnoreCase)
                || editMethodName.Contains("mobile", StringComparison.OrdinalIgnoreCase);

            bool isBankEdit =
                !isMobileBankingEdit &&
                (editMethodName.Equals("Bank", StringComparison.OrdinalIgnoreCase)
                || editMethodName.Equals("Bank Payment", StringComparison.OrdinalIgnoreCase)
                || editMethodName.Equals("Bank Transfer", StringComparison.OrdinalIgnoreCase));

            if (isCash)
            {
                payment.TransactionNumber = null;
                payment.BankName = null;
                payment.MobileBanking = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(payment.TransactionNumber))
                {
                    ModelState.AddModelError(
                        nameof(Payment.TransactionNumber),
                        "Transaction number is required for non-cash payment.");
                }
                else
                {
                    payment.TransactionNumber = payment.TransactionNumber.Trim();
                }

                if (isBankEdit)
                {
                    payment.MobileBanking = null;

                    if (string.IsNullOrWhiteSpace(payment.BankName))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.BankName),
                            "Please select a bank.");
                    }
                    else if (!IsAllowedBank(payment.BankName))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.BankName),
                            "Invalid bank selected.");
                    }
                }
                else if (isMobileBankingEdit)
                {
                    payment.BankName = null;

                    if (string.IsNullOrWhiteSpace(payment.MobileBanking))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.MobileBanking),
                            "Please select a mobile banking service.");
                    }
                    else if (!IsAllowedMobileBanking(payment.MobileBanking))
                    {
                        ModelState.AddModelError(
                            nameof(Payment.MobileBanking),
                            "Invalid mobile banking service selected.");
                    }
                }
                else
                {
                    payment.BankName = null;
                    payment.MobileBanking = null;
                }
            }


            // VALIDATION
            // =====================================================
            if (!ModelState.IsValid)
            {
                await LoadPaymentMethods(
                    payment.PaymentMethodId);

                payment.Invoice =
                    await _context.Invoices
                        .Include(i => i.Patient)
                        .Include(i => i.Appointment)
                        .FirstOrDefaultAsync(
                            i =>
                                i.Id ==
                                payment.InvoiceId);

                return View(payment);
            }


            // =====================================================
            // EXISTING PAYMENT
            // =====================================================
            var existingPayment =
                await _context.Payments
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            id);

            if (existingPayment == null)
            {
                return NotFound();
            }


            int oldInvoiceId =
                existingPayment.InvoiceId;

            int newInvoiceId =
                payment.InvoiceId;


            // =====================================================
            // SAME INVOICE
            // =====================================================
            if (oldInvoiceId ==
                newInvoiceId)
            {
                var invoice =
                    await _context.Invoices
                        .FirstOrDefaultAsync(
                            i =>
                                i.Id ==
                                newInvoiceId);

                if (invoice == null)
                {
                    return NotFound();
                }


                // -------------------------------------------------
                // Other Payments
                // -------------------------------------------------
                decimal otherPayments =
                    await _context.Payments
                        .Where(
                            p =>
                                p.InvoiceId ==
                                newInvoiceId &&
                                p.Id != id)
                        .Select(
                            p =>
                                (decimal?)p.Amount)
                        .SumAsync()
                    ?? 0m;


                // -------------------------------------------------
                // Net Amount
                // -------------------------------------------------
                decimal netAmount =
                    SafeDecimal(
                        invoice.NetAmount);


                // -------------------------------------------------
                // Available Due
                // -------------------------------------------------
                decimal availableDue =
                    netAmount -
                    otherPayments;


                if (availableDue < 0m)
                {
                    availableDue = 0m;
                }


                // -------------------------------------------------
                // Prevent Overpayment
                // -------------------------------------------------
                if (newAmount >
                    availableDue)
                {
                    ModelState.AddModelError(
                        nameof(Payment.Amount),
                        $"Payment cannot be greater than due amount. Maximum allowed is BDT {availableDue:N2}.");

                    await LoadPaymentMethods(
                        payment.PaymentMethodId);

                    payment.Invoice =
                        await _context.Invoices
                            .Include(i => i.Patient)
                            .Include(i => i.Appointment)
                            .FirstOrDefaultAsync(
                                i =>
                                    i.Id ==
                                    payment.InvoiceId);

                    return View(payment);
                }


                // =================================================
                // UPDATE PAYMENT
                // =================================================

                existingPayment.PaymentDate =
                    payment.PaymentDate;

                existingPayment.PaymentMethodId =
                    payment.PaymentMethodId;

                existingPayment.Amount =
                    newAmount;

                existingPayment.TransactionNumber =
                    isCash
                        ? null
                        : payment.TransactionNumber;

                existingPayment.BankName =
                    isBankEdit ? payment.BankName : null;

                existingPayment.MobileBanking =
                    isMobileBankingEdit ? payment.MobileBanking : null;


                // =================================================
                // RECALCULATE INVOICE
                // =================================================

                decimal totalPaid =
                    otherPayments +
                    newAmount;

                decimal totalDue =
                    netAmount -
                    totalPaid;

                if (totalDue < 0m)
                {
                    totalDue = 0m;
                }


                invoice.PaidAmount =
                    totalPaid;

                invoice.DueAmount =
                    totalDue;

                invoice.PaymentStatusId =
                    await GetPaymentStatusId(
                        totalPaid,
                        netAmount);


                await _context.SaveChangesAsync();


                return RedirectToAction(
                    nameof(Index));
            }


            // =====================================================
            // PAYMENT MOVED TO ANOTHER INVOICE
            // =====================================================

            var oldInvoice =
                await _context.Invoices
                    .FirstOrDefaultAsync(
                        i =>
                            i.Id ==
                            oldInvoiceId);


            var newInvoice =
                await _context.Invoices
                    .FirstOrDefaultAsync(
                        i =>
                            i.Id ==
                            newInvoiceId);


            if (newInvoice == null)
            {
                return NotFound();
            }


            // =====================================================
            // OLD INVOICE PAYMENTS
            // =====================================================

            decimal oldOtherPayments =
                await _context.Payments
                    .Where(
                        p =>
                            p.InvoiceId ==
                            oldInvoiceId &&
                            p.Id != id)
                    .Select(
                        p =>
                            (decimal?)p.Amount)
                    .SumAsync()
                ?? 0m;


            // =====================================================
            // NEW INVOICE PAYMENTS
            // =====================================================

            decimal newOtherPayments =
                await _context.Payments
                    .Where(
                        p =>
                            p.InvoiceId ==
                            newInvoiceId &&
                            p.Id != id)
                    .Select(
                        p =>
                            (decimal?)p.Amount)
                    .SumAsync()
                ?? 0m;


            // =====================================================
            // NEW INVOICE NET AMOUNT
            // =====================================================

            decimal newInvoiceNetAmount =
                SafeDecimal(
                    newInvoice.NetAmount);


            // =====================================================
            // NEW INVOICE AVAILABLE DUE
            // =====================================================

            decimal newInvoiceAvailableDue =
                newInvoiceNetAmount -
                newOtherPayments;


            if (newInvoiceAvailableDue < 0m)
            {
                newInvoiceAvailableDue = 0m;
            }


            // =====================================================
            // OVERPAYMENT
            // =====================================================

            if (newAmount >
                newInvoiceAvailableDue)
            {
                ModelState.AddModelError(
                    nameof(Payment.Amount),
                    $"Payment cannot be greater than due amount. Maximum allowed is BDT {newInvoiceAvailableDue:N2}.");

                await LoadPaymentMethods(
                    payment.PaymentMethodId);

                payment.Invoice =
                    await _context.Invoices
                        .Include(i => i.Patient)
                        .Include(i => i.Appointment)
                        .FirstOrDefaultAsync(
                            i =>
                                i.Id ==
                                newInvoiceId);

                return View(payment);
            }


            // =====================================================
            // UPDATE PAYMENT
            // =====================================================

            existingPayment.InvoiceId =
                newInvoiceId;

            existingPayment.PaymentDate =
                payment.PaymentDate;

            existingPayment.PaymentMethodId =
                payment.PaymentMethodId;

            existingPayment.Amount =
                newAmount;

            existingPayment.TransactionNumber =
                isCash
                    ? null
                    : payment.TransactionNumber;

            existingPayment.BankName =
                isBankEdit ? payment.BankName : null;

            existingPayment.MobileBanking =
                isMobileBankingEdit ? payment.MobileBanking : null;


            // =====================================================
            // RECALCULATE OLD INVOICE
            // =====================================================

            if (oldInvoice != null)
            {
                decimal oldNetAmount =
                    SafeDecimal(
                        oldInvoice.NetAmount);

                decimal oldPaid =
                    oldOtherPayments;

                decimal oldDue =
                    oldNetAmount -
                    oldPaid;

                if (oldDue < 0m)
                {
                    oldDue = 0m;
                }

                oldInvoice.PaidAmount =
                    oldPaid;

                oldInvoice.DueAmount =
                    oldDue;

                oldInvoice.PaymentStatusId =
                    await GetPaymentStatusId(
                        oldPaid,
                        oldNetAmount);
            }


            // =====================================================
            // RECALCULATE NEW INVOICE
            // =====================================================

            decimal newPaid =
                newOtherPayments +
                newAmount;

            decimal newDue =
                newInvoiceNetAmount -
                newPaid;

            if (newDue < 0m)
            {
                newDue = 0m;
            }

            newInvoice.PaidAmount =
                newPaid;

            newInvoice.DueAmount =
                newDue;

            newInvoice.PaymentStatusId =
                await GetPaymentStatusId(
                    newPaid,
                    newInvoiceNetAmount);


            await _context.SaveChangesAsync();


            return RedirectToAction(
                nameof(Index));
        }


        // =========================================================
        // DELETE - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Delete(
            int id)
        {
            var payment =
                await _context.Payments
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Patient)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Appointment)
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(
                "~/Areas/Receptionist/Views/Payments/Delete.cshtml",
                payment);
        }


        // =========================================================
        // DELETE - POST
        // =========================================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var payment =
                await _context.Payments
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            id);

            if (payment == null)
            {
                return NotFound();
            }


            int invoiceId =
                payment.InvoiceId;


            var invoice =
                await _context.Invoices
                    .FirstOrDefaultAsync(
                        i =>
                            i.Id ==
                            invoiceId);


            // Remove Payment
            _context.Payments.Remove(
                payment);


            // =====================================================
            // RECALCULATE INVOICE
            // =====================================================

            if (invoice != null)
            {
                decimal remainingPaid =
                    await _context.Payments
                        .Where(
                            p =>
                                p.InvoiceId ==
                                invoiceId &&
                                p.Id != id)
                        .Select(
                            p =>
                                (decimal?)p.Amount)
                        .SumAsync()
                    ?? 0m;


                decimal invoiceNetAmount =
                    SafeDecimal(
                        invoice.NetAmount);


                decimal remainingDue =
                    invoiceNetAmount -
                    remainingPaid;


                if (remainingDue < 0m)
                {
                    remainingDue = 0m;
                }


                invoice.PaidAmount =
                    remainingPaid;

                invoice.DueAmount =
                    remainingDue;

                invoice.PaymentStatusId =
                    await GetPaymentStatusId(
                        remainingPaid,
                        invoiceNetAmount);


                _context.Invoices.Update(
                    invoice);
            }


            await _context.SaveChangesAsync();


            return RedirectToAction(
                nameof(Index));
        }


        
        // GENERATE PAYMENT PDF
        
        [HttpGet]
        public async Task<IActionResult> GeneratePdf(
            int id)
        {
            var payment =
                await _context.Payments
                    .Include(p => p.PaymentMethod)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Patient)
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Appointment)
                    .FirstOrDefaultAsync(
                        p =>
                            p.Id ==
                            id);


            if (payment == null)
            {
                return NotFound();
            }


            if (payment.Invoice == null)
            {
                return NotFound(
                    "Invoice not found for this payment.");
            }


            var invoice =
                payment.Invoice;


            // =====================================================
            // PAYMENT STATUS
            // =====================================================

            var paymentStatus =
                await _context.PaymentStatuses
                    .FirstOrDefaultAsync(
                        s =>
                            s.Id ==
                            invoice.PaymentStatusId);


            // =====================================================
            // PATIENT
            // =====================================================

            string patientName =
                invoice.Patient != null
                    ? $"{invoice.Patient.FirstName} {invoice.Patient.LastName}"
                    : "N/A";


            string patientCode =
                invoice.Patient?.PatientCode
                ?? "N/A";


            string patientPhone =
                invoice.Patient?.Phone
                ?? "N/A";


            string patientEmail =
                invoice.Patient?.Email
                ?? "N/A";


            // =====================================================
            // APPOINTMENT
            // =====================================================

            string appointmentCode =
                invoice.Appointment
                    ?.AppointmentCode
                ?? "N/A";


            string appointmentDate =
                invoice.Appointment != null
                    ? invoice.Appointment
                        .AppointmentDate
                        .ToString("dd MMM yyyy")
                    : "N/A";


            string appointmentTime =
                invoice.Appointment != null
                    ? DateTime.Today
                        .Add(
                            invoice.Appointment
                                .AppointmentTime)
                        .ToString("hh:mm tt")
                    : "N/A";


            // =====================================================
            // DECIMAL VALUES
            // Safe for decimal / decimal?
            // =====================================================

            decimal paymentAmount =
                SafeDecimal(
                    payment.Amount);


            decimal subtotal =
                SafeDecimal(
                    invoice.Subtotal);


            decimal discount =
                SafeDecimal(
                    invoice.Discount);


            decimal tax =
                SafeDecimal(
                    invoice.Tax);


            decimal netAmount =
                SafeDecimal(
                    invoice.NetAmount);


            decimal paidAmount =
                SafeDecimal(
                    invoice.PaidAmount);


            decimal dueAmount =
                SafeDecimal(
                    invoice.DueAmount);


            // =====================================================
            // INVOICE DATE
            // =====================================================

            string invoiceDate =
                invoice.InvoiceDate.HasValue
                    ? invoice.InvoiceDate.Value
                        .ToString("dd MMM yyyy")
                    : "N/A";


            // =====================================================
            // TRANSACTION NUMBER
            // =====================================================

            string transactionNumber =
                string.IsNullOrWhiteSpace(
                    payment.TransactionNumber)
                    ? "N/A - Cash Payment"
                    : payment.TransactionNumber;


            // =====================================================
            // PAYMENT METHOD
            // =====================================================

            string methodName =
                payment.PaymentMethod
                    ?.MethodName
                ?? payment.PaymentMethodId
                    .ToString();

            string paymentChannel =
                !string.IsNullOrWhiteSpace(payment.BankName)
                    ? payment.BankName
                    : !string.IsNullOrWhiteSpace(payment.MobileBanking)
                        ? payment.MobileBanking
                        : "N/A";


            // =====================================================
            // STATUS
            // =====================================================

            string statusName =
                paymentStatus?.StatusName
                ?? "Unpaid";


            // =====================================================
            // CREATE PDF
            // =====================================================

            byte[] pdf =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // =================================================
                        // PAGE
                        // =================================================

                        page.Size(
                            PageSizes.A4);

                        page.Margin(
                            40);


                        // =================================================
                        // DEFAULT FONT
                        // ONLY LATO
                        // =================================================

                        page.DefaultTextStyle(
                            x =>
                                x
                                    .FontFamily("Lato")
                                    .FontSize(10)
                                    .FontColor("#29243A"));


                        // =================================================
                        // HEADER
                        // =================================================

                        page.Header()
                            .Column(column =>
                            {
                                column.Spacing(4);


                                column.Item()
                                    .Text("LABSYNC")
                                    .FontFamily("Lato")
                                    .FontSize(28)
                                    .Bold()
                                    .FontColor("#8B5CF6");


                                column.Item()
                                    .Text(
                                        "Laboratory Management System")
                                    .FontFamily("Lato")
                                    .FontSize(10)
                                    .FontColor("#9296AA");


                                column.Item()
                                    .PaddingTop(10)
                                    .LineHorizontal(1)
                                    .LineColor("#E8E5EF");
                            });


                        // =================================================
                        // CONTENT
                        // =================================================

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(16);


                                // =========================================
                                // TITLE
                                // =========================================

                                column.Item()
                                    .PaddingTop(12)
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text(
                                                "PAYMENT RECEIPT")
                                            .FontFamily("Lato")
                                            .FontSize(22)
                                            .Bold()
                                            .FontColor("#18213A");


                                        row.ConstantItem(185)
                                            .AlignRight()
                                            .Column(right =>
                                            {
                                                right.Item()
                                                    .Text(
                                                        $"Payment ID: PAT{payment.Id}")
                                                    .FontFamily("Lato")
                                                    .FontSize(10)
                                                    .SemiBold()
                                                    .FontColor("#29243A");


                                                right.Item()
                                                    .Text(
                                                        payment.PaymentDate.HasValue
                                                            ? $"Date: {payment.PaymentDate.Value:dd MMM yyyy}"
                                                            : "Date: N/A")
                                                    .FontFamily("Lato")
                                                    .FontSize(10)
                                                    .FontColor("#777080");
                                            });
                                    });


                                // =========================================
                                // PAYMENT INFORMATION
                                // =========================================

                                column.Item()
                                    .Border(1)
                                    .BorderColor("#E8E5EF")
                                    .Background("#FBFAFE")
                                    .Padding(14)
                                    .Column(card =>
                                    {
                                        card.Item()
                                            .Text(
                                                "Payment Information")
                                            .FontFamily("Lato")
                                            .FontSize(14)
                                            .Bold()
                                            .FontColor("#8B5CF6");


                                        card.Item()
                                            .PaddingTop(8)
                                            .Table(table =>
                                            {
                                                table.ColumnsDefinition(
                                                    columns =>
                                                    {
                                                        columns
                                                            .ConstantColumn(125);

                                                        columns
                                                            .RelativeColumn();

                                                        columns
                                                            .ConstantColumn(125);

                                                        columns
                                                            .RelativeColumn();
                                                    });


                                                AddPdfCell(
                                                    table,
                                                    "Transaction Number",
                                                    transactionNumber,
                                                    true);


                                                AddPdfCell(
                                                    table,
                                                    "Payment Method",
                                                    methodName,
                                                    true);

                                                if (paymentChannel != "N/A")
                                                {
                                                    AddPdfCell(
                                                        table,
                                                        "Bank / Mobile",
                                                        paymentChannel,
                                                        true);
                                                }


                                                AddPdfCell(
                                                    table,
                                                    "Payment Amount",
                                                    $"BDT {paymentAmount:N2}",
                                                    true);


                                                AddPdfCell(
                                                    table,
                                                    "Payment Status",
                                                    statusName,
                                                    true);
                                            });
                                    });


                                // =========================================
                                // PATIENT INFORMATION
                                // =========================================

                                column.Item()
                                    .Border(1)
                                    .BorderColor("#E8E5EF")
                                    .Padding(14)
                                    .Column(card =>
                                    {
                                        card.Item()
                                            .Text(
                                                "Patient Information")
                                            .FontFamily("Lato")
                                            .FontSize(14)
                                            .Bold()
                                            .FontColor("#8B5CF6");


                                        card.Item()
                                            .PaddingTop(8)
                                            .Table(table =>
                                            {
                                                table.ColumnsDefinition(
                                                    columns =>
                                                    {
                                                        columns
                                                            .ConstantColumn(95);

                                                        columns
                                                            .RelativeColumn();

                                                        columns
                                                            .ConstantColumn(95);

                                                        columns
                                                            .RelativeColumn();
                                                    });


                                                AddPdfCell(
                                                    table,
                                                    "Patient ID",
                                                    patientCode);


                                                AddPdfCell(
                                                    table,
                                                    "Patient Name",
                                                    patientName);


                                                AddPdfCell(
                                                    table,
                                                    "Phone",
                                                    patientPhone);


                                                AddPdfCell(
                                                    table,
                                                    "Email",
                                                    patientEmail);


                                                AddPdfCell(
                                                    table,
                                                    "Invoice Number",
                                                    invoice.InvoiceNumber);
                                            });
                                    });


                                // =========================================
                                // APPOINTMENT INFORMATION
                                // =========================================

                                column.Item()
                                    .Border(1)
                                    .BorderColor("#E8E5EF")
                                    .Padding(14)
                                    .Column(card =>
                                    {
                                        card.Item()
                                            .Text(
                                                "Appointment Information")
                                            .FontFamily("Lato")
                                            .FontSize(14)
                                            .Bold()
                                            .FontColor("#8B5CF6");


                                        card.Item()
                                            .PaddingTop(8)
                                            .Table(table =>
                                            {
                                                table.ColumnsDefinition(
                                                    columns =>
                                                    {
                                                        columns
                                                            .ConstantColumn(105);

                                                        columns
                                                            .RelativeColumn();

                                                        columns
                                                            .ConstantColumn(105);

                                                        columns
                                                            .RelativeColumn();
                                                    });


                                                AddPdfCell(
                                                    table,
                                                    "Appointment ID",
                                                    appointmentCode);


                                                AddPdfCell(
                                                    table,
                                                    "Appointment Date",
                                                    appointmentDate);


                                                AddPdfCell(
                                                    table,
                                                    "Appointment Time",
                                                    appointmentTime);


                                                AddPdfCell(
                                                    table,
                                                    "Invoice Date",
                                                    invoiceDate);
                                            });
                                    });


                                // =========================================
                                // INVOICE SUMMARY
                                // =========================================

                                column.Item()
                                    .Background("#F7F4FF")
                                    .Border(1)
                                    .BorderColor("#E8E5EF")
                                    .Padding(14)
                                    .Column(card =>
                                    {
                                        card.Item()
                                            .Text(
                                                "Invoice Summary")
                                            .FontFamily("Lato")
                                            .FontSize(14)
                                            .Bold()
                                            .FontColor("#8B5CF6");


                                        card.Item()
                                            .PaddingTop(8)
                                            .Table(table =>
                                            {
                                                table.ColumnsDefinition(
                                                    columns =>
                                                    {
                                                        columns
                                                            .RelativeColumn();

                                                        columns
                                                            .ConstantColumn(145);
                                                    });


                                                AddAmountRow(
                                                    table,
                                                    "Subtotal",
                                                    subtotal);


                                                AddAmountRow(
                                                    table,
                                                    "Discount",
                                                    discount);


                                                AddAmountRow(
                                                    table,
                                                    "Tax",
                                                    tax);


                                                AddAmountRow(
                                                    table,
                                                    "Net Amount",
                                                    netAmount,
                                                    true);


                                                AddAmountRow(
                                                    table,
                                                    "Paid Amount",
                                                    paidAmount,
                                                    true);


                                                AddAmountRow(
                                                    table,
                                                    "Due Amount",
                                                    dueAmount,
                                                    true);


                                                table.Cell()
                                                    .ColumnSpan(2)
                                                    .PaddingTop(7)
                                                    .LineHorizontal(1)
                                                    .LineColor("#DDD8EA");


                                                table.Cell()
                                                    .PaddingTop(7)
                                                    .Text(
                                                        "This Payment")
                                                    .FontFamily("Lato")
                                                    .Bold()
                                                    .FontSize(12);


                                                table.Cell()
                                                    .PaddingTop(7)
                                                    .AlignRight()
                                                    .Text(
                                                        $"BDT {paymentAmount:N2}")
                                                    .FontFamily("Lato")
                                                    .Bold()
                                                    .FontSize(14)
                                                    .FontColor("#8B5CF6");
                                            });
                                    });


                                // =========================================
                                // FOOT NOTE
                                // =========================================

                                column.Item()
                                    .PaddingTop(8)
                                    .AlignCenter()
                                    .Text(
                                        "Thank you for choosing LabSync.")
                                    .FontFamily("Lato")
                                    .FontSize(10)
                                    .Italic()
                                    .FontColor("#9296AA");
                            });


                        // =================================================
                        // FOOTER
                        // =================================================

                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span(
                                    "LabSync - Payment Receipt")
                                    .FontFamily("Lato")
                                    .FontSize(9)
                                    .FontColor("#9993A8");


                                text.Span(" | ")
                                    .FontFamily("Lato")
                                    .FontSize(9)
                                    .FontColor("#9993A8");


                                text.Span(
                                    "Generated electronically")
                                    .FontFamily("Lato")
                                    .FontSize(9)
                                    .FontColor("#9993A8");
                            });
                    });
                })
                .GeneratePdf();


            // =====================================================
            // INLINE PDF
            // =====================================================

            return File(
                pdf,
                "application/pdf");
        }


        // =========================================================
        // LOAD PAYMENT METHODS
        // =========================================================
        private async Task LoadPaymentMethods(
            int? selectedId = null)
        {
            var methods =
                await _context.PaymentMethods
                    .OrderBy(
                        pm =>
                            pm.MethodName)
                    .ToListAsync();

            ViewBag.PaymentMethods =
                methods;

            ViewBag.SelectedPaymentMethodId =
                selectedId;
        }


        // =========================================================
        // BANK / MOBILE BANKING VALIDATION
        // =========================================================
        private static bool IsAllowedBank(string? bankName)
        {
            return bankName == "UCB Bank"
                || bankName == "Dutch-Bangla Bank"
                || bankName == "Sonali Bank";
        }

        private static bool IsAllowedMobileBanking(string? mobileBanking)
        {
            return mobileBanking == "bKash"
                || mobileBanking == "Nagad"
                || mobileBanking == "Rocket";
        }


        // =========================================================
        // PAYMENT STATUS
        // =========================================================
        private async Task<int> GetPaymentStatusId(
            decimal paidAmount,
            decimal netAmount)
        {
            paidAmount =
                Math.Round(
                    paidAmount,
                    2);

            netAmount =
                Math.Round(
                    netAmount,
                    2);


            // =====================================================
            // PAID
            // =====================================================

            if (paidAmount >= netAmount)
            {
                var paidStatus =
                    await _context.PaymentStatuses
                        .FirstOrDefaultAsync(
                            s =>
                                s.StatusName
                                    .ToLower()
                                    == "paid");

                if (paidStatus != null)
                {
                    return paidStatus.Id;
                }
            }


            // =====================================================
            // PARTIALLY PAID
            // =====================================================

            if (paidAmount > 0m)
            {
                var partialStatus =
                    await _context.PaymentStatuses
                        .FirstOrDefaultAsync(
                            s =>
                                s.StatusName
                                    .ToLower()
                                    .Contains(
                                        "partial"));

                if (partialStatus != null)
                {
                    return partialStatus.Id;
                }
            }


            // =====================================================
            // UNPAID
            // =====================================================

            var unpaidStatus =
                await _context.PaymentStatuses
                    .FirstOrDefaultAsync(
                        s =>
                            s.StatusName
                                .ToLower()
                                == "unpaid");

            if (unpaidStatus != null)
            {
                return unpaidStatus.Id;
            }


            // =====================================================
            // DUE
            // =====================================================

            var dueStatus =
                await _context.PaymentStatuses
                    .FirstOrDefaultAsync(
                        s =>
                            s.StatusName
                                .ToLower()
                                .Contains(
                                    "due"));

            if (dueStatus != null)
            {
                return dueStatus.Id;
            }


            // =====================================================
            // PENDING
            // =====================================================

            var pendingStatus =
                await _context.PaymentStatuses
                    .FirstOrDefaultAsync(
                        s =>
                            s.StatusName
                                .ToLower()
                                .Contains(
                                    "pending"));

            if (pendingStatus != null)
            {
                return pendingStatus.Id;
            }


            return 1;
        }


        // =========================================================
        // SAFE DECIMAL
        //
        // Supports BOTH:
        // decimal
        // decimal?
        //
        // This prevents:
        // CS0019 ?? decimal/decimal
        // CS0266 decimal? -> decimal
        // =========================================================

        private static decimal SafeDecimal(
            decimal value)
        {
            return value;
        }


        private static decimal SafeDecimal(
            decimal? value)
        {
            return value ?? 0m;
        }


        // =========================================================
        // PDF CELL HELPER
        // =========================================================

        private static void AddPdfCell(
            TableDescriptor table,
            string label,
            string value,
            bool emphasize = false)
        {
            table.Cell()
                .PaddingVertical(5)
                .Text(label)
                .FontFamily("Lato")
                .SemiBold()
                .FontSize(9)
                .FontColor("#777080");


            var valueCell =
                table.Cell()
                    .PaddingVertical(5);


            if (emphasize)
            {
                valueCell
                    .Text(value)
                    .FontFamily("Lato")
                    .SemiBold()
                    .FontSize(10)
                    .FontColor("#29243A");
            }
            else
            {
                valueCell
                    .Text(value)
                    .FontFamily("Lato")
                    .FontSize(10)
                    .FontColor("#29243A");
            }
        }


        // =========================================================
        // PDF AMOUNT ROW
        // =========================================================

        private static void AddAmountRow(
            TableDescriptor table,
            string label,
            decimal amount,
            bool bold = false)
        {
            var left =
                table.Cell()
                    .PaddingVertical(5);


            var right =
                table.Cell()
                    .PaddingVertical(5)
                    .AlignRight();


            if (bold)
            {
                left
                    .Text(label)
                    .FontFamily("Lato")
                    .Bold()
                    .FontSize(10);


                right
                    .Text(
                        $"BDT {amount:N2}")
                    .FontFamily("Lato")
                    .Bold()
                    .FontSize(10);
            }
            else
            {
                left
                    .Text(label)
                    .FontFamily("Lato")
                    .FontSize(10);


                right
                    .Text(
                        $"BDT {amount:N2}")
                    .FontFamily("Lato")
                    .FontSize(10);
            }
        }
    }
}