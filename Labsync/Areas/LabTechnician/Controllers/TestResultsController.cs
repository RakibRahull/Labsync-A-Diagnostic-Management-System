using System.Security.Claims;
using Labsync.Models.Data;
using LabSync.Areas.LabTechnician.Models;
using LabSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabSync.Areas.LabTechnician.Controllers
{
    [Area("LabTechnician")]
    [Authorize]
    public class TestResultsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;


        public TestResultsController(
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
            string? patientId,
            string? resultStatus)
        {
            appointmentId =
                string.IsNullOrWhiteSpace(appointmentId)
                    ? null
                    : appointmentId.Trim();

            patientId =
                string.IsNullOrWhiteSpace(patientId)
                    ? null
                    : patientId.Trim();

            resultStatus =
                string.IsNullOrWhiteSpace(resultStatus)
                    ? null
                    : resultStatus.Trim();


            var results =
                await _context.Database
                    .SqlQueryRaw<TestResultListRowViewModel>(
                        """
                        SELECT

                            ISNULL(
                                tr.Id,
                                0
                            ) AS TestResultId,

                            at.Id
                                AS AppointmentTestId,

                            a.Id
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


                            t.TestCode
                                AS TestCode,

                            t.TestName
                                AS TestName,

                            t.Unit
                                AS Unit,


                            rr.ReferenceRange
                                AS ReferenceRange,

                            rr.Description
                                AS ReferenceRangeDescription,


                            tr.ResultValue
                                AS ResultValue,

                            tr.ResultInterpretation
                                AS ResultInterpretation,

                            tr.Remarks
                                AS Remarks,

                            ISNULL(
                                tr.ResultStatusId,
                                0
                            ) AS ResultStatusId,

                            ISNULL(
                                rs.StatusName,
                                'Not Entered'
                            ) AS ResultStatusName,

                            tr.ResultDate
                                AS ResultDate,


                            at.SampleStatusId
                                AS SampleStatusId,

                            ISNULL(
                                ss.StatusName,
                                'Pending'
                            ) AS SampleStatusName

                        FROM AppointmentTests at

                        INNER JOIN Appointments a
                            ON a.Id = at.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id = a.PatientId

                        INNER JOIN Tests t
                            ON t.Id = at.TestId

                        LEFT JOIN ReferenceRanges rr
                            ON rr.Id = t.ReferenceRangeId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id = at.SampleStatusId

                        LEFT JOIN TestResults tr
                            ON tr.AppointmentTestId = at.Id

                        LEFT JOIN ResultStatuses rs
                            ON rs.Id = tr.ResultStatusId

                        WHERE
                            LOWER(
                                LTRIM(
                                    RTRIM(
                                        ISNULL(
                                            ss.StatusName,
                                            ''
                                        )
                                    )
                                )
                            ) = 'collected'

                            AND
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

                                OR CAST(
                                    a.Id AS VARCHAR(20)
                                ) = {0}
                            )

                            AND
                            (
                                NULLIF(
                                    {1},
                                    ''
                                ) IS NULL

                                OR UPPER(
                                    ISNULL(
                                        p.PatientCode,
                                        ''
                                    )
                                )
                                LIKE
                                '%' +
                                UPPER({1}) +
                                '%'
                            )

                            AND
                            (
                                NULLIF(
                                    {2},
                                    ''
                                ) IS NULL

                                OR LOWER(
                                    ISNULL(
                                        rs.StatusName,
                                        ''
                                    )
                                )
                                =
                                LOWER({2})
                            )

                        ORDER BY
                            a.AppointmentDate DESC,
                            a.AppointmentTime DESC,
                            at.Id ASC
                        """,

                        appointmentId ?? "",

                        patientId ?? "",

                        resultStatus ?? ""
                    )
                    .ToListAsync();


            // =====================================================
            // STATUS LIST
            // =====================================================

            var statuses =
                await _context.Database
                    .SqlQueryRaw<
                        ResultStatusOptionViewModel>(
                        """
                        SELECT
                            Id,
                            StatusName
                        FROM ResultStatuses
                        ORDER BY Id
                        """
                    )
                    .ToListAsync();


            ViewBag.AppointmentId =
                appointmentId ?? "";

            ViewBag.PatientId =
                patientId ?? "";

            ViewBag.ResultStatus =
                resultStatus ?? "";

            ViewBag.ResultStatuses =
                statuses;


            // =====================================================
            // RESULT SUMMARY
            // =====================================================

            ViewBag.TotalTests =
                results.Count;


            ViewBag.CompletedTests =
                results.Count(
                    x => x.TestResultId > 0
                );


            ViewBag.RemainingTests =
                results.Count(
                    x => x.TestResultId <= 0
                );


            ViewBag.AllResultsCompleted =
                !string.IsNullOrWhiteSpace(
                    appointmentId
                )
                &&
                results.Count > 0
                &&
                results.All(
                    x =>
                        x.TestResultId > 0
                        &&
                        !string.IsNullOrWhiteSpace(
                            x.ResultValue
                        )
                        &&
                        !string.IsNullOrWhiteSpace(
                            x.ResultInterpretation
                        )
                );


            ViewData["ActiveMenu"] =
                "TestResults";


            return View(results);
        }


        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(
            int appointmentTestId)
        {
            if (appointmentTestId <= 0)
            {
                TempData["Error"] =
                    "Invalid appointment test.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            var model =
                await _context.Database
                    .SqlQueryRaw<TestResultEntryViewModel>(
                        """
                        SELECT TOP 1

                            ISNULL(
                                tr.Id,
                                0
                            ) AS TestResultId,


                            at.Id
                                AS AppointmentTestId,

                            a.Id
                                AS AppointmentId,

                            a.AppointmentCode
                                AS AppointmentCode,

                            a.AppointmentDate
                                AS AppointmentDate,

                            a.AppointmentTime
                                AS AppointmentTime,


                            p.Id
                                AS PatientId,

                            p.PatientCode
                                AS PatientCode,

                            CONCAT(
                                p.FirstName,
                                ' ',
                                p.LastName
                            ) AS PatientName,

                            p.Phone
                                AS Phone,

                            p.Email
                                AS Email,


                            t.Id
                                AS TestId,

                            t.TestCode
                                AS TestCode,

                            t.TestName
                                AS TestName,

                            t.Price
                                AS Price,

                            t.Unit
                                AS Unit,

                            CAST(
                                t.TurnAroundTime
                                AS VARCHAR(100)
                            ) AS TurnAroundTime,


                            ISNULL(
                                t.ReferenceRangeId,
                                0
                            ) AS ReferenceRangeId,

                            ISNULL(
                                rr.ReferenceRange,
                                ''
                            ) AS ReferenceRange,

                            ISNULL(
                                rr.Description,
                                ''
                            ) AS ReferenceRangeDescription,


                            at.SampleStatusId
                                AS SampleStatusId,

                            ISNULL(
                                ss.StatusName,
                                'Pending'
                            ) AS SampleStatusName,

                            t.SampleTypeId
                                AS SampleTypeId,

                            st.SampleTypeName
                                AS SampleTypeName,


                            ISNULL(
                                tr.ResultStatusId,
                                0
                            ) AS ResultStatusId,

                            rs.StatusName
                                AS ResultStatusName,

                            tr.ResultValue
                                AS ResultValue,

                            tr.ResultInterpretation
                                AS ResultInterpretation,

                            tr.Remarks
                                AS Remarks,

                            tr.ResultDate
                                AS ResultDate

                        FROM AppointmentTests at

                        INNER JOIN Appointments a
                            ON a.Id =
                               at.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id =
                               a.PatientId

                        INNER JOIN Tests t
                            ON t.Id =
                               at.TestId

                        LEFT JOIN ReferenceRanges rr
                            ON rr.Id =
                               t.ReferenceRangeId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id =
                               at.SampleStatusId

                        LEFT JOIN SampleTypes st
                            ON st.Id =
                               t.SampleTypeId

                        LEFT JOIN TestResults tr
                            ON tr.AppointmentTestId =
                               at.Id

                        LEFT JOIN ResultStatuses rs
                            ON rs.Id =
                               tr.ResultStatusId

                        WHERE
                            at.Id = {0}
                        """,

                        appointmentTestId
                    )
                    .FirstOrDefaultAsync();


            if (model == null)
            {
                TempData["Error"] =
                    "The selected test was not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // SAMPLE STATUS
            // =====================================================

            var sampleStatus =
                (
                    model.SampleStatusName
                    ?? ""
                ).Trim();


            var canEnterResult =
                sampleStatus.Equals(
                    "Collected",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                sampleStatus.Equals(
                    "Processing",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                sampleStatus.Equals(
                    "Completed",
                    StringComparison.OrdinalIgnoreCase
                );


            if (!canEnterResult)
            {
                TempData["Error"] =
                    "Sample must be collected before entering a result.";

                return RedirectToAction(
                    "Index",
                    "Tests",
                    new
                    {
                        area = "LabTechnician",
                        appointmentId =
                            model.AppointmentCode
                    }
                );
            }


            // =====================================================
            // RESULT STATUS
            // =====================================================

            model.ResultStatuses =
                await _context.Database
                    .SqlQueryRaw<
                        ResultStatusOptionViewModel>(
                        """
                        SELECT
                            Id,
                            StatusName
                        FROM ResultStatuses
                        ORDER BY Id
                        """
                    )
                    .ToListAsync();


            if (model.ResultStatusId <= 0)
            {
                var pending =
                    model.ResultStatuses
                        .FirstOrDefault(
                            x =>
                                x.StatusName
                                    .Trim()
                                    .Equals(
                                        "Pending",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                        );


                if (pending != null)
                {
                    model.ResultStatusId =
                        pending.Id;

                    model.ResultStatusName =
                        pending.StatusName;
                }
            }


            model.HasExistingResult =
                model.TestResultId > 0;


            ViewData["ActiveMenu"] =
                "TestResults";


            return View(
                "Create",
                model
            );
        }


        // =========================================================
        // SAVE RESULT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(
            TestResultEntryViewModel model,
            int appointmentTestId)
        {
            // =====================================================
            // ENSURE ID
            // =====================================================

            if (appointmentTestId > 0)
            {
                model.AppointmentTestId =
                    appointmentTestId;
            }


            if (model.AppointmentTestId <= 0)
            {
                TempData["Error"] =
                    "Invalid appointment test.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // RESULT VALIDATION
            // =====================================================

            if (
                string.IsNullOrWhiteSpace(
                    model.ResultValue
                )
            )
            {
                ModelState.AddModelError(
                    "ResultValue",
                    "Result value is required."
                );
            }


            var interpretation =
                model.ResultInterpretation?
                    .Trim();


            if (string.IsNullOrWhiteSpace(
                interpretation))
            {
                ModelState.AddModelError(
                    "ResultInterpretation",
                    "Please select Normal or Abnormal."
                );
            }
            else if (
                !interpretation.Equals(
                    "Normal",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                !interpretation.Equals(
                    "Abnormal",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                ModelState.AddModelError(
                    "ResultInterpretation",
                    "Invalid result interpretation."
                );
            }


            // =====================================================
            // LOAD TEST
            // =====================================================

            var testInfo =
                await _context.Database
                    .SqlQueryRaw<TestResultLookupViewModel>(
                        """
                        SELECT TOP 1

                            ISNULL(
                                tr.Id,
                                0
                            ) AS TestResultId,

                            at.Id
                                AS AppointmentTestId,

                            a.Id
                                AS AppointmentId,

                            a.AppointmentCode
                                AS AppointmentCode,

                            a.AppointmentDate
                                AS AppointmentDate,

                            a.AppointmentTime
                                AS AppointmentTime,


                            p.Id
                                AS PatientId,

                            p.PatientCode
                                AS PatientCode,

                            CONCAT(
                                p.FirstName,
                                ' ',
                                p.LastName
                            ) AS PatientName,

                            p.Phone
                                AS Phone,

                            p.Email
                                AS Email,


                            t.Id
                                AS TestId,

                            t.TestCode
                                AS TestCode,

                            t.TestName
                                AS TestName,

                            t.Price
                                AS Price,

                            t.Unit
                                AS Unit,

                            CAST(
                                t.TurnAroundTime
                                AS VARCHAR(100)
                            ) AS TurnAroundTime,


                            ISNULL(
                                t.ReferenceRangeId,
                                0
                            ) AS ReferenceRangeId,

                            ISNULL(
                                rr.ReferenceRange,
                                ''
                            ) AS ReferenceRange,

                            ISNULL(
                                rr.Description,
                                ''
                            ) AS ReferenceRangeDescription,


                            at.SampleStatusId
                                AS SampleStatusId,

                            ISNULL(
                                ss.StatusName,
                                'Pending'
                            ) AS SampleStatusName,

                            t.SampleTypeId
                                AS SampleTypeId,

                            st.SampleTypeName
                                AS SampleTypeName,


                            tr.ResultValue
                                AS ResultValue,

                            tr.ResultInterpretation
                                AS ResultInterpretation,

                            tr.Remarks
                                AS Remarks,

                            tr.ResultStatusId
                                AS ResultStatusId,

                            rs.StatusName
                                AS ResultStatusName,

                            tr.ResultDate
                                AS ResultDate

                        FROM AppointmentTests at

                        INNER JOIN Appointments a
                            ON a.Id =
                               at.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id =
                               a.PatientId

                        INNER JOIN Tests t
                            ON t.Id =
                               at.TestId

                        LEFT JOIN ReferenceRanges rr
                            ON rr.Id =
                               t.ReferenceRangeId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id =
                               at.SampleStatusId

                        LEFT JOIN SampleTypes st
                            ON st.Id =
                               t.SampleTypeId

                        LEFT JOIN TestResults tr
                            ON tr.AppointmentTestId =
                               at.Id

                        LEFT JOIN ResultStatuses rs
                            ON rs.Id =
                               tr.ResultStatusId

                        WHERE
                            at.Id = {0}
                        """,

                        model.AppointmentTestId
                    )
                    .FirstOrDefaultAsync();


            if (testInfo == null)
            {
                TempData["Error"] =
                    "The selected test was not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // SAMPLE VALIDATION
            // =====================================================

            var currentSampleStatus =
                (
                    testInfo.SampleStatusName
                    ?? ""
                ).Trim();


            var sampleIsValid =
                currentSampleStatus.Equals(
                    "Collected",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                currentSampleStatus.Equals(
                    "Processing",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                currentSampleStatus.Equals(
                    "Completed",
                    StringComparison.OrdinalIgnoreCase
                );


            if (!sampleIsValid)
            {
                TempData["Error"] =
                    "The sample must be collected before entering a result.";

                return RedirectToAction(
                    "Create",
                    "TestResults",
                    new
                    {
                        area = "LabTechnician",
                        appointmentTestId =
                            model.AppointmentTestId
                    }
                );
            }


            // =====================================================
            // RESULT STATUS
            // =====================================================

            var resultStatusId =
                model.ResultStatusId;


            if (resultStatusId <= 0)
            {
                var pendingStatus =
                    await _context.Database
                        .SqlQueryRaw<
                            ResultStatusOptionViewModel>(
                            """
                            SELECT TOP 1
                                Id,
                                StatusName
                            FROM ResultStatuses
                            WHERE LOWER(
                                LTRIM(
                                    RTRIM(
                                        StatusName
                                    )
                                )
                            ) = 'pending'
                            ORDER BY Id
                            """
                        )
                        .FirstOrDefaultAsync();


                if (pendingStatus != null)
                {
                    resultStatusId =
                        pendingStatus.Id;
                }
            }


            if (resultStatusId <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "No result status is available."
                );
            }


            // =====================================================
            // RELOAD IF INVALID
            // =====================================================

            if (!ModelState.IsValid)
            {
                await RebuildEntryModelAsync(
                    model,
                    testInfo
                );

                return View(
                    "Create",
                    model
                );
            }


            // =====================================================
            // FIND CURRENT USER
            // =====================================================

            var enteredByUser =
                await FindLoggedInUserAsync();


            if (enteredByUser == null)
            {
                TempData["Error"] =
                    "Your Lab Technician account is not linked with the Users table.";

                return RedirectToAction(
                    "Create",
                    "TestResults",
                    new
                    {
                        area = "LabTechnician",
                        appointmentTestId =
                            model.AppointmentTestId
                    }
                );
            }


            // =====================================================
            // CHECK EXISTING RESULT
            // =====================================================

            var existingResult =
                await _context.Database
                    .SqlQueryRaw<TestResultExistingViewModel>(
                        """
                        SELECT TOP 1

                            Id AS TestResultId,

                            AppointmentTestId
                                AS AppointmentTestId

                        FROM TestResults

                        WHERE
                            AppointmentTestId = {0}

                        ORDER BY
                            Id DESC
                        """,

                        model.AppointmentTestId
                    )
                    .FirstOrDefaultAsync();


            // =====================================================
            // UPDATE
            // =====================================================

            if (existingResult != null)
            {
                await _context.Database
                    .ExecuteSqlRawAsync(
                        """
                        UPDATE TestResults

                        SET
                            ResultStatusId = {0},

                            ResultValue = {1},

                            ResultInterpretation = {2},

                            Remarks = {3},

                            ResultDate = {4}

                        WHERE
                            Id = {5}
                        """,

                        resultStatusId,

                        model.ResultValue!
                            .Trim(),

                        interpretation!,

                        string.IsNullOrWhiteSpace(
                            model.Remarks
                        )
                            ? null
                            : model.Remarks.Trim(),

                        DateTime.Now,

                        existingResult.TestResultId
                    );


                TempData["Success"] =
                    "Test result updated successfully.";
            }


            // =====================================================
            // INSERT
            // =====================================================

            else
            {
                await _context.Database
                    .ExecuteSqlRawAsync(
                        """
                        INSERT INTO TestResults
                        (
                            AppointmentTestId,
                            EnteredBy,
                            ApprovedBy,
                            ResultStatusId,
                            ResultValue,
                            ResultInterpretation,
                            Remarks,
                            ResultDate
                        )
                        VALUES
                        (
                            {0},
                            {1},
                            NULL,
                            {2},
                            {3},
                            {4},
                            {5},
                            {6}
                        )
                        """,

                        model.AppointmentTestId,

                        enteredByUser.Id,

                        resultStatusId,

                        model.ResultValue!
                            .Trim(),

                        interpretation!,

                        string.IsNullOrWhiteSpace(
                            model.Remarks
                        )
                            ? null
                            : model.Remarks.Trim(),

                        DateTime.Now
                    );


                TempData["Success"] =
                    "Test result saved successfully.";
            }


            // =====================================================
            // RETURN TO SAME APPOINTMENT
            // =====================================================

            return RedirectToAction(
                nameof(Index),
                new
                {
                    appointmentId =
                        testInfo.AppointmentCode
                }
            );
        }


        // =========================================================
        // GENERATE REPORT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GenerateReport(
            int appointmentId)
        {
            if (appointmentId <= 0)
            {
                TempData["Error"] =
                    "Invalid appointment.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =====================================================
            // CURRENT USER
            // =====================================================

            var generatedByUser =
                await FindLoggedInUserAsync();


            if (generatedByUser == null)
            {
                TempData["Error"] =
                    "Your account is not linked with the Users table.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        appointmentId
                    }
                );
            }


            // =====================================================
            // LOAD ALL TESTS
            // =====================================================

            var reportResults =
                await _context.Database
                    .SqlQueryRaw<TestResultListRowViewModel>(
                        """
                        SELECT

                            ISNULL(
                                tr.Id,
                                0
                            ) AS TestResultId,

                            at.Id
                                AS AppointmentTestId,

                            a.Id
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

                            t.TestCode
                                AS TestCode,

                            t.TestName
                                AS TestName,

                            t.Unit
                                AS Unit,

                            rr.ReferenceRange
                                AS ReferenceRange,

                            rr.Description
                                AS ReferenceRangeDescription,

                            tr.ResultValue
                                AS ResultValue,

                            tr.ResultInterpretation
                                AS ResultInterpretation,

                            tr.Remarks
                                AS Remarks,

                            ISNULL(
                                tr.ResultStatusId,
                                0
                            ) AS ResultStatusId,

                            ISNULL(
                                rs.StatusName,
                                'Not Entered'
                            ) AS ResultStatusName,

                            tr.ResultDate
                                AS ResultDate,

                            at.SampleStatusId
                                AS SampleStatusId,

                            ISNULL(
                                ss.StatusName,
                                'Pending'
                            ) AS SampleStatusName

                        FROM AppointmentTests at

                        INNER JOIN Appointments a
                            ON a.Id =
                               at.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id =
                               a.PatientId

                        INNER JOIN Tests t
                            ON t.Id =
                               at.TestId

                        LEFT JOIN ReferenceRanges rr
                            ON rr.Id =
                               t.ReferenceRangeId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id =
                               at.SampleStatusId

                        LEFT JOIN TestResults tr
                            ON tr.AppointmentTestId =
                               at.Id

                        LEFT JOIN ResultStatuses rs
                            ON rs.Id =
                               tr.ResultStatusId

                        WHERE
                            a.Id = {0}

                        ORDER BY
                            at.Id ASC
                        """,

                        appointmentId
                    )
                    .ToListAsync();


            // =====================================================
            // NO TEST
            // =====================================================

            if (
                reportResults == null
                ||
                reportResults.Count == 0
            )
            {
                TempData["Error"] =
                    "No tests were found for this appointment.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        appointmentId
                    }
                );
            }


            // CHECK ALL RESULTS

            var incomplete =
                reportResults
                    .Where(
                        x =>
                            x.TestResultId <= 0
                            ||
                            string.IsNullOrWhiteSpace(
                                x.ResultValue
                            )
                            ||
                            string.IsNullOrWhiteSpace(
                                x.ResultInterpretation
                            )
                    )
                    .ToList();


            if (incomplete.Count > 0)
            {
                TempData["Error"] =
                    "All test results must be entered before generating the report.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        appointmentId =
                            reportResults
                                .First()
                                .AppointmentCode
                    }
                );
            }


            // CHECK APPROVED EXISTING REPORT

            var existingReport =
                await _context.Database
                    .SqlQueryRaw<ExistingReportViewModel>(
                        """
                        SELECT TOP 1

                            Id,

                            ApprovalStatusId,

                            PdfPath

                        FROM Reports

                        WHERE
                            AppointmentId = {0}

                        ORDER BY
                            Id DESC
                        """,
                        appointmentId
                    )
                    .FirstOrDefaultAsync();


            if (
                existingReport != null
                &&
                existingReport.ApprovalStatusId == 2
            )
            {
                TempData["Error"] =
                    "This appointment already has an approved report.";

                return RedirectToAction(
                    "Index",
                    "Reports",
                    new
                    {
                        area = "LabTechnician"
                    }
                );
            }


            // =====================================================
            // CREATE REPORT DIRECTORY
            // =====================================================

            var reportsDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "reports"
                );


            if (!Directory.Exists(
                reportsDirectory))
            {
                Directory.CreateDirectory(
                    reportsDirectory
                );
            }


            // =====================================================
            // FILE NAME
            // =====================================================

            var appointmentCode =
                reportResults
                    .First()
                    .AppointmentCode
                    ?? $"APT-{appointmentId}";


            var safeCode =
                string.Join(
                    "_",
                    appointmentCode.Split(
                        Path.GetInvalidFileNameChars(),
                        StringSplitOptions.RemoveEmptyEntries
                    )
                );


            var fileName =
                $"{safeCode}_Laboratory_Report.pdf";


            var filePath =
                Path.Combine(
                    reportsDirectory,
                    fileName
                );


            var pdfPath =
                "/reports/" +
                fileName;


          
            // CREATE PDF

            var document =
                Document.Create(
                    container =>
                    {
                        foreach (
                            var result
                            in reportResults
                        )
                        {
                            container.Page(
                                page =>
                                {
                                    page.Size(
                                        PageSizes.A4
                                    );

                                    page.Margin(
                                        40
                                    );


                                    // =========================
                                    // HEADER
                                    // =========================

                                    page.Header()
                                        .Column(
                                            column =>
                                            {
                                                column.Item()
                                                    .AlignCenter()
                                                    .Text(
                                                        "LabSync Diagnostic Laboratory"
                                                    )
                                                    .FontSize(21)
                                                    .Bold();

                                                column.Item()
                                                    .AlignCenter()
                                                    .Text(
                                                        "Laboratory Test Report"
                                                    )
                                                    .FontSize(13);

                                                column.Item()
                                                    .PaddingTop(8)
                                                    .LineHorizontal(
                                                        1
                                                    );
                                            }
                                        );


                                    // =========================
                                    // CONTENT
                                    // =========================

                                    page.Content()
                                        .PaddingVertical(20)
                                        .Column(
                                            column =>
                                            {
                                                column.Spacing(
                                                    12
                                                );


                                                // PATIENT

                                                column.Item()
                                                    .Text(
                                                        "Patient Information"
                                                    )
                                                    .FontSize(14)
                                                    .Bold();


                                                column.Item()
                                                    .Border(1)
                                                    .Padding(12)
                                                    .Column(
                                                        info =>
                                                        {
                                                            info.Spacing(6);

                                                            info.Item()
                                                                .Text(
                                                                    $"Patient Name: {result.PatientName}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Patient Code: {result.PatientCode ?? "N/A"}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Phone: {result.Phone ?? "N/A"}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Appointment ID: {result.AppointmentCode ?? "N/A"}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Appointment Date: {result.AppointmentDate:dd/MM/yyyy}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Appointment Time: {DateTime.Today.Add(result.AppointmentTime):hh:mm tt}"
                                                                );
                                                        }
                                                    );


                                                // TEST

                                                column.Item()
                                                    .PaddingTop(8)
                                                    .Text(
                                                        "Test Information"
                                                    )
                                                    .FontSize(14)
                                                    .Bold();


                                                column.Item()
                                                    .Border(1)
                                                    .Padding(12)
                                                    .Column(
                                                        info =>
                                                        {
                                                            info.Spacing(6);

                                                            info.Item()
                                                                .Text(
                                                                    $"Test Code: {result.TestCode ?? "N/A"}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Test Name: {result.TestName}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Unit: {result.Unit ?? "N/A"}"
                                                                );
                                                        }
                                                    );


                                                // RESULT

                                                column.Item()
                                                    .PaddingTop(8)
                                                    .Text(
                                                        "Test Result"
                                                    )
                                                    .FontSize(14)
                                                    .Bold();


                                                column.Item()
                                                    .Border(1)
                                                    .Padding(14)
                                                    .Column(
                                                        info =>
                                                        {
                                                            info.Spacing(8);

                                                            info.Item()
                                                                .Text(
                                                                    $"Result Value: {result.ResultValue}"
                                                                )
                                                                .FontSize(15)
                                                                .Bold();

                                                            info.Item()
                                                                .Text(
                                                                    $"Reference Range: {result.ReferenceRange ?? "Not Available"}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Interpretation: {result.ResultInterpretation}"
                                                                )
                                                                .Bold();

                                                            info.Item()
                                                                .Text(
                                                                    $"Result Status: {result.ResultStatusName}"
                                                                );

                                                            info.Item()
                                                                .Text(
                                                                    $"Result Date: {(result.ResultDate.HasValue ? result.ResultDate.Value.ToString("dd/MM/yyyy hh:mm tt") : "N/A")}"
                                                                );
                                                        }
                                                    );


                                                // REFERENCE DESCRIPTION

                                                if (
                                                    !string.IsNullOrWhiteSpace(
                                                        result.ReferenceRangeDescription
                                                    )
                                                )
                                                {
                                                    column.Item()
                                                        .Text(
                                                            "Reference Description"
                                                        )
                                                        .FontSize(13)
                                                        .Bold();

                                                    column.Item()
                                                        .Text(
                                                            result.ReferenceRangeDescription
                                                        );
                                                }


                                                // REMARKS

                                                if (
                                                    !string.IsNullOrWhiteSpace(
                                                        result.Remarks
                                                    )
                                                )
                                                {
                                                    column.Item()
                                                        .Text(
                                                            "Remarks"
                                                        )
                                                        .FontSize(13)
                                                        .Bold();

                                                    column.Item()
                                                        .Text(
                                                            result.Remarks
                                                        );
                                                }


                                                // SIGNATURE

                                                column.Item()
                                                    .PaddingTop(35)
                                                    .Row(
                                                        row =>
                                                        {
                                                            row.RelativeItem()
                                                                .Column(
                                                                    tech =>
                                                                    {
                                                                        tech.Item()
                                                                            .LineHorizontal(1);

                                                                        tech.Item()
                                                                            .AlignCenter()
                                                                            .Text(
                                                                                "Lab Technician"
                                                                            );
                                                                    }
                                                                );


                                                            row.ConstantItem(
                                                                50
                                                            );


                                                            row.RelativeItem()
                                                                .Column(
                                                                    admin =>
                                                                    {
                                                                        admin.Item()
                                                                            .LineHorizontal(1);

                                                                        admin.Item()
                                                                            .AlignCenter()
                                                                            .Text(
                                                                                "Authorized Signature"
                                                                            );
                                                                    }
                                                                );
                                                        }
                                                    );
                                            }
                                        );


                                    // =========================
                                    // FOOTER
                                    // =========================

                                    page.Footer()
                                        .AlignCenter()
                                        .Text(
                                            text =>
                                            {
                                                text.Span(
                                                    "LabSync | Page "
                                                );

                                                text.CurrentPageNumber();

                                                text.Span(
                                                    " of "
                                                );

                                                text.TotalPages();
                                            }
                                        );
                                }
                            );
                        }
                    }
                );


            // =====================================================
            // GENERATE PDF
            // =====================================================

            var pdfBytes =
                document.GeneratePdf();


            // =====================================================
            // SAVE FILE
            // =====================================================

            await System.IO.File.WriteAllBytesAsync(
                filePath,
                pdfBytes
            );


            // =====================================================
            // INSERT / UPDATE REPORT TABLE
            // =====================================================

            if (existingReport != null)
            {
                await _context.Database
                    .ExecuteSqlRawAsync(
                        """
                        UPDATE Reports

                        SET
                            GeneratedBy = {0},

                            GeneratedDate = {1},

                            PdfPath = {2},

                            ApprovalStatusId = {3},

                            ApprovedBy = NULL,

                            ApprovalDate = NULL,

                            IsDelivered = 0,

                            DeliveredAt = NULL,

                            DeliveredBy = NULL

                        WHERE
                            Id = {4}
                        """,

                        generatedByUser.Id,

                        DateTime.Now,

                        pdfPath,

                        1,

                        existingReport.Id
                    );
            }
            else
            {
                await _context.Database
                    .ExecuteSqlRawAsync(
                        """
                        INSERT INTO Reports
                        (
                            AppointmentId,

                            GeneratedBy,

                            GeneratedDate,

                            PdfPath,

                            ApprovalStatusId,

                            ApprovedBy,

                            ApprovalDate,

                            IsDelivered,

                            DeliveredAt,

                            DeliveredBy
                        )

                        VALUES
                        (
                            {0},

                            {1},

                            {2},

                            {3},

                            {4},

                            NULL,

                            NULL,

                            0,

                            NULL,

                            NULL
                        )
                        """,

                        appointmentId,

                        generatedByUser.Id,

                        DateTime.Now,

                        pdfPath,

                        1
                    );
            }


            TempData["Success"] =
                "Laboratory report generated and sent for Admin approval.";


            // =====================================================
            // GO TO LAB TECHNICIAN REPORTS
            // =====================================================

            return RedirectToAction(
                "Index",
                "Reports",
                new
                {
                    area = "LabTechnician"
                }
            );
        }


        // =========================================================
        // FIND LOGGED-IN USER
        // =========================================================

        private async Task<LabUserLookupViewModel?>
            FindLoggedInUserAsync()
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


            // BY ID

            if (
                !string.IsNullOrWhiteSpace(
                    userIdClaim
                )
                &&
                int.TryParse(
                    userIdClaim,
                    out int numericUserId
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
                            numericUserId
                        )
                        .FirstOrDefaultAsync();
            }


            // BY EMAIL

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


            // BY USERNAME

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


        // =========================================================
        // REBUILD ENTRY MODEL
        // =========================================================

        private async Task RebuildEntryModelAsync(
            TestResultEntryViewModel model,
            TestResultLookupViewModel testInfo)
        {
            model.TestResultId =
                testInfo.TestResultId;

            model.AppointmentId =
                testInfo.AppointmentId;

            model.AppointmentCode =
                testInfo.AppointmentCode;

            model.AppointmentDate =
                testInfo.AppointmentDate;

            model.AppointmentTime =
                testInfo.AppointmentTime;

            model.PatientId =
                testInfo.PatientId;

            model.PatientCode =
                testInfo.PatientCode;

            model.PatientName =
                testInfo.PatientName;

            model.Phone =
                testInfo.Phone;

            model.Email =
                testInfo.Email;

            model.TestId =
                testInfo.TestId;

            model.TestCode =
                testInfo.TestCode;

            model.TestName =
                testInfo.TestName;

            model.Price =
                testInfo.Price;

            model.Unit =
                testInfo.Unit;

            model.TurnAroundTime =
                testInfo.TurnAroundTime;

            model.ReferenceRangeId =
                testInfo.ReferenceRangeId;

            model.ReferenceRange =
                testInfo.ReferenceRange;

            model.ReferenceRangeDescription =
                testInfo.ReferenceRangeDescription;

            model.SampleStatusId =
                testInfo.SampleStatusId;

            model.SampleStatusName =
                testInfo.SampleStatusName;

            model.SampleTypeId =
                testInfo.SampleTypeId;

            model.SampleTypeName =
                testInfo.SampleTypeName;


            model.ResultStatuses =
                await _context.Database
                    .SqlQueryRaw<
                        ResultStatusOptionViewModel>(
                        """
                        SELECT
                            Id,
                            StatusName
                        FROM ResultStatuses
                        ORDER BY Id
                        """
                    )
                    .ToListAsync();


            model.HasExistingResult =
                testInfo.TestResultId > 0;
        }


        // =========================================================
        // PRIVATE MODELS
        // =========================================================

        private class TestResultExistingViewModel
        {
            public int TestResultId { get; set; }

            public int AppointmentTestId { get; set; }
        }


        private class ExistingReportViewModel
        {
            public int Id { get; set; }

            public int ApprovalStatusId { get; set; }

            public string? PdfPath { get; set; }
        }
    }
}