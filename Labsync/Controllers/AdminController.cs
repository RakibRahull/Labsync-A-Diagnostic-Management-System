using Labsync.Models.Data;
using LabSync.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // ADMIN DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? selectedDate)
        {
            ViewData["ActiveMenu"] = "Dashboard";

            // =================================================
            // SELECTED DATE
            // =================================================

            DateTime dashboardDate =
                selectedDate?.Date ?? DateTime.Today;

            ViewBag.SelectedDate =
                dashboardDate.ToString("yyyy-MM-dd");

            ViewBag.SelectedDateDisplay =
                dashboardDate.ToString("dd MMM yyyy");

            var model =
                new AdminDashboardViewModel();


            
            // 1. TOTAL PATIENTS
           

            model.TotalPatients =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Patients
                    """
                )
                .SingleAsync();


            // 2. SELECTED DATE APPOINTMENTS COUNT
         

            model.TodayAppointments =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Appointments
                    WHERE AppointmentDate = {0}
                    """,
                    dashboardDate
                )
                .SingleAsync();


        
            // 3. PENDING REPORTS
       

            model.PendingReports =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Reports r

                    INNER JOIN ReportApprovalStatuses s
                        ON s.Id = r.ApprovalStatusId

                    WHERE LOWER(s.StatusName)
                        NOT LIKE '%approved%'
                    """
                )
                .SingleAsync();


            // 4. COMPLETED TESTS
           

            model.CompletedTests =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM TestResults tr

                    INNER JOIN ResultStatuses rs
                        ON rs.Id = tr.ResultStatusId

                    WHERE LOWER(rs.StatusName)
                        LIKE '%approved%'

                       OR LOWER(rs.StatusName)
                        LIKE '%completed%'
                    """
                )
                .SingleAsync();


         
            // 5. SELECTED DATE APPOINTMENT LIST
            

            model.TodayAppointmentsList =
                await _context.Database
                .SqlQueryRaw<TodayAppointmentViewModel>(
                    """
                    SELECT TOP 10

                        CONVERT(
                            VARCHAR(8),
                            a.AppointmentTime,
                            100
                        ) AS AppointmentTime,

                        CONCAT(
                            p.FirstName,
                            ' ',
                            p.LastName
                        ) AS PatientName,

                        ISNULL(
                            t.TestName,
                            'General Diagnostic'
                        ) AS TestName,

                        ISNULL(
                            ast.StatusName,
                            'Pending'
                        ) AS StatusName

                    FROM Appointments a

                    INNER JOIN Patients p
                        ON p.Id = a.PatientId

                    LEFT JOIN AppointmentTests at
                        ON at.AppointmentId = a.Id

                    LEFT JOIN Tests t
                        ON t.Id = at.TestId

                    LEFT JOIN AppointmentStatuses ast
                        ON ast.Id = a.AppointmentStatusId

                    WHERE a.AppointmentDate = {0}

                    ORDER BY a.AppointmentTime ASC
                    """,
                    dashboardDate
                )
                .ToListAsync();


            // =================================================
            // 6. RECENT ACTIVITY
            // =================================================

            var recentActivities =
                new List<RecentActivityViewModel>();


            // -------------------------------------------------
            // NEW PATIENTS
            // -------------------------------------------------

            var recentPatients =
                await _context.Database
                .SqlQueryRaw<RecentActivityQueryResult>(
                    """
                    SELECT TOP 5

                        'New patient registered' AS Title,

                        'Patient registration completed'
                            AS Description,

                        CreatedAt AS ActivityDate,

                        'bi-person-plus-fill' AS Icon,

                        'purple' AS IconClass

                    FROM Patients

                    WHERE CreatedAt IS NOT NULL

                    ORDER BY CreatedAt DESC
                    """
                )
                .ToListAsync();


            // -------------------------------------------------
            // REPORTS GENERATED
            // -------------------------------------------------

            var recentReports =
                await _context.Database
                .SqlQueryRaw<RecentActivityQueryResult>(
                    """
                    SELECT TOP 5

                        'Test report generated' AS Title,

                        'Laboratory report generated successfully'
                            AS Description,

                        GeneratedDate AS ActivityDate,

                        'bi-file-earmark-medical-fill' AS Icon,

                        'blue' AS IconClass

                    FROM Reports

                    WHERE GeneratedDate IS NOT NULL

                    ORDER BY GeneratedDate DESC
                    """
                )
                .ToListAsync();


            // -------------------------------------------------
            // COMBINE ACTIVITIES
            // -------------------------------------------------

            var allActivities = recentPatients
                .Concat(recentReports)
                .OrderByDescending(x => x.ActivityDate)
                .Take(5)
                .ToList();


            // -------------------------------------------------
            // CREATE VIEW MODEL
            // -------------------------------------------------

            foreach (var activity in allActivities)
            {
                recentActivities.Add(
                    new RecentActivityViewModel
                    {
                        Title = activity.Title,

                        Description = activity.Description,

                        Icon = activity.Icon,

                        IconClass = activity.IconClass,

                        TimeAgo =
                            GetTimeAgo(
                                activity.ActivityDate
                            )
                    }
                );
            }


            model.RecentActivities =
                recentActivities;


            // =================================================
            // RETURN DASHBOARD
            // =================================================

            return View(model);
        }


        // =====================================================
        // APPOINTMENT DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AppointmentDetails(
            DateTime? selectedDate,
            string? patientId)
        {
            // =================================================
            // SELECTED DATE
            // =================================================

            DateTime appointmentDate =
                selectedDate?.Date ?? DateTime.Today;


            // =================================================
            // CLEAN PATIENT ID
            // =================================================

            patientId =
                string.IsNullOrWhiteSpace(patientId)
                    ? null
                    : patientId.Trim();


            // =================================================
            // CREATE VIEW MODEL
            // =================================================

            var model =
                new AdminAppointmentDetailsViewModel
                {
                    SelectedDate = appointmentDate,

                    PatientIdSearch = patientId
                };


            // =================================================
            // DATE RANGE
            // =================================================

            DateTime nextDate =
                appointmentDate.AddDays(1);


            // =================================================
            // GET APPOINTMENTS
            // =================================================

            model.Appointments =
                await _context.Database
                .SqlQueryRaw<AdminAppointmentRowViewModel>(
                    """
                    SELECT

                        a.Id AS AppointmentId,

                        a.AppointmentCode AS AppointmentCode,

                        p.PatientCode AS PatientCode,

                        CONCAT(
                            p.FirstName,
                            ' ',
                            p.LastName
                        ) AS PatientName,

                        p.Phone AS Phone,

                        p.Gender AS Gender,

                        p.DateOfBirth AS DateOfBirth,

                        a.AppointmentTime AS AppointmentTime,

                        COALESCE(
                            STRING_AGG(
                                t.TestName,
                                ', '
                            ),
                            'General Diagnostic'
                        ) AS TestName,

                        ISNULL(
                            ast.StatusName,
                            'Pending'
                        ) AS StatusName,

                        a.Remarks AS Remarks

                    FROM Appointments a

                    INNER JOIN Patients p
                        ON p.Id = a.PatientId

                    LEFT JOIN AppointmentTests at
                        ON at.AppointmentId = a.Id

                    LEFT JOIN Tests t
                        ON t.Id = at.TestId

                    LEFT JOIN AppointmentStatuses ast
                        ON ast.Id = a.AppointmentStatusId

                    WHERE a.AppointmentDate >= {0}

                      AND a.AppointmentDate < {1}

                      AND
                      (
                          {2} IS NULL

                          OR

                          p.PatientCode LIKE '%' + {2} + '%'
                      )

                    GROUP BY

                        a.Id,

                        a.AppointmentCode,

                        p.PatientCode,

                        p.FirstName,

                        p.LastName,

                        p.Phone,

                        p.Gender,

                        p.DateOfBirth,

                        a.AppointmentTime,

                        ast.StatusName,

                        a.Remarks

                    ORDER BY
                        a.AppointmentTime ASC,

                        a.Id ASC
                    """,
                    appointmentDate,
                    nextDate,
                    patientId
                )
                .ToListAsync();


            // =================================================
            // TOTAL APPOINTMENTS
            // =================================================

            model.TotalAppointments =
                model.Appointments.Count;


            // =================================================
            // VIEWBAG DATE
            // =================================================

            ViewBag.SelectedDate =
                appointmentDate.ToString("yyyy-MM-dd");


            ViewBag.SelectedDateDisplay =
                appointmentDate.ToString("dd MMM yyyy");


            // =================================================
            // RETURN VIEW
            // =================================================

            return View(model);
        }


        // =====================================================
        // ALL RECENT ACTIVITIES
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> RecentActivity()
        {
            var recentActivities =
                new List<RecentActivityViewModel>();


            // -------------------------------------------------
            // ALL RECENT PATIENTS
            // -------------------------------------------------

            var recentPatients =
                await _context.Database
                .SqlQueryRaw<RecentActivityQueryResult>(
                    """
                    SELECT TOP 50

                        'New patient registered' AS Title,

                        'Patient registration completed'
                            AS Description,

                        CreatedAt AS ActivityDate,

                        'bi-person-plus-fill' AS Icon,

                        'purple' AS IconClass

                    FROM Patients

                    WHERE CreatedAt IS NOT NULL

                    ORDER BY CreatedAt DESC
                    """
                )
                .ToListAsync();


            // -------------------------------------------------
            // ALL RECENT REPORTS
            // -------------------------------------------------

            var recentReports =
                await _context.Database
                .SqlQueryRaw<RecentActivityQueryResult>(
                    """
                    SELECT TOP 50

                        'Test report generated' AS Title,

                        'Laboratory report generated successfully'
                            AS Description,

                        GeneratedDate AS ActivityDate,

                        'bi-file-earmark-medical-fill' AS Icon,

                        'blue' AS IconClass

                    FROM Reports

                    WHERE GeneratedDate IS NOT NULL

                    ORDER BY GeneratedDate DESC
                    """
                )
                .ToListAsync();


            // -------------------------------------------------
            // COMBINE
            // -------------------------------------------------

            var allActivities = recentPatients
                .Concat(recentReports)
                .OrderByDescending(x => x.ActivityDate)
                .ToList();


            // -------------------------------------------------
            // CREATE VIEW MODEL
            // -------------------------------------------------

            foreach (var activity in allActivities)
            {
                recentActivities.Add(
                    new RecentActivityViewModel
                    {
                        Title = activity.Title,

                        Description = activity.Description,

                        Icon = activity.Icon,

                        IconClass = activity.IconClass,

                        TimeAgo =
                            GetTimeAgo(
                                activity.ActivityDate
                            )
                    }
                );
            }


            return View(recentActivities);
        }


        // =====================================================
        // ANALYTICS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            ViewData["ActiveMenu"] = "Analytics";

            var model =
                new AdminAnalyticsViewModel();


            // -------------------------------------------------
            // TOTAL PATIENTS
            // -------------------------------------------------

            model.TotalPatients =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Patients
                    """
                )
                .SingleAsync();


            // -------------------------------------------------
            // TOTAL APPOINTMENTS
            // -------------------------------------------------

            model.TotalAppointments =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Appointments
                    """
                )
                .SingleAsync();


            // -------------------------------------------------
            // TOTAL REPORTS
            // -------------------------------------------------

            model.TotalReports =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Reports
                    """
                )
                .SingleAsync();


            // -------------------------------------------------
            // APPROVED REPORTS
            // -------------------------------------------------

            model.ApprovedReports =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Reports r

                    INNER JOIN ReportApprovalStatuses s
                        ON s.Id = r.ApprovalStatusId

                    WHERE LOWER(s.StatusName)
                        LIKE '%approved%'
                    """
                )
                .SingleAsync();


            // -------------------------------------------------
            // PENDING REPORTS
            // -------------------------------------------------

            model.PendingReports =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Reports r

                    INNER JOIN ReportApprovalStatuses s
                        ON s.Id = r.ApprovalStatusId

                    WHERE LOWER(s.StatusName)
                        NOT LIKE '%approved%'

                      AND LOWER(s.StatusName)
                        NOT LIKE '%rejected%'
                    """
                )
                .SingleAsync();


            // -------------------------------------------------
            // REJECTED REPORTS
            // -------------------------------------------------

            model.RejectedReports =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM Reports r

                    INNER JOIN ReportApprovalStatuses s
                        ON s.Id = r.ApprovalStatusId

                    WHERE LOWER(s.StatusName)
                        LIKE '%rejected%'
                    """
                )
                .SingleAsync();


            // -------------------------------------------------
            // COMPLETED TESTS
            // -------------------------------------------------

            model.CompletedTests =
                await _context.Database
                .SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value]
                    FROM TestResults tr

                    INNER JOIN ResultStatuses rs
                        ON rs.Id = tr.ResultStatusId

                    WHERE LOWER(rs.StatusName)
                        LIKE '%approved%'

                       OR LOWER(rs.StatusName)
                        LIKE '%completed%'
                    """
                )
                .SingleAsync();


            return View(model);
        }


        // =====================================================
        // AUDIT LOGS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> AuditLogs(
            DateTime? selectedDate)
        {
            ViewData["ActiveMenu"] = "AuditLogs";


            // =================================================
            // SELECTED DATE
            // =================================================

            DateTime auditDate =
                selectedDate?.Date ?? DateTime.Today;


            // =================================================
            // NEXT DATE
            //
            // Used to include the complete selected day.
            // =================================================

            DateTime nextDate =
                auditDate.AddDays(1);


            // =================================================
            // GET AUDIT LOGS
            //
            // Include User so Email Address is available.
            // =================================================

            var auditLogs =
                await _context.AuditLogs
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x =>
                    x.Timestamp.HasValue &&
                    x.Timestamp.Value >= auditDate &&
                    x.Timestamp.Value < nextDate
                )
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();


            // =================================================
            // VIEWBAG DATE
            // =================================================

            ViewBag.SelectedDate =
                auditDate.ToString("yyyy-MM-dd");


            ViewBag.SelectedDateDisplay =
                auditDate.ToString("dd MMM yyyy");


            // =================================================
            // RETURN VIEW
            // =================================================

            return View(auditLogs);
        }


        // =====================================================
        // GLOBAL SEARCH
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> GlobalSearch(
            string query)
        {
            // -------------------------------------------------
            // EMPTY SEARCH
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new
                {
                    success = false,

                    message =
                        "Please enter something to search."
                });
            }


            query = query.Trim();


            // =================================================
            // 1. PATIENT SEARCH
            // =================================================

            var patient =
                await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>

                    (
                        p.PatientCode != null &&
                        p.PatientCode.Contains(query)
                    )

                    ||

                    (
                        p.FirstName != null &&
                        p.FirstName.Contains(query)
                    )

                    ||

                    (
                        p.LastName != null &&
                        p.LastName.Contains(query)
                    )

                    ||

                    (
                        p.Phone != null &&
                        p.Phone.Contains(query)
                    )
                );


            if (patient != null)
            {
                return Json(new
                {
                    success = true,

                    type = "patient",

                    url = Url.Action(
                        "Details",
                        "Patients",
                        new
                        {
                            id = patient.Id
                        }
                    ),

                    message =
                        $"Patient found: " +
                        $"{patient.FirstName} " +
                        $"{patient.LastName}"
                });
            }


            // =================================================
            // 2. INVOICE SEARCH
            // =================================================

            var invoice =
                await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i =>

                    i.InvoiceNumber != null &&

                    i.InvoiceNumber.Contains(query)
                );


            if (invoice != null)
            {
                return Json(new
                {
                    success = true,

                    type = "invoice",

                    url = Url.Action(
                        "Details",
                        "Invoices",
                        new
                        {
                            id = invoice.Id
                        }
                    ),

                    message =
                        $"Invoice found: " +
                        $"{invoice.InvoiceNumber}"
                });
            }


            // =================================================
            // 3. APPOINTMENT SEARCH
            // =================================================

            var appointment =
                await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>

                    a.AppointmentCode != null &&

                    a.AppointmentCode.Contains(query)
                );


            if (appointment != null)
            {
                return Json(new
                {
                    success = true,

                    type = "appointment",

                    url = Url.Action(
                        "Details",
                        "Appointments",
                        new
                        {
                            id = appointment.Id
                        }
                    ),

                    message =
                        $"Appointment found: " +
                        $"{appointment.AppointmentCode}"
                });
            }


            // =================================================
            // 4. TEST SEARCH
            // =================================================

            var test =
                await _context.Tests
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>

                    t.TestName != null &&

                    t.TestName.Contains(query)
                );


            if (test != null)
            {
                return Json(new
                {
                    success = true,

                    type = "test",

                    url = Url.Action(
                        "Details",
                        "Tests",
                        new
                        {
                            id = test.Id
                        }
                    ),

                    message =
                        $"Test found: " +
                        $"{test.TestName}"
                });
            }


            // =================================================
            // 5. REFERRAL DOCTOR SEARCH
            // =================================================

            var doctor =
                await _context.ReferralDoctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>

                    (
                        d.DoctorName != null &&
                        d.DoctorName.Contains(query)
                    )

                    ||

                    (
                        d.Phone != null &&
                        d.Phone.Contains(query)
                    )
                );


            if (doctor != null)
            {
                return Json(new
                {
                    success = true,

                    type = "doctor",

                    url = Url.Action(
                        "Details",
                        "ReferralDoctors",
                        new
                        {
                            id = doctor.Id
                        }
                    ),

                    message =
                        $"Doctor found: " +
                        $"{doctor.DoctorName}"
                });
            }


            // =================================================
            // NOTHING FOUND
            // =================================================

            return Json(new
            {
                success = false,

                message =
                    $"No result found for \"{query}\"."
            });
        }


        // =====================================================
        // TIME AGO
        // =====================================================

        private string GetTimeAgo(DateTime? date)
        {
            if (!date.HasValue)
                return "-";


            var difference =
                DateTime.Now - date.Value;


            if (difference.TotalSeconds < 60)
            {
                return "Just now";
            }


            if (difference.TotalMinutes < 60)
            {
                int minutes =
                    (int)difference.TotalMinutes;

                return minutes == 1
                    ? "1 min ago"
                    : $"{minutes} min ago";
            }


            if (difference.TotalHours < 24)
            {
                int hours =
                    (int)difference.TotalHours;

                return hours == 1
                    ? "1 hour ago"
                    : $"{hours} hours ago";
            }


            if (difference.TotalDays < 7)
            {
                int days =
                    (int)difference.TotalDays;

                return days == 1
                    ? "1 day ago"
                    : $"{days} days ago";
            }


            return date.Value.ToString(
                "dd MMM yyyy"
            );
        }


        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );


            return RedirectToAction(
                "Login",
                "Account"
            );
        }
    }


    // =========================================================
    // RECENT ACTIVITY QUERY RESULT
    // =========================================================

    public class RecentActivityQueryResult
    {
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime? ActivityDate { get; set; }

        public string Icon { get; set; } = "";

        public string IconClass { get; set; } = "";
    }
}