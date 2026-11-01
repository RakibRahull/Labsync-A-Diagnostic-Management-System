using Labsync.Models.Data;
using LabSync.Areas.LabTechnician.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.LabTechnician.Controllers
{
    [Area("LabTechnician")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // LAB TECHNICIAN APPOINTMENT WORKLIST
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? selectedDate,
            string? appointmentId)
        {
            // =================================================
            // SELECTED DATE
            // =================================================

            DateTime appointmentDate =
                selectedDate?.Date ?? DateTime.Today;


            // =================================================
            // CLEAN SEARCH
            // =================================================

            appointmentId =
                string.IsNullOrWhiteSpace(appointmentId)
                    ? null
                    : appointmentId.Trim();


            // =================================================
            // VIEW MODEL
            // =================================================

            var model =
                new LabTechnicianAppointmentViewModel
                {
                    SearchAppointmentId = appointmentId,

                    SelectedDate = appointmentDate
                };


            // =================================================
            // GET APPOINTMENTS
            // =================================================

            if (string.IsNullOrWhiteSpace(appointmentId))
            {
                model.Appointments =
                    await _context.Database
                    .SqlQueryRaw<LabTechnicianAppointmentRowViewModel>(
                        """
                        SELECT

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

                            a.AppointmentDate AS AppointmentDate,

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

                        GROUP BY

                            a.Id,

                            a.AppointmentCode,

                            p.Id,

                            p.PatientCode,

                            p.FirstName,

                            p.LastName,

                            p.Phone,

                            a.AppointmentDate,

                            a.AppointmentTime,

                            ast.StatusName,

                            a.Remarks

                        ORDER BY

                            a.AppointmentDate ASC,

                            a.AppointmentTime ASC,

                            a.Id ASC
                        """,
                        appointmentDate
                    )
                    .ToListAsync();
            }
            else
            {
                // =================================================
                // SEARCH BY NUMERIC APPOINTMENT ID
                // =================================================

                int numericAppointmentId;

                if (int.TryParse(
                    appointmentId,
                    out numericAppointmentId))
                {
                    model.Appointments =
                        await _context.Database
                        .SqlQueryRaw<LabTechnicianAppointmentRowViewModel>(
                            """
                            SELECT

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

                                a.AppointmentDate AS AppointmentDate,

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

                            WHERE a.Id = {0}

                            GROUP BY

                                a.Id,

                                a.AppointmentCode,

                                p.Id,

                                p.PatientCode,

                                p.FirstName,

                                p.LastName,

                                p.Phone,

                                a.AppointmentDate,

                                a.AppointmentTime,

                                ast.StatusName,

                                a.Remarks

                            ORDER BY

                                a.AppointmentDate ASC,

                                a.AppointmentTime ASC
                            """,
                            numericAppointmentId
                        )
                        .ToListAsync();
                }
                else
                {
                    // =================================================
                    // SEARCH BY APPOINTMENT CODE
                    // =================================================

                    model.Appointments =
                        await _context.Database
                        .SqlQueryRaw<LabTechnicianAppointmentRowViewModel>(
                            """
                            SELECT

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

                                a.AppointmentDate AS AppointmentDate,

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

                            WHERE a.AppointmentCode LIKE '%' + {0} + '%'

                            GROUP BY

                                a.Id,

                                a.AppointmentCode,

                                p.Id,

                                p.PatientCode,

                                p.FirstName,

                                p.LastName,

                                p.Phone,

                                a.AppointmentDate,

                                a.AppointmentTime,

                                ast.StatusName,

                                a.Remarks

                            ORDER BY

                                a.AppointmentDate ASC,

                                a.AppointmentTime ASC,

                                a.Id ASC
                            """,
                            appointmentId
                        )
                        .ToListAsync();
                }
            }


            // =================================================
            // TOTAL
            // =================================================

            model.TotalAppointments =
                model.Appointments.Count;


            // =================================================
            // VIEW DATA
            // =================================================

            ViewData["ActiveMenu"] =
                "Appointments";


            ViewBag.SelectedDate =
                appointmentDate.ToString(
                    "yyyy-MM-dd"
                );


            ViewBag.SelectedDateDisplay =
                appointmentDate.ToString(
                    "dd MMM yyyy"
                );


            // =================================================
            // RETURN VIEW
            // =================================================

            return View(model);
        }
    }
}
