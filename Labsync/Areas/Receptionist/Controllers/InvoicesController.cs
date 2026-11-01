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
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? invoiceId,
            DateTime? invoiceDate)
        {
            invoiceId = invoiceId?.Trim();

            IQueryable<Invoice> query =
                _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment);

            if (!string.IsNullOrWhiteSpace(invoiceId))
            {
                if (int.TryParse(invoiceId, out int numericId))
                {
                    query = query.Where(i =>
                        i.Id == numericId ||
                        (
                            i.InvoiceNumber != null &&
                            i.InvoiceNumber.Contains(invoiceId)
                        ));
                }
                else
                {
                    query = query.Where(i =>
                        i.InvoiceNumber != null &&
                        i.InvoiceNumber.Contains(invoiceId));
                }
            }

            if (invoiceDate.HasValue)
            {
                var selectedDate = invoiceDate.Value.Date;
                var nextDate = selectedDate.AddDays(1);

                query = query.Where(i =>
                    i.InvoiceDate.HasValue &&
                    i.InvoiceDate.Value >= selectedDate &&
                    i.InvoiceDate.Value < nextDate);
            }

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            ViewBag.InvoiceId = invoiceId ?? "";
            ViewBag.InvoiceDate =
                invoiceDate?.ToString("yyyy-MM-dd") ?? "";

            return View(invoices);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create(string? appointmentId)
        {
            var invoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                PaidAmount = 0m,
                DueAmount = 0m
            };

            ViewBag.InitialAppointmentId = appointmentId ?? "";

            if (!string.IsNullOrWhiteSpace(appointmentId))
            {
                var appointment =
                    await FindAppointmentEntity(appointmentId);

                if (appointment != null)
                {
                    invoice.AppointmentId = appointment.Id;
                    invoice.PatientId = appointment.PatientId;
                }
            }

            return View(invoice);
        }

        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            ModelState.Remove(nameof(invoice.InvoiceNumber));
            ModelState.Remove(nameof(invoice.Subtotal));
            ModelState.Remove(nameof(invoice.NetAmount));
            ModelState.Remove(nameof(invoice.PaidAmount));
            ModelState.Remove(nameof(invoice.DueAmount));
            ModelState.Remove(nameof(invoice.PaymentStatusId));
            ModelState.Remove(nameof(invoice.InvoiceDate));

            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a =>
                    a.Id == invoice.AppointmentId);

            if (appointment == null)
            {
                ModelState.AddModelError(
                    "AppointmentId",
                    "Invalid appointment.");

                return View(invoice);
            }

            if (appointment.Patient == null)
            {
                ModelState.AddModelError(
                    "",
                    "Patient information was not found for this appointment.");

                return View(invoice);
            }

            var existingInvoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>
                    i.AppointmentId == appointment.Id);

            if (existingInvoice != null)
            {
                TempData["Error"] =
                    $"Invoice already exists for appointment " +
                    $"{appointment.AppointmentCode ?? appointment.Id.ToString()}.";

                return RedirectToAction(nameof(Index));
            }
            // Appointment to Test //
            var appointmentTests = await _context.Database
                .SqlQueryRaw<AppointmentTestLookupRow>(
                    @"
                    SELECT
                        t.Id AS TestId,
                        t.TestCode AS TestCode,
                        t.TestName AS TestName,
                        t.Price AS Price,
                        t.Unit AS Unit,
                        t.TurnAroundTime AS TurnAroundTime
                    FROM AppointmentTests at
                    INNER JOIN Tests t
                        ON at.TestId = t.Id
                    WHERE at.AppointmentId = {0}
                    ORDER BY t.TestName
                    ",
                    appointment.Id)
                .ToListAsync();
                              //
            if (!appointmentTests.Any())
            {
                ModelState.AddModelError(
                    "",
                    "No tests were found for this appointment.");

                return View(invoice);
            }

            decimal discountPercent = invoice.Discount ?? 0m;
            decimal taxPercent = invoice.Tax ?? 0m;

            if (discountPercent < 0 || discountPercent > 100)
            {
                ModelState.AddModelError(
                    "Discount",
                    "Discount must be between 0 and 100 percent.");

                return View(invoice);
            }

            if (taxPercent < 0 || taxPercent > 100)
            {
                ModelState.AddModelError(
                    "Tax",
                    "Tax must be between 0 and 100 percent.");

                return View(invoice);
            }
            // Calculation //
            decimal subtotal =
                appointmentTests.Sum(x => x.Price);

            decimal discountAmount =
                subtotal * discountPercent / 100m;

            decimal afterDiscount =
                subtotal - discountAmount;

            decimal taxAmount =
                afterDiscount * taxPercent / 100m;

            decimal netAmount =
                afterDiscount + taxAmount;

            decimal paidAmount = 0m;
            decimal dueAmount = netAmount;

            int paymentStatusId = 0;

            var paymentStatus =
                await _context.PaymentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.StatusName != null &&
                        (
                            p.StatusName.ToLower() == "unpaid" ||
                            p.StatusName.ToLower() == "due" ||
                            p.StatusName.ToLower() == "pending"
                        ));

            if (paymentStatus != null)
            {
                paymentStatusId = paymentStatus.Id;
            }
            else
            {
                var firstStatus =
                    await _context.PaymentStatuses
                        .AsNoTracking()
                        .OrderBy(p => p.Id)
                        .FirstOrDefaultAsync();

                if (firstStatus != null)
                    paymentStatusId = firstStatus.Id;
            }

            if (paymentStatusId == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Payment status configuration was not found.");

                return View(invoice);
            }

           
            // SEQUENTIAL INVOICE NUMBER
           
            // Exiting Number
            var existingInvoiceNumbers =
                await _context.Invoices
                    .AsNoTracking()
                    .Where(i =>
                        i.InvoiceNumber != null &&
                        i.InvoiceNumber.StartsWith("INV"))
                    .Select(i => i.InvoiceNumber!)
                    .ToListAsync();
            // Max Number
            int maxInvoiceNumber = 0;

            foreach (var code in existingInvoiceNumbers)
            {
                if (code.Length <= 3)
                    continue;

                var numberPart = code.Substring(3);

                if (int.TryParse(numberPart, out int number) &&
                    number > maxInvoiceNumber)
                {
                    maxInvoiceNumber = number;
                }
            }
            // Increment 
            string invoiceNumber =
                $"INV{maxInvoiceNumber + 1}";
            //
            var newInvoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                PatientId = appointment.PatientId,
                AppointmentId = appointment.Id,
                Subtotal = subtotal,
                Discount = discountAmount,
                Tax = taxAmount,
                NetAmount = netAmount,
                PaidAmount = paidAmount,
                DueAmount = dueAmount,
                PaymentStatusId = paymentStatusId,
                InvoiceDate = DateTime.Now
            };

            _context.Invoices.Add(newInvoice);

            await _context.SaveChangesAsync();

            foreach (var item in appointmentTests)
            {
                var invoiceItem = new InvoiceItem
                {
                    InvoiceId = newInvoice.Id,
                    TestId = item.TestId,
                    Quantity = 1,
                    UnitPrice = item.Price
                };

                _context.InvoiceItems.Add(invoiceItem);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Invoice created successfully.";

            return RedirectToAction(
                nameof(GeneratePdf),
                new { id = newInvoice.Id });
        }

        // =====================================================
        // FIND APPOINTMENT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> FindAppointment(
            string? appointmentId)
        {
            if (string.IsNullOrWhiteSpace(appointmentId))
            {
                return Json(new
                {
                    success = false,
                    message = "Appointment ID is required."
                });
            }

            appointmentId = appointmentId.Trim();

            var appointment =
                await FindAppointmentEntity(appointmentId);

            if (appointment == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        $"Appointment '{appointmentId}' was not found."
                });
            }

            var patient = appointment.Patient;

            if (patient == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Patient information was not found for this appointment."
                });
            }

            var existingInvoice =
                await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i =>
                        i.AppointmentId == appointment.Id);

            if (existingInvoice != null)
            {
                return Json(new
                {
                    success = false,
                    duplicateInvoice = true,
                    invoiceId = existingInvoice.Id,
                    invoiceNumber = existingInvoice.InvoiceNumber,
                    message =
                        "An invoice already exists for this appointment."
                });
            }

            var tests =
                await _context.Database
                    .SqlQueryRaw<AppointmentTestLookupRow>(
                        @"
                        SELECT
                            t.Id AS TestId,
                            t.TestCode AS TestCode,
                            t.TestName AS TestName,
                            t.Price AS Price,
                            t.Unit AS Unit,
                            t.TurnAroundTime AS TurnAroundTime
                        FROM AppointmentTests at
                        INNER JOIN Tests t
                            ON at.TestId = t.Id
                        WHERE at.AppointmentId = {0}
                        ORDER BY t.TestName
                        ",
                        appointment.Id)
                    .ToListAsync();

            if (!tests.Any())
            {
                return Json(new
                {
                    success = false,
                    message =
                        "No tests are assigned to this appointment."
                });
            }

            return Json(new
            {
                success = true,

                appointmentId = appointment.Id,
                appointmentCode = appointment.AppointmentCode,

                appointmentDate =
                    appointment.AppointmentDate
                        .ToString("dd MMM yyyy"),

                appointmentTime =
                    DateTime.Today
                        .Add(appointment.AppointmentTime)
                        .ToString("hh:mm tt"),

                patientId = patient.Id,
                patientCode = patient.PatientCode,

                patientName =
                    $"{patient.FirstName} {patient.LastName}",

                phone =
                    string.IsNullOrWhiteSpace(patient.Phone)
                        ? "N/A"
                        : patient.Phone,

                email =
                    string.IsNullOrWhiteSpace(patient.Email)
                        ? "N/A"
                        : patient.Email,

                gender =
                    string.IsNullOrWhiteSpace(patient.Gender)
                        ? "N/A"
                        : patient.Gender,

                dateOfBirth =
                    patient.DateOfBirth.HasValue
                        ? patient.DateOfBirth.Value
                            .ToString("dd MMM yyyy")
                        : "N/A",

                address =
                    string.IsNullOrWhiteSpace(patient.Address)
                        ? "N/A"
                        : patient.Address,

                tests = tests.Select(t => new
                {
                    testId = t.TestId,
                    testCode = t.TestCode,
                    testName = t.TestName,
                    price = t.Price,
                    unit = t.Unit,
                    turnAroundTime = t.TurnAroundTime
                })
            });
        }

        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            var paymentStatus =
                await _context.PaymentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.Id == invoice.PaymentStatusId);

            ViewBag.PaymentStatusName =
                paymentStatus?.StatusName ?? "N/A";

            var invoiceItems =
                await _context.InvoiceItems
                    .AsNoTracking()
                    .Where(x => x.InvoiceId == id)
                    .ToListAsync();

            var testIds =
                invoiceItems
                    .Select(x => x.TestId)
                    .Distinct()
                    .ToList();

            var tests =
                await _context.Tests
                    .AsNoTracking()
                    .Where(t => testIds.Contains(t.Id))
                    .ToListAsync();

            ViewBag.InvoiceItems = invoiceItems;
            ViewBag.Tests = tests;

            return View(invoice);
        }

        // =====================================================
        // EDIT - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            ViewBag.PaymentStatuses =
                await _context.PaymentStatuses
                    .AsNoTracking()
                    .OrderBy(p => p.Id)
                    .ToListAsync();

            return View(invoice);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Invoice invoice)
        {
            if (id != invoice.Id)
                return NotFound();

            ModelState.Remove(nameof(invoice.InvoiceNumber));
            ModelState.Remove(nameof(invoice.Subtotal));
            ModelState.Remove(nameof(invoice.NetAmount));
            ModelState.Remove(nameof(invoice.DueAmount));
            ModelState.Remove(nameof(invoice.InvoiceDate));
            ModelState.Remove(nameof(invoice.PatientId));
            ModelState.Remove(nameof(invoice.AppointmentId));

            var existingInvoice =
                await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(i => i.Id == id);

            if (existingInvoice == null)
                return NotFound();

            decimal discount =
                invoice.Discount ?? 0m;

            decimal tax =
                invoice.Tax ?? 0m;

            decimal paidAmount =
                invoice.PaidAmount ?? 0m;

            if (discount < 0)
            {
                ModelState.AddModelError(
                    nameof(invoice.Discount),
                    "Discount cannot be negative.");
            }

            if (tax < 0)
            {
                ModelState.AddModelError(
                    nameof(invoice.Tax),
                    "Tax cannot be negative.");
            }

            if (paidAmount < 0)
            {
                ModelState.AddModelError(
                    nameof(invoice.PaidAmount),
                    "Paid amount cannot be negative.");
            }

            var invoiceItems =
                await _context.InvoiceItems
                    .AsNoTracking()
                    .Where(x => x.InvoiceId == id)
                    .ToListAsync();

            decimal subtotal =
                invoiceItems.Sum(x =>
                    Convert.ToDecimal(x.TotalPrice));

            decimal netAmount =
                subtotal - discount + tax;

            if (netAmount < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Net amount cannot be negative.");
            }

            if (paidAmount > netAmount)
            {
                ModelState.AddModelError(
                    nameof(invoice.PaidAmount),
                    "Paid amount cannot be greater than net amount.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PaymentStatuses =
                    await _context.PaymentStatuses
                        .AsNoTracking()
                        .OrderBy(p => p.Id)
                        .ToListAsync();

                return View(invoice);
            }

            decimal dueAmount =
                netAmount - paidAmount;

            existingInvoice.Discount = discount;
            existingInvoice.Tax = tax;
            existingInvoice.Subtotal = subtotal;
            existingInvoice.NetAmount = netAmount;
            existingInvoice.PaidAmount = paidAmount;
            existingInvoice.DueAmount = dueAmount;
            existingInvoice.PaymentStatusId =
                invoice.PaymentStatusId;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Invoice updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = existingInvoice.Id });
        }

        // =====================================================
        // DELETE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice =
                await _context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            return View(invoice);
        }

     
        // GENERATE PDF

        [HttpGet]
        public async Task<IActionResult> GeneratePdf(int id)
        {
            var invoice =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.Patient)
                    .Include(i => i.Appointment)
                    .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            var invoiceItems =
                await _context.InvoiceItems
                    .AsNoTracking()
                    .Where(x => x.InvoiceId == id)
                    .ToListAsync();

            var testIds =
                invoiceItems
                    .Select(x => x.TestId)
                    .Distinct()
                    .ToList();

            var tests =
                await _context.Tests
                    .AsNoTracking()
                    .Where(t => testIds.Contains(t.Id))
                    .ToListAsync();

            var pdfItems =
                invoiceItems.Select(item =>
                {
                    var test =
                        tests.FirstOrDefault(t =>
                            t.Id == item.TestId);

                    return new InvoiceTestPdfViewModel
                    {
                        TestCode =
                            test?.TestCode ?? "N/A",

                        TestName =
                            test?.TestName ?? "N/A",

                        Quantity =
                            Convert.ToInt32(
                                item.Quantity).ToString(),

                        UnitPrice =
                            $"BDT {Convert.ToDecimal(item.UnitPrice):N2}",

                        TotalPrice =
                            $"BDT {Convert.ToDecimal(item.TotalPrice):N2}"
                    };
                }).ToList();

            QuestPDF.Settings.License =
                LicenseType.Community;

            var document =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(35);

                        page.DefaultTextStyle(
                            TextStyle.Default
                                .FontFamily("Lato")
                                .FontSize(10));

                        // HEADER
                        page.Header()
                            .Column(header =>
                            {
                                header.Item()
                                    .Text("LabSync")
                                    .FontSize(28)
                                    .Bold()
                                    .FontColor("#8B5CF6");

                                header.Item()
                                    .PaddingTop(2)
                                    .Text(
                                        "Laboratory Management System")
                                    .FontSize(10)
                                    .FontColor("#666666");

                                header.Item()
                                    .PaddingTop(10)
                                    .LineHorizontal(1)
                                    .LineColor("#E8E5EF");
                            });

                        // CONTENT
                        page.Content()
                            .PaddingTop(20)
                            .Column(content =>
                            {
                                content.Spacing(12);

                                content.Item()
                                    .AlignCenter()
                                    .Text("INVOICE")
                                    .FontSize(22)
                                    .Bold()
                                    .FontColor("#18213A");

                                content.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Column(left =>
                                            {
                                                left.Item()
                                                    .Text(
                                                        $"Invoice No: {invoice.InvoiceNumber}")
                                                    .Bold();

                                                left.Item()
                                                    .PaddingTop(4)
                                                    .Text(
                                                        $"Invoice Date: {(invoice.InvoiceDate.HasValue
                                                            ? invoice.InvoiceDate.Value.ToString("dd MMM yyyy hh:mm tt")
                                                            : "N/A")}");

                                                left.Item()
                                                    .PaddingTop(4)
                                                    .Text(
                                                        $"Patient ID: {invoice.Patient?.PatientCode ?? "N/A"}");
                                            });

                                        row.RelativeItem()
                                            .AlignRight()
                                            .Column(right =>
                                            {
                                                right.Item()
                                                    .Text(
                                                        $"Appointment ID: {invoice.Appointment?.AppointmentCode ?? "N/A"}")
                                                    .Bold();

                                                right.Item()
                                                    .PaddingTop(4)
                                                    .Text(
                                                        $"Patient Name: {(invoice.Patient == null
                                                            ? "N/A"
                                                            : $"{invoice.Patient.FirstName} {invoice.Patient.LastName}")}");

                                                right.Item()
                                                    .PaddingTop(4)
                                                    .Text(
                                                        $"Phone: {invoice.Patient?.Phone ?? "N/A"}");
                                            });
                                    });

                                // TEST TABLE
                                content.Item()
                                    .PaddingTop(8)
                                    .Border(1)
                                    .BorderColor("#E8E5EF")
                                    .Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(35);
                                            columns.RelativeColumn(1.5f);
                                            columns.RelativeColumn(3.5f);
                                            columns.ConstantColumn(55);
                                            columns.ConstantColumn(85);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Background("#8B5CF6")
                                                .Padding(8)
                                                .Text("#")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#8B5CF6")
                                                .Padding(8)
                                                .Text("Test Code")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#8B5CF6")
                                                .Padding(8)
                                                .Text("Test Name")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#8B5CF6")
                                                .Padding(8)
                                                .AlignRight()
                                                .Text("Qty")
                                                .FontColor("#FFFFFF")
                                                .Bold();

                                            header.Cell()
                                                .Background("#8B5CF6")
                                                .Padding(8)
                                                .AlignRight()
                                                .Text("Amount")
                                                .FontColor("#FFFFFF")
                                                .Bold();
                                        });

                                        int serial = 1;

                                        foreach (var item in pdfItems)
                                        {
                                            table.Cell()
                                                .Padding(8)
                                                .Text(serial.ToString());

                                            table.Cell()
                                                .Padding(8)
                                                .Text(item.TestCode);

                                            table.Cell()
                                                .Padding(8)
                                                .Text(item.TestName);

                                            table.Cell()
                                                .Padding(8)
                                                .AlignRight()
                                                .Text(item.Quantity);

                                            table.Cell()
                                                .Padding(8)
                                                .AlignRight()
                                                .Text(item.TotalPrice);

                                            serial++;
                                        }
                                    });

                                // SUMMARY
                                content.Item()
                                    .PaddingTop(15)
                                    .AlignRight()
                                    .Column(summary =>
                                    {
                                        summary.Item()
                                            .Text(
                                                $"Subtotal: BDT {invoice.Subtotal:N2}");

                                        summary.Item()
                                            .PaddingTop(4)
                                            .Text(
                                                $"Discount: BDT {(invoice.Discount ?? 0):N2}");

                                        summary.Item()
                                            .PaddingTop(4)
                                            .Text(
                                                $"Tax: BDT {(invoice.Tax ?? 0):N2}");

                                        summary.Item()
                                            .PaddingTop(7)
                                            .Text(
                                                $"Net Amount: BDT {invoice.NetAmount:N2}")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor("#8B5CF6");

                                        summary.Item()
                                            .PaddingTop(4)
                                            .Text(
                                                $"Paid Amount: BDT {(invoice.PaidAmount ?? 0):N2}");

                                        summary.Item()
                                            .PaddingTop(4)
                                            .Text(
                                                $"Due Amount: BDT {invoice.DueAmount:N2}")
                                            .Bold();
                                    });

                                content.Item()
                                    .PaddingTop(22)
                                    .LineHorizontal(1)
                                    .LineColor("#E8E5EF");

                                content.Item()
                                    .PaddingTop(8)
                                    .AlignCenter()
                                    .Text(
                                        "Thank you for choosing LabSync.")
                                    .FontSize(9)
                                    .FontColor("#888888");
                            });

                        // FOOTER
                        page.Footer()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("LabSync • Invoice")
                                    .FontSize(8)
                                    .FontColor("#9993A8");
                            });
                    });
                });

            var pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf");
        }

        // =====================================================
        // FIND APPOINTMENT ENTITY
        // =====================================================

        private async Task<Appointment?> FindAppointmentEntity(
            string appointmentId)
        {
            Appointment? appointment = null;

            if (int.TryParse(
                appointmentId,
                out int numericId))
            {
                appointment =
                    await _context.Appointments
                        .AsNoTracking()
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a =>
                            a.Id == numericId);
            }

            if (appointment == null)
            {
                appointment =
                    await _context.Appointments
                        .AsNoTracking()
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a =>
                            a.AppointmentCode != null &&
                            a.AppointmentCode == appointmentId);
            }

            return appointment;
        }

        // =====================================================
        // RAW SQL MODEL
        // =====================================================

        private class AppointmentTestLookupRow
        {
            public int TestId { get; set; }

            public string? TestCode { get; set; }

            public string? TestName { get; set; }

            public decimal Price { get; set; }

            public string? Unit { get; set; }

            public int? TurnAroundTime { get; set; }
        }

        // =====================================================
        // PDF MODEL
        // =====================================================

        public class InvoiceTestPdfViewModel
        {
            public string TestCode { get; set; } = "N/A";

            public string TestName { get; set; } = "N/A";

            public string Quantity { get; set; } = "1";

            public string UnitPrice { get; set; } =
                "BDT 0.00";

            public string TotalPrice { get; set; } =
                "BDT 0.00";
        }
    }
}