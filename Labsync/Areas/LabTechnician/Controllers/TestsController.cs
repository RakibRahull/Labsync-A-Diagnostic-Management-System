using Labsync.Models.Data;
using LabSync.Areas.LabTechnician.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.LabTechnician.Controllers
{
    [Area("LabTechnician")]
    [Authorize]
    public class TestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // INDEX

        [HttpGet]
        public async Task<IActionResult> Index(string? appointmentId)
        {
            // =================================================
            // CREATE MODEL
            // =================================================

            var model = new LabTechnicianTestsViewModel
            {
                SearchAppointmentId =
                    appointmentId?.Trim(),

                HasSearched =
                    !string.IsNullOrWhiteSpace(appointmentId)
            };


            // =================================================
            // NO SEARCH
            // =================================================

            if (string.IsNullOrWhiteSpace(appointmentId))
            {
                ViewData["ActiveMenu"] = "Tests";

                return View(model);
            }


            // =================================================
            // CLEAN SEARCH VALUE
            // =================================================

            var searchValue =
                appointmentId.Trim();


            // =================================================
            // SEARCH BY APPOINTMENT CODE
            var appointmentInfo =
                await _context.Database
                    .SqlQueryRaw<LabTechnicianTestsAppointmentInfo>(
                        """
                        SELECT TOP 1

                            a.Id AS AppointmentId,

                            a.AppointmentCode AS AppointmentCode,

                            p.Id AS PatientId,

                            p.PatientCode AS PatientCode,

                            CONCAT(
                                p.FirstName,
                                ' ',
                                p.LastName
                            ) AS PatientName,

                            p.Phone AS Phone,

                            p.Email AS Email,

                            a.AppointmentDate AS AppointmentDate,

                            a.AppointmentTime AS AppointmentTime

                        FROM Appointments a

                        INNER JOIN Patients p
                            ON p.Id = a.PatientId

                        WHERE
                            UPPER(
                                LTRIM(
                                    RTRIM(
                                        ISNULL(a.AppointmentCode, '')
                                    )
                                )
                            )
                            =
                            UPPER(
                                LTRIM(
                                    RTRIM({0})
                                )
                            )

                        ORDER BY
                            a.Id DESC
                        """,
                        searchValue
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // FALLBACK:

            if (appointmentInfo == null &&
                int.TryParse(
                    searchValue,
                    out int numericAppointmentId))
            {
                appointmentInfo =
                    await _context.Database
                        .SqlQueryRaw<LabTechnicianTestsAppointmentInfo>(
                            """
                            SELECT TOP 1

                                a.Id AS AppointmentId,

                                a.AppointmentCode AS AppointmentCode,

                                p.Id AS PatientId,

                                p.PatientCode AS PatientCode,

                                CONCAT(
                                    p.FirstName,
                                    ' ',
                                    p.LastName
                                ) AS PatientName,

                                p.Phone AS Phone,

                                p.Email AS Email,

                                a.AppointmentDate AS AppointmentDate,

                                a.AppointmentTime AS AppointmentTime

                            FROM Appointments a

                            INNER JOIN Patients p
                                ON p.Id = a.PatientId

                            WHERE
                                a.Id = {0}

                            ORDER BY
                                a.Id DESC
                            """,
                            numericAppointmentId
                        )
                        .FirstOrDefaultAsync();
            }


            // =================================================
            // APPOINTMENT NOT FOUND
            // =================================================

            if (appointmentInfo == null)
            {
                ModelState.AddModelError(
                    "",
                    $"No appointment was found for \"{searchValue}\"."
                );

                ViewData["ActiveMenu"] = "Tests";

                return View(model);
            }


            // =================================================
            // GET ALL TESTS ASSIGNED TO APPOINTMENT
            // =================================================

            var assignedTests =
                await _context.Database
                    .SqlQueryRaw<LabTechnicianTestRowViewModel>(
                        """
                        SELECT

                            at.Id AS AppointmentTestId,

                            at.AppointmentId AS AppointmentId,

                            at.TestId AS TestId,

                            t.TestCode AS TestCode,

                            t.TestName AS TestName,

                            t.Price AS Price,

                            t.Unit AS Unit,

                            CAST(
                                t.TurnAroundTime AS VARCHAR(100)
                            ) AS TurnAroundTime,

                            t.IsActive AS IsActive,

                            at.SampleStatusId AS SampleStatusId,

                            CASE
                                WHEN at.SampleStatusId = 1
                                    THEN 'Pending'

                                WHEN at.SampleStatusId = 2
                                    THEN 'Collected'

                                WHEN at.SampleStatusId = 3
                                    THEN 'Processing'

                                WHEN at.SampleStatusId = 4
                                    THEN 'Completed'

                                ELSE 'Unknown'
                            END AS SampleStatusName,

                            at.TechnicianId AS TechnicianId,

                            at.CreatedAt AS CreatedAt

                        FROM AppointmentTests at

                        INNER JOIN Tests t
                            ON t.Id = at.TestId

                        WHERE
                            at.AppointmentId = {0}

                        ORDER BY
                            at.Id ASC
                        """,
                        appointmentInfo.AppointmentId
                    )
                    .ToListAsync();


            // =================================================
            // FILL MODEL
            // =================================================

            model.AppointmentId =
                appointmentInfo.AppointmentId;

            model.AppointmentCode =
                appointmentInfo.AppointmentCode;

            model.PatientId =
                appointmentInfo.PatientId;

            model.PatientCode =
                appointmentInfo.PatientCode;

            model.PatientName =
                appointmentInfo.PatientName;

            model.Phone =
                appointmentInfo.Phone;

            model.Email =
                appointmentInfo.Email;

            model.AppointmentDate =
                appointmentInfo.AppointmentDate;

            model.AppointmentTime =
                appointmentInfo.AppointmentTime;

            model.Tests =
                assignedTests;


            // =================================================
            // NO TESTS
            // =================================================

            if (assignedTests == null ||
                assignedTests.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    $"No tests are assigned to appointment \"{appointmentInfo.AppointmentCode}\"."
                );
            }


            // =================================================
            // ACTIVE MENU
            // =================================================

            ViewData["ActiveMenu"] = "Tests";


            return View(model);
        }
    }


    // =========================================================
    // APPOINTMENT INFORMATION
    // =========================================================

    public class LabTechnicianTestsAppointmentInfo
    {
        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public int PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = "";

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }
    }
}