using Labsync.Models.Data;
using LabSync.Areas.LabTechnician.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.LabTechnician.Controllers
{
    [Area("LabTechnician")]
    [Authorize]
    public class SamplesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SamplesController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // INDEX
        // SAMPLE LIST
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? appointmentId,
            string? patientId,
            int? sampleStatusId)
        {
            appointmentId =
                string.IsNullOrWhiteSpace(appointmentId)
                    ? null
                    : appointmentId.Trim();

            patientId =
                string.IsNullOrWhiteSpace(patientId)
                    ? null
                    : patientId.Trim();

            int statusFilter =
                sampleStatusId ?? 0;


            // =================================================
            // LOAD SAMPLE LIST
            // =================================================

            var samples =
                await _context.Database
                    .SqlQueryRaw<SampleListRowViewModel>(
                        """
                        SELECT

                            at.Id AS AppointmentTestId,

                            a.Id AS AppointmentId,

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

                            t.TestCode AS TestCode,

                            t.TestName AS TestName,

                            t.SampleTypeId AS SampleTypeId,

                            st.SampleTypeName AS SampleTypeName,

                            at.SampleStatusId AS SampleStatusId,

                            ISNULL(
                                ss.StatusName,
                                'Pending'
                            ) AS SampleStatusName,

                            at.CreatedAt AS CreatedAt

                        FROM AppointmentTests at

                        INNER JOIN Appointments a
                            ON a.Id = at.AppointmentId

                        INNER JOIN Patients p
                            ON p.Id = a.PatientId

                        INNER JOIN Tests t
                            ON t.Id = at.TestId

                        LEFT JOIN SampleTypes st
                            ON st.Id = t.SampleTypeId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id = at.SampleStatusId

                        WHERE

                            (
                                NULLIF({0}, '') IS NULL

                                OR UPPER(
                                    ISNULL(
                                        a.AppointmentCode,
                                        ''
                                    )
                                ) LIKE
                                    '%' +
                                    UPPER({0}) +
                                    '%'

                                OR CAST(
                                    a.Id AS VARCHAR(20)
                                ) = {0}
                            )

                            AND

                            (
                                NULLIF({1}, '') IS NULL

                                OR UPPER(
                                    ISNULL(
                                        p.PatientCode,
                                        ''
                                    )
                                ) LIKE
                                    '%' +
                                    UPPER({1}) +
                                    '%'
                            )

                            AND

                            (
                                {2} = 0

                                OR at.SampleStatusId = {2}
                            )

                        ORDER BY

                            a.AppointmentDate DESC,

                            a.AppointmentTime DESC,

                            at.Id DESC
                        """,
                        appointmentId ?? "",
                        patientId ?? "",
                        statusFilter
                    )
                    .ToListAsync();


            // =================================================
            // LOAD SAMPLE STATUSES
            // =================================================

            var statuses =
                await _context.Database
                    .SqlQueryRaw<SampleStatusOptionViewModel>(
                        """
                        SELECT
                            Id,
                            StatusName
                        FROM SampleStatuses
                        ORDER BY Id
                        """
                    )
                    .ToListAsync();


            // =================================================
            // VIEW BAG
            // =================================================

            ViewBag.AppointmentId =
                appointmentId ?? "";

            ViewBag.PatientId =
                patientId ?? "";

            ViewBag.SampleStatusId =
                sampleStatusId;

            ViewBag.SampleStatuses =
                statuses;


            ViewData["ActiveMenu"] =
                "Samples";


            return View(samples);
        }


        // =====================================================
        // CREATE / COLLECT SAMPLE - GET
        // =====================================================

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


            // =================================================
            // LOAD SAMPLE DETAILS
            // =================================================

            var sample =
                await _context.Database
                    .SqlQueryRaw<SampleCollectionViewModel>(
                        """
                        SELECT TOP 1

                            at.Id AS AppointmentTestId,

                            a.Id AS AppointmentId,

                            a.AppointmentCode AS AppointmentCode,

                            a.AppointmentDate AS AppointmentDate,

                            a.AppointmentTime AS AppointmentTime,

                            p.Id AS PatientId,

                            p.PatientCode AS PatientCode,

                            CONCAT(
                                p.FirstName,
                                ' ',
                                p.LastName
                            ) AS PatientName,

                            p.Phone AS Phone,

                            p.Email AS Email,

                            t.Id AS TestId,

                            t.TestCode AS TestCode,

                            t.TestName AS TestName,

                            t.Price AS Price,

                            t.Unit AS Unit,

                            CAST(
                                t.TurnAroundTime
                                AS VARCHAR(100)
                            ) AS TurnAroundTime,

                            t.IsActive AS IsActive,

                            t.SampleTypeId AS SampleTypeId,

                            st.SampleTypeName
                                AS SampleTypeName,

                            st.Description
                                AS SampleTypeDescription,

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

                        LEFT JOIN SampleTypes st
                            ON st.Id = t.SampleTypeId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id = at.SampleStatusId

                        WHERE at.Id = {0}
                        """,
                        appointmentTestId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // NOT FOUND
            // =================================================

            if (sample == null)
            {
                TempData["Error"] =
                    "The selected sample could not be found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            ViewData["ActiveMenu"] =
                "Samples";


            return View(sample);
        }


        // =====================================================
        // CONFIRM SAMPLE COLLECTION - POST
        //
        // IMPORTANT:
        // This is NOT named Create.
        // This avoids the CS0111 duplicate Create(int) error.
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCollection(
            SampleCollectionViewModel model)
        {
            // =================================================
            // VALIDATE
            // =================================================

            if (model.AppointmentTestId <= 0)
            {
                TempData["Error"] =
                    "Invalid appointment test.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =================================================
            // GET CURRENT SAMPLE INFORMATION
            // =================================================

            var sampleInfo =
                await _context.Database
                    .SqlQueryRaw<SampleUpdateLookupViewModel>(
                        """
                        SELECT TOP 1

                            at.Id AS AppointmentTestId,

                            at.AppointmentId AS AppointmentId,

                            a.AppointmentCode AS AppointmentCode,

                            at.SampleStatusId AS SampleStatusId,

                            ISNULL(
                                ss.StatusName,
                                'Pending'
                            ) AS SampleStatusName

                        FROM AppointmentTests at

                        INNER JOIN Appointments a
                            ON a.Id = at.AppointmentId

                        LEFT JOIN SampleStatuses ss
                            ON ss.Id = at.SampleStatusId

                        WHERE at.Id = {0}
                        """,
                        model.AppointmentTestId
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // NOT FOUND
            // =================================================

            if (sampleInfo == null)
            {
                TempData["Error"] =
                    "The selected appointment test was not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }


            // =================================================
            // GET COLLECTED STATUS
            // =================================================

            var collectedStatus =
                await _context.Database
                    .SqlQueryRaw<SampleStatusOptionViewModel>(
                        """
                        SELECT TOP 1

                            Id,

                            StatusName

                        FROM SampleStatuses

                        WHERE LOWER(
                            LTRIM(
                                RTRIM(StatusName)
                            )
                        ) = 'collected'

                        ORDER BY Id
                        """
                    )
                    .FirstOrDefaultAsync();


            // =================================================
            // STATUS NOT FOUND
            // =================================================

            if (collectedStatus == null)
            {
                TempData["Error"] =
                    "Collected status was not found in SampleStatuses.";

                return RedirectToAction(
                    "Create",
                    "Samples",
                    new
                    {
                        area = "LabTechnician",
                        appointmentTestId =
                            model.AppointmentTestId
                    }
                );
            }


            // =================================================
            // CHECK ALREADY COLLECTED
            // =================================================

            var currentStatus =
                (
                    sampleInfo.SampleStatusName
                    ?? ""
                ).Trim();


            if (
                currentStatus.Equals(
                    "Collected",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                TempData["Success"] =
                    "Sample has already been collected.";

                return RedirectToAction(
                    "Index",
                    "Tests",
                    new
                    {
                        area = "LabTechnician",
                        appointmentId =
                            sampleInfo.AppointmentCode
                            ?? sampleInfo.AppointmentId.ToString()
                    }
                );
            }


            // =================================================
            // UPDATE SAMPLE STATUS
            // =================================================

            var affectedRows =
                await _context.Database
                    .ExecuteSqlRawAsync(
                        """
                        UPDATE AppointmentTests

                        SET SampleStatusId = {0}

                        WHERE Id = {1}
                        """,
                        collectedStatus.Id,
                        model.AppointmentTestId
                    );


            // =================================================
            // UPDATE FAILED
            // =================================================

            if (affectedRows == 0)
            {
                TempData["Error"] =
                    "Sample status could not be updated.";

                return RedirectToAction(
                    "Create",
                    "Samples",
                    new
                    {
                        area = "LabTechnician",
                        appointmentTestId =
                            model.AppointmentTestId
                    }
                );
            }


            // =================================================
            // SUCCESS
            // =================================================

            TempData["Success"] =
                "Sample collected successfully.";


            // =================================================
            // RETURN TO TESTS
            // USE APPOINTMENT CODE
            // =================================================

            return RedirectToAction(
                "Index",
                "Tests",
                new
                {
                    area = "LabTechnician",

                    appointmentId =
                        sampleInfo.AppointmentCode
                        ?? sampleInfo.AppointmentId.ToString()
                }
            );
        }


        // =====================================================
        // LOOKUP MODEL
        // =====================================================

        private class SampleUpdateLookupViewModel
        {
            public int AppointmentTestId { get; set; }

            public int AppointmentId { get; set; }

            public string? AppointmentCode { get; set; }

            public int SampleStatusId { get; set; }

            public string? SampleStatusName { get; set; }
        }
    }
}