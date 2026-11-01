using Labsync.Models.Data;
using LabSync.Areas.LabTechnician.Models;
using LabSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LabSync.Controllers
{
    [Authorize]
    public class AdminReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;


        public AdminReportsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? appointmentId,
            int? approvalStatusId)
        {
            appointmentId =
                string.IsNullOrWhiteSpace(
                    appointmentId)
                    ? null
                    : appointmentId.Trim();


            var reports =
                await _context.Database
                    .SqlQueryRaw<ReportListRowViewModel>(
                        """
                        SELECT

                            r.Id AS Id,

                            r.AppointmentId
                                AS AppointmentId,

                            a.AppointmentCode
                                AS AppointmentCode,

                            a.AppointmentDate
                                AS AppointmentDate,

                            a.AppointmentTime
                                AS AppointmentTime,


                            p.PatientCode
                                AS PatientCode,

                            CONCAT(
                                p.FirstName,
                                ' ',
                                p.LastName
                            ) AS PatientName,

                            p.Phone
                                AS Phone,


                            r.GeneratedBy
                                AS GeneratedBy,

                            gu.Email
                                AS GeneratedByEmail,

                            r.GeneratedDate
                                AS GeneratedDate,

                            r.PdfPath
                                AS PdfPath,


                            r.ApprovalStatusId
                                AS ApprovalStatusId,

                            ras.StatusName
                                AS ApprovalStatusName,

                            r.ApprovedBy
                                AS ApprovedBy,

                            au.Email
                                AS ApprovedByEmail,

                            r.ApprovalDate
                                AS ApprovalDate,


                            r.IsDelivered
                                AS IsDelivered,

                            r.DeliveredAt
                                AS DeliveredAt,

                            r.DeliveredBy
                                AS DeliveredBy

                        FROM Reports r

                        INNER JOIN Appointments a
                            ON a.Id =
                               r.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id =
                               a.PatientId

                        INNER JOIN Users gu
                            ON gu.Id =
                               r.GeneratedBy

                        LEFT JOIN Users au
                            ON au.Id =
                               r.ApprovedBy

                        INNER JOIN ReportApprovalStatuses ras
                            ON ras.Id =
                               r.ApprovalStatusId

                        WHERE

                            (
                                NULLIF(
                                    {0},
                                    ''
                                ) IS NULL

                                OR UPPER(
                                    ISNULL(
                                        a.AppointmentCode,
                                        ''
                                    )
                                )
                                LIKE
                                '%' +
                                UPPER({0}) +
                                '%'
                            )

                            AND

                            (
                                {1} IS NULL

                                OR r.ApprovalStatusId = {1}
                            )

                        ORDER BY
                            r.GeneratedDate DESC,
                            r.Id DESC
                        """,

                        appointmentId ?? "",

                        approvalStatusId
                    )
                    .ToListAsync();


            ViewBag.AppointmentId =
                appointmentId ?? "";


            ViewBag.ApprovalStatusId =
                approvalStatusId;


            ViewData["ActiveMenu"] =
                "Reports";


            return View(
                "~/Views/Reports/Index.cshtml",
                reports
            );
        }


        // =========================================================
        // VIEW PDF
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ViewPdf(
            int id)
        {
            var report =
                await GetReportAsync(id);


            if (report == null)
            {
                return NotFound();
            }


            var filePath =
                GetPhysicalPdfPath(
                    report.PdfPath
                );


            if (filePath == null)
            {
                return NotFound();
            }


            var bytes =
                await System.IO.File.ReadAllBytesAsync(
                    filePath
                );


            return File(
                bytes,
                "application/pdf"
            );
        }


        // =========================================================
        // DOWNLOAD PDF
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(
            int id)
        {
            var report =
                await GetReportAsync(id);


            if (report == null)
            {
                return NotFound();
            }


            var filePath =
                GetPhysicalPdfPath(
                    report.PdfPath
                );


            if (filePath == null)
            {
                return NotFound();
            }


            var fileName =
                Path.GetFileName(
                    filePath
                );


            var bytes =
                await System.IO.File.ReadAllBytesAsync(
                    filePath
                );


            return File(
                bytes,
                "application/pdf",
                fileName
            );
        }


        // =========================================================
        // APPROVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id)
        {
            var report =
                await GetReportAsync(id);


            if (report == null)
            {
                TempData["Error"] =
                    "Report not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            if (report.ApprovalStatusId == 2)
            {
                TempData["Error"] =
                    "This report is already approved.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var adminUser =
                await FindCurrentUserAsync();


            if (adminUser == null)
            {
                TempData["Error"] =
                    "Your Admin account is not linked with the Users table.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            await _context.Database
                .ExecuteSqlRawAsync(
                    """
                    UPDATE Reports

                    SET
                        ApprovalStatusId = 2,

                        ApprovedBy = {0},

                        ApprovalDate = {1}

                    WHERE
                        Id = {2}
                    """,

                    adminUser.Id,

                    DateTime.Now,

                    id
                );


            TempData["Success"] =
                "Report approved successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // REJECT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id)
        {
            var report =
                await GetReportAsync(id);


            if (report == null)
            {
                TempData["Error"] =
                    "Report not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var adminUser =
                await FindCurrentUserAsync();


            if (adminUser == null)
            {
                TempData["Error"] =
                    "Your Admin account is not linked with the Users table.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            await _context.Database
                .ExecuteSqlRawAsync(
                    """
                    UPDATE Reports

                    SET
                        ApprovalStatusId = 3,

                        ApprovedBy = {0},

                        ApprovalDate = {1}

                    WHERE
                        Id = {2}
                    """,

                    adminUser.Id,

                    DateTime.Now,

                    id
                );


            TempData["Success"] =
                "Report rejected successfully.";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =========================================================
        // GET REPORT
        // =========================================================

        private async Task<ReportListRowViewModel?>
            GetReportAsync(
                int id)
        {
            return await _context.Database
                .SqlQueryRaw<ReportListRowViewModel>(
                    """
                    SELECT TOP 1

                        r.Id AS Id,

                        r.AppointmentId
                            AS AppointmentId,

                        a.AppointmentCode
                            AS AppointmentCode,

                        a.AppointmentDate
                            AS AppointmentDate,

                        a.AppointmentTime
                            AS AppointmentTime,

                        p.PatientCode
                            AS PatientCode,

                        CONCAT(
                            p.FirstName,
                            ' ',
                            p.LastName
                        ) AS PatientName,

                        p.Phone
                            AS Phone,

                        r.GeneratedBy
                            AS GeneratedBy,

                        gu.Email
                            AS GeneratedByEmail,

                        r.GeneratedDate
                            AS GeneratedDate,

                        r.PdfPath
                            AS PdfPath,

                        r.ApprovalStatusId
                            AS ApprovalStatusId,

                        ras.StatusName
                            AS ApprovalStatusName,

                        r.ApprovedBy
                            AS ApprovedBy,

                        au.Email
                            AS ApprovedByEmail,

                        r.ApprovalDate
                            AS ApprovalDate,

                        r.IsDelivered
                            AS IsDelivered,

                        r.DeliveredAt
                            AS DeliveredAt,

                        r.DeliveredBy
                            AS DeliveredBy

                    FROM Reports r

                    INNER JOIN Appointments a
                        ON a.Id =
                           r.AppointmentId

                    INNER JOIN Patients p
                        ON p.Id =
                           a.PatientId

                    INNER JOIN Users gu
                        ON gu.Id =
                           r.GeneratedBy

                    LEFT JOIN Users au
                        ON au.Id =
                           r.ApprovedBy

                    INNER JOIN ReportApprovalStatuses ras
                        ON ras.Id =
                           r.ApprovalStatusId

                    WHERE
                        r.Id = {0}
                    """,

                    id
                )
                .FirstOrDefaultAsync();
        }


        // =========================================================
        // PDF PHYSICAL PATH
        // =========================================================

        private string? GetPhysicalPdfPath(
            string? pdfPath)
        {
            if (
                string.IsNullOrWhiteSpace(
                    pdfPath)
            )
            {
                return null;
            }


            var fileName =
                Path.GetFileName(
                    pdfPath.Replace(
                        "\\",
                        "/"
                    )
                );


            if (
                string.IsNullOrWhiteSpace(
                    fileName)
            )
            {
                return null;
            }


            var filePath =
                Path.Combine(
                    _environment.WebRootPath,
                    "reports",
                    fileName
                );


            return System.IO.File.Exists(
                filePath
            )
                ? filePath
                : null;
        }


        // =========================================================
        // CURRENT USER
        // =========================================================

        private async Task<LabUserLookupViewModel?>
            FindCurrentUserAsync()
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            var userEmail =
                User.FindFirstValue(
                    ClaimTypes.Email
                );


            var userName =
                User.Identity?.Name;


            LabUserLookupViewModel? user =
                null;


            if (
                !string.IsNullOrWhiteSpace(
                    userIdClaim
                )
                &&
                int.TryParse(
                    userIdClaim,
                    out int userId
                )
            )
            {
                user =
                    await _context.Database
                        .SqlQueryRaw<LabUserLookupViewModel>(
                            """
                            SELECT TOP 1
                                Id,
                                Email
                            FROM Users
                            WHERE Id = {0}
                            """,
                            userId
                        )
                        .FirstOrDefaultAsync();
            }


            if (
                user == null
                &&
                !string.IsNullOrWhiteSpace(
                    userEmail
                )
            )
            {
                user =
                    await _context.Database
                        .SqlQueryRaw<LabUserLookupViewModel>(
                            """
                            SELECT TOP 1
                                Id,
                                Email
                            FROM Users
                            WHERE LOWER(
                                LTRIM(
                                    RTRIM(
                                        ISNULL(
                                            Email,
                                            ''
                                        )
                                    )
                                )
                            )
                            =
                            LOWER(
                                LTRIM(
                                    RTRIM({0})
                                )
                            )
                            """,
                            userEmail
                        )
                        .FirstOrDefaultAsync();
            }


            if (
                user == null
                &&
                !string.IsNullOrWhiteSpace(
                    userName
                )
            )
            {
                user =
                    await _context.Database
                        .SqlQueryRaw<LabUserLookupViewModel>(
                            """
                            SELECT TOP 1
                                Id,
                                Email
                            FROM Users
                            WHERE LOWER(
                                LTRIM(
                                    RTRIM(
                                        ISNULL(
                                            Email,
                                            ''
                                        )
                                    )
                                )
                            )
                            =
                            LOWER(
                                LTRIM(
                                    RTRIM({0})
                                )
                            )
                            """,
                            userName
                        )
                        .FirstOrDefaultAsync();
            }


            return user;
        }
    }
}