
using System;
using System.IO;
using System.Security.Claims;
using LabSync.Areas.Receptionist.Models;
using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.Receptionist.Controllers
{
    [Area("Receptionist")]
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX - ONLY APPROVED REPORTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? appointmentId)
        {
            appointmentId =
                string.IsNullOrWhiteSpace(appointmentId)
                    ? null
                    : appointmentId.Trim();

            var reports =
                await _context.Database
                    .SqlQueryRaw<
                        LabSync.Areas.Receptionist.Models.ReportListRowViewModel>(
                        """
                        SELECT
                            r.Id AS Id,
                            r.AppointmentId AS AppointmentId,

                            a.AppointmentCode AS AppointmentCode,
                            a.AppointmentDate AS AppointmentDate,
                            a.AppointmentTime AS AppointmentTime,

                            p.PatientCode AS PatientCode,

                            CONCAT(
                                p.FirstName,
                                ' ',
                                p.LastName
                            ) AS PatientName,

                            p.Phone AS Phone,

                            r.GeneratedBy AS GeneratedBy,
                            gu.Email AS GeneratedByEmail,

                            r.GeneratedDate AS GeneratedDate,
                            r.PdfPath AS PdfPath,

                            r.ApprovalStatusId AS ApprovalStatusId,
                            ras.StatusName AS ApprovalStatusName,

                            r.ApprovedBy AS ApprovedBy,
                            au.Email AS ApprovedByEmail,
                            r.ApprovalDate AS ApprovalDate,

                            r.IsDelivered AS IsDelivered,
                            r.DeliveredAt AS DeliveredAt,
                            r.DeliveredBy AS DeliveredBy

                        FROM Reports r

                        INNER JOIN Appointments a
                            ON a.Id = r.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id = a.PatientId

                        INNER JOIN Users gu
                            ON gu.Id = r.GeneratedBy

                        LEFT JOIN Users au
                            ON au.Id = r.ApprovedBy

                        INNER JOIN ReportApprovalStatuses ras
                            ON ras.Id = r.ApprovalStatusId

                        WHERE
                            r.ApprovalStatusId = 2

                            AND
                            (
                                NULLIF({0}, '') IS NULL

                                OR UPPER(
                                    ISNULL(
                                        a.AppointmentCode,
                                        ''
                                    )
                                )
                                LIKE
                                '%' + UPPER({0}) + '%'
                            )

                        ORDER BY
                            r.ApprovalDate DESC,
                            r.Id DESC
                        """,
                        appointmentId ?? ""
                    )
                    .ToListAsync();

            ViewBag.AppointmentId =
                appointmentId ?? "";

            ViewData["ActiveMenu"] = "Reports";

            return View("Index", reports);
        }

        // =========================================================
        // MARK REPORT AS DELIVERED
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered(
            int appointmentId)
        {
            if (appointmentId <= 0)
            {
                TempData["Error"] =
                    "Invalid appointment.";

                return RedirectToAction(nameof(Index));
            }

            var report =
                await _context.Reports
                    .FirstOrDefaultAsync(r =>
                        r.AppointmentId == appointmentId &&
                        r.ApprovalStatusId == 2);

            if (report == null)
            {
                TempData["Error"] =
                    "Approved report not found.";

                return RedirectToAction(nameof(Index));
            }

            if (report.IsDelivered)
            {
                TempData["Info"] =
                    "This report has already been delivered.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        appointmentId =
                            await GetAppointmentCode(appointmentId)
                    });
            }

            // Get logged-in user's ID
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                    userIdClaim,
                    out int deliveredById))
            {
                TempData["Error"] =
                    "Unable to identify the logged-in user ID.";

                return RedirectToAction(nameof(Index));
            }

            report.IsDelivered = true;
            report.DeliveredAt = DateTime.Now;
            report.DeliveredBy = deliveredById;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Report marked as Delivered successfully.";

            var appointmentCode =
                await GetAppointmentCode(appointmentId);

            return RedirectToAction(
                nameof(Index),
                new
                {
                    appointmentId = appointmentCode
                });
        }

        // =========================================================
        // GET APPOINTMENT CODE
        // =========================================================

        private async Task<string?> GetAppointmentCode(
            int appointmentId)
        {
            return await _context.Appointments
                .Where(a => a.Id == appointmentId)
                .Select(a => a.AppointmentCode)
                .FirstOrDefaultAsync();
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Invalid report.";

                return RedirectToAction(nameof(Index));
            }

            var report =
                await _context.Reports
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (report == null)
            {
                TempData["Error"] = "Report not found.";

                return RedirectToAction(nameof(Index));
            }

            var appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(
                        x => x.Id == report.AppointmentId);

            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";

                return RedirectToAction(nameof(Index));
            }

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        x => x.Id == appointment.PatientId);

            var tests =
                await _context.Database
                    .SqlQueryRaw<ReportTestDetailsViewModel>(
                        """
                        SELECT
                            at.Id AS AppointmentTestId,

                            t.TestCode AS TestCode,
                            t.TestName AS TestName,
                            t.Price AS Price,
                            t.TurnAroundTime AS TurnAroundTime,
                            t.Unit AS Unit,

                            rr.ReferenceRange AS ReferenceRange,
                            rr.Description AS ReferenceRangeDescription,

                            tr.ResultValue AS ResultValue,
                            tr.ResultInterpretation AS ResultInterpretation,
                            tr.Remarks AS Remarks,

                            rs.StatusName AS ResultStatusName,
                            tr.ResultDate AS ResultDate

                        FROM AppointmentTests at

                        INNER JOIN Tests t
                            ON t.Id = at.TestId

                        LEFT JOIN ReferenceRanges rr
                            ON rr.Id = t.ReferenceRangeId

                        LEFT JOIN TestResults tr
                            ON tr.AppointmentTestId = at.Id

                        LEFT JOIN ResultStatuses rs
                            ON rs.Id = tr.ResultStatusId

                        WHERE
                            at.AppointmentId = {0}

                        ORDER BY
                            at.Id ASC
                        """,
                        report.AppointmentId
                    )
                    .ToListAsync();

            var generatedByUser =
                await _context.Users
                    .FirstOrDefaultAsync(
                        x => x.Id == report.GeneratedBy);

            User? approvedByUser = null;

            if (report.ApprovedBy.HasValue)
            {
                approvedByUser =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            x => x.Id == report.ApprovedBy.Value);
            }

            var approvalStatus =
                await _context.Database
                    .SqlQueryRaw<ReportApprovalStatusViewModel>(
                        """
                        SELECT TOP 1
                            Id,
                            StatusName
                        FROM ReportApprovalStatuses
                        WHERE Id = {0}
                        """,
                        report.ApprovalStatusId
                    )
                    .FirstOrDefaultAsync();

            ViewBag.PaymentStatus = "Unpaid";

            ViewBag.Appointment = appointment;
            ViewBag.Patient = patient;
            ViewBag.Tests = tests;

            ViewBag.GeneratedByUser = generatedByUser;
            ViewBag.ApprovedByUser = approvedByUser;

            ViewBag.ApprovalStatus =
                approvalStatus?.StatusName ?? "Pending";

            ViewData["ActiveMenu"] = "Reports";

            return View("Details", report);
        }

        // =========================================================
        // VIEW PDF
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ViewPdf(
            int id)
        {
            var report =
                await _context.Reports
                    .FirstOrDefaultAsync(
                        x => x.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(report.PdfPath))
            {
                return NotFound();
            }

            var fileName =
                Path.GetFileName(
                    report.PdfPath.Replace("\\", "/"));

            var webRootPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot");

            var filePath =
                Path.Combine(
                    webRootPath,
                    "reports",
                    fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var bytes =
                await System.IO.File.ReadAllBytesAsync(
                    filePath);

            return File(bytes, "application/pdf");
        }

        // =========================================================
        // APPROVAL STATUS VIEWMODEL
        // =========================================================

        private class ReportApprovalStatusViewModel
        {
            public int Id { get; set; }

            public string StatusName { get; set; }
                = string.Empty;
        }
    }
}