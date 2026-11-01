using LabSync.Models;
using Labsync.Models.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.Receptionist.Controllers
{
    [Area("Receptionist")]
    [Authorize(Roles = "Receptionist")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? appointmentId,
            string? patientId,
            DateTime? appointmentDate)
        {
            IQueryable<Appointment> query =
                _context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Patient);


            // =================================================
            // SEARCH BY APPOINTMENT ID / CODE
            // =================================================

            if (!string.IsNullOrWhiteSpace(appointmentId))
            {
                appointmentId =
                    appointmentId.Trim().ToUpper();

                query = query.Where(a =>
                    a.AppointmentCode != null &&
                    a.AppointmentCode
                        .ToUpper()
                        .Contains(appointmentId));
            }


            // =================================================
            // SEARCH BY PATIENT ID / CODE
            // =================================================

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                patientId =
                    patientId.Trim().ToUpper();

                query = query.Where(a =>
                    a.Patient != null &&
                    a.Patient.PatientCode != null &&
                    a.Patient.PatientCode
                        .ToUpper()
                        .Contains(patientId));
            }


            // =================================================
            // FILTER BY APPOINTMENT DATE
            // =================================================

            if (appointmentDate.HasValue)
            {
                var selectedDate =
                    appointmentDate.Value.Date;

                var nextDate =
                    selectedDate.AddDays(1);

                query = query.Where(a =>
                    a.AppointmentDate >= selectedDate &&
                    a.AppointmentDate < nextDate);
            }


            // =================================================
            // LOAD APPOINTMENTS
            // =================================================

            var appointments =
                await query
                    .OrderByDescending(a =>
                        a.AppointmentDate)
                    .ThenByDescending(a =>
                        a.AppointmentTime)
                    .ThenByDescending(a =>
                        a.Id)
                    .ToListAsync();


            // =================================================
            // FILTER VALUES
            // =================================================

            ViewBag.AppointmentId =
                appointmentId ?? "";

            ViewBag.PatientId =
                patientId ?? "";

            ViewBag.AppointmentDate =
                appointmentDate?
                    .ToString("yyyy-MM-dd")
                    ?? "";


            return View(appointments);
        }


        // =====================================================
        // CREATE - GET
        //
        // Automatic
        

        [HttpGet]
        public async Task<IActionResult> Create(
            string? patientCode)
        {
            

            await LoadDropdowns();


            // =================================================
            // NEW APPOINTMENT
            // =================================================

            var appointment =
                new Appointment
                {
                    AppointmentDate =
                        DateTime.Today,

                    AppointmentTime =
                        DateTime.Now.TimeOfDay
                };


            // =================================================
            // DEFAULT STATUS = PENDING
            // =================================================

            var pendingStatus =
                await _context
                    .AppointmentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.StatusName == "Pending");


            if (pendingStatus != null)
            {
                appointment.AppointmentStatusId =
                    pendingStatus.Id;
            }


            // =================================================
            // AUTO SELECT PATIENT BY PATIENT CODE
            // =================================================

            if (!string.IsNullOrWhiteSpace(patientCode))
            {
                patientCode =
                    patientCode.Trim();


                var patient =
                    await _context.Patients
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.PatientCode != null &&
                            p.PatientCode.ToLower() ==
                            patientCode.ToLower());


                // =============================================
                // PATIENT FOUND
                // =============================================

                if (patient != null)
                {
                    appointment.PatientId =
                        patient.Id;

                    appointment.Patient =
                        patient;
                }


                // =============================================
                // PATIENT NOT FOUND
                // =============================================

                else
                {
                    TempData["Error"] =
                        $"Patient ID '{patientCode}' was not found.";
                }
            }


            // =================================================
            // SELECTED TESTS
            // =================================================

            ViewBag.SelectedTestIds =
                new List<int>();


            return View(appointment);
        }


        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Appointment appointment,
            List<int>? SelectedTestIds)
        {
            SelectedTestIds ??=
                new List<int>();


            // =================================================
            // AUTO GENERATED VALUES
            // =================================================

            ModelState.Remove(
                nameof(Appointment.AppointmentCode));

            ModelState.Remove(
                nameof(Appointment.CreatedAt));


            // =================================================
            // VALIDATE PATIENT
            // =================================================

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(p =>
                        p.Id ==
                        appointment.PatientId);


            if (patient == null)
            {
                ModelState.AddModelError(
                    "PatientId",
                    "Please search and select a valid Patient ID."
                );
            }


            // =================================================
            // VALIDATE REFERRAL DOCTOR
            // =================================================

            if (appointment.ReferralDoctorId.HasValue)
            {
                var doctorExists =
                    await _context.ReferralDoctors
                        .AnyAsync(d =>
                            d.Id ==
                            appointment
                                .ReferralDoctorId
                                .Value);


                if (!doctorExists)
                {
                    ModelState.AddModelError(
                        "ReferralDoctorId",
                        "The entered Referral Doctor ID does not exist."
                    );
                }
            }


            // =================================================
            // VALIDATE BRANCH
            // =================================================

            var branchExists =
                await _context.Branches
                    .AnyAsync(b =>
                        b.Id ==
                        appointment.BranchId);


            if (!branchExists)
            {
                ModelState.AddModelError(
                    "BranchId",
                    "Please select a valid Branch."
                );
            }


            // =================================================
            // VALIDATE TEST SELECTION
            // =================================================

            if (SelectedTestIds.Count == 0)
            {
                ModelState.AddModelError(
                    "SelectedTestIds",
                    "Please select at least one test."
                );
            }


            // =================================================
            // VALIDATE TEST IDs
            // =================================================

            if (SelectedTestIds.Count > 0)
            {
                var validTestIds =
                    await _context.Tests
                        .Where(t =>
                            t.IsActive == true &&
                            SelectedTestIds.Contains(t.Id))
                        .Select(t => t.Id)
                        .ToListAsync();


                var invalidTests =
                    SelectedTestIds
                        .Except(validTestIds)
                        .Any();


                if (invalidTests)
                {
                    ModelState.AddModelError(
                        "SelectedTestIds",
                        "One or more selected tests are invalid or inactive."
                    );
                }
            }


            // =================================================
            // VALIDATION FAILED
            // =================================================

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();


                if (patient != null)
                {
                    appointment.Patient =
                        patient;
                }


                ViewBag.SelectedTestIds =
                    SelectedTestIds;


                return View(appointment);
            }


            // =================================================
            // FIND PENDING STATUS
            // =================================================

            var appointmentStatus =
                await _context
                    .AppointmentStatuses
                    .FirstOrDefaultAsync(s =>
                        s.StatusName == "Pending");


            if (appointmentStatus == null)
            {
                appointmentStatus =
                    await _context
                        .AppointmentStatuses
                        .OrderBy(s => s.Id)
                        .FirstOrDefaultAsync();
            }


            if (appointmentStatus == null)
            {
                ModelState.AddModelError(
                    "",
                    "No appointment status is available."
                );


                await LoadDropdowns();


                appointment.Patient =
                    patient;


                ViewBag.SelectedTestIds =
                    SelectedTestIds;


                return View(appointment);
            }


            // =================================================
            // ASSIGN STATUS
            // =================================================

            appointment.AppointmentStatusId =
                appointmentStatus.Id;


            // =================================================
            // GENERATE SEQUENTIAL APPOINTMENT CODE
            //
            // APT1
            // APT2
            // APT3
            // APT4
            // APT5
            // ...
            // =================================================

            var existingCodes =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(a =>
                        a.AppointmentCode != null &&
                        a.AppointmentCode.StartsWith("APT"))
                    .Select(a =>
                        a.AppointmentCode!)
                    .ToListAsync();


            int maxAppointmentNumber = 0;


            foreach (var code in existingCodes)
            {
                // =============================================
                // APT এর পরে কিছু আছে কিনা check
                // =============================================

                if (code.Length <= 3)
                {
                    continue;
                }


                // =============================================
                // APT বাদ দিয়ে number নেওয়া
                //
                // APT1 -> 1
                // APT25 -> 25
                // =============================================

                var numberPart =
                    code.Substring(3);


                if (
                    int.TryParse(
                        numberPart,
                        out int number)
                    &&
                    number > maxAppointmentNumber
                )
                {
                    maxAppointmentNumber =
                        number;
                }
            }


            // =============================================
            // NEXT APPOINTMENT CODE
            //
            // Empty table -> APT1
            // APT1 exists -> APT2
            // APT2 exists -> APT3
            // =============================================

            appointment.AppointmentCode =
                $"APT{maxAppointmentNumber + 1}";


            // =================================================
            // CREATED AT
            // =================================================

            appointment.CreatedAt =
                DateTime.Now;


            // =================================================
            // DATABASE TRANSACTION
            // =================================================

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                // =============================================
                // SAVE APPOINTMENT
                // =============================================

                _context.Appointments.Add(
                    appointment
                );


                await _context.SaveChangesAsync();


                // =============================================
                // SAVE APPOINTMENT TESTS
                //
                // SampleStatusId = 1 = Pending
                // =============================================

                foreach (
                    var testId in
                    SelectedTestIds.Distinct())
                {
                    await _context.Database
                        .ExecuteSqlRawAsync(
                            @"
                            INSERT INTO AppointmentTests
                            (
                                AppointmentId,
                                TestId,
                                SampleStatusId,
                                CreatedAt
                            )
                            VALUES
                            (
                                {0},
                                {1},
                                {2},
                                {3}
                            )
                            ",
                            appointment.Id,
                            testId,
                            1,
                            DateTime.Now
                        );
                }


                // =============================================
                // COMMIT
                // =============================================

                await transaction.CommitAsync();


                // =============================================
                // SUCCESS MESSAGE
                // =============================================

                TempData["Success"] =
                    $"Appointment {appointment.AppointmentCode} added successfully.";


                // =============================================
                // REDIRECT TO INVOICE CREATE
                // =============================================

                return RedirectToAction(
                    "Create",
                    "Invoices",
                    new
                    {
                        area = "Receptionist",
                        appointmentId =
                            appointment.AppointmentCode
                    }
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();


                var errorMessage =
                    ex.Message;


                if (ex.InnerException != null)
                {
                    errorMessage +=
                        " | Inner Error: " +
                        ex.InnerException.Message;
                }


                ModelState.AddModelError(
                    "",
                    "Save Error: " +
                    errorMessage
                );


                await LoadDropdowns();


                appointment.Patient =
                    patient;


                ViewBag.SelectedTestIds =
                    SelectedTestIds;


                return View(appointment);
            }
        }


        // =====================================================
        // FIND PATIENT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> FindPatient(
            string? patientCode)
        {
            if (string.IsNullOrWhiteSpace(patientCode))
            {
                return Json(new
                {
                    success = false,

                    message =
                        "Please enter Patient ID."
                });
            }


            var code =
                patientCode.Trim();


            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.PatientCode != null &&
                        p.PatientCode.ToLower() ==
                        code.ToLower());


            if (patient == null)
            {
                return Json(new
                {
                    success = false,

                    message =
                        $"Patient ID '{code}' was not found."
                });
            }


            return Json(new
            {
                success = true,

                patientId =
                    patient.Id,

                patientCode =
                    patient.PatientCode,

                patientName =
                    $"{patient.FirstName} {patient.LastName}",

                phone =
                    string.IsNullOrWhiteSpace(
                        patient.Phone)
                        ? "N/A"
                        : patient.Phone,

                email =
                    string.IsNullOrWhiteSpace(
                        patient.Email)
                        ? "N/A"
                        : patient.Email,

                gender =
                    string.IsNullOrWhiteSpace(
                        patient.Gender)
                        ? "N/A"
                        : patient.Gender,

                dateOfBirth =
                    patient.DateOfBirth.HasValue
                        ? patient.DateOfBirth.Value
                            .ToString("dd MMM yyyy")
                        : "N/A",

                bloodGroup =
                    string.IsNullOrWhiteSpace(
                        patient.BloodGroup)
                        ? "N/A"
                        : patient.BloodGroup,

                address =
                    string.IsNullOrWhiteSpace(
                        patient.Address)
                        ? "N/A"
                        : patient.Address
            });
        }


        // =====================================================
        // FIND REFERRAL DOCTOR
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            FindReferralDoctor(int id)
        {
            if (id <= 0)
            {
                return Json(new
                {
                    success = false,

                    message =
                        "Please enter a valid Referral Doctor ID."
                });
            }


            var doctor =
                await _context.ReferralDoctors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d =>
                        d.Id == id);


            if (doctor == null)
            {
                return Json(new
                {
                    success = false,

                    message =
                        $"Referral Doctor ID '{id}' was not found."
                });
            }


            return Json(new
            {
                success = true,

                doctorId =
                    doctor.Id,

                doctorName =
                    doctor.DoctorName,

                specialization =
                    string.IsNullOrWhiteSpace(
                        doctor.Specialization)
                        ? "N/A"
                        : doctor.Specialization,

                hospitalName =
                    string.IsNullOrWhiteSpace(
                        doctor.HospitalName)
                        ? "N/A"
                        : doctor.HospitalName,

                phone =
                    string.IsNullOrWhiteSpace(
                        doctor.Phone)
                        ? "N/A"
                        : doctor.Phone
            });
        }


        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            // =================================================
            // APPOINTMENT + PATIENT
            // =================================================

            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (appointment == null)
            {
                return NotFound();
            }


            // =================================================
            // STATUS
            // =================================================

            var appointmentStatus =
                await _context
                    .AppointmentStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.Id ==
                        appointment.AppointmentStatusId);


            ViewBag.AppointmentStatus =
                appointmentStatus?.StatusName
                ?? "N/A";


            // =================================================
            // ASSIGNED TESTS
            // =================================================

            var assignedTests =
                await _context.Database
                    .SqlQueryRaw<
                        AppointmentTestDetailsViewModel
                    >(
                        @"
                        SELECT
                            at.AppointmentId AS AppointmentId,
                            at.TestId AS TestId,
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
                        appointment.Id
                    )
                    .ToListAsync();


            ViewBag.AssignedTests =
                assignedTests;


            return View(appointment);
        }


        // =====================================================
        // EDIT - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (appointment == null)
            {
                return NotFound();
            }


            await LoadDropdowns();


            var selectedTestIds =
                await _context.Database
                    .SqlQueryRaw<int>(
                        @"
                        SELECT TestId
                        FROM AppointmentTests
                        WHERE AppointmentId = {0}
                        ",
                        appointment.Id
                    )
                    .ToListAsync();


            ViewBag.SelectedTestIds =
                selectedTestIds;


            return View(appointment);
        }


        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Appointment appointment,
            List<int>? SelectedTestIds)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }


            SelectedTestIds ??=
                new List<int>();


            ModelState.Remove(
                nameof(Appointment.AppointmentCode));

            ModelState.Remove(
                nameof(Appointment.CreatedAt));


            // =================================================
            // PATIENT
            // =================================================

            var patientExists =
                await _context.Patients
                    .AnyAsync(p =>
                        p.Id ==
                        appointment.PatientId);


            if (!patientExists)
            {
                ModelState.AddModelError(
                    "PatientId",
                    "Please search and select a valid Patient ID."
                );
            }


            // =================================================
            // DOCTOR
            // =================================================

            if (appointment.ReferralDoctorId.HasValue)
            {
                var doctorExists =
                    await _context.ReferralDoctors
                        .AnyAsync(d =>
                            d.Id ==
                            appointment
                                .ReferralDoctorId
                                .Value);


                if (!doctorExists)
                {
                    ModelState.AddModelError(
                        "ReferralDoctorId",
                        "The entered Referral Doctor ID does not exist."
                    );
                }
            }


            // =================================================
            // BRANCH
            // =================================================

            var branchExists =
                await _context.Branches
                    .AnyAsync(b =>
                        b.Id ==
                        appointment.BranchId);


            if (!branchExists)
            {
                ModelState.AddModelError(
                    "BranchId",
                    "Please select a valid Branch."
                );
            }


            // =================================================
            // TESTS
            // =================================================

            if (SelectedTestIds.Count == 0)
            {
                ModelState.AddModelError(
                    "SelectedTestIds",
                    "Please select at least one test."
                );
            }


            // =================================================
            // VALIDATE ACTIVE TESTS
            // =================================================

            if (SelectedTestIds.Count > 0)
            {
                var validTestIds =
                    await _context.Tests
                        .Where(t =>
                            t.IsActive == true &&
                            SelectedTestIds.Contains(t.Id))
                        .Select(t => t.Id)
                        .ToListAsync();


                if (
                    SelectedTestIds
                        .Except(validTestIds)
                        .Any()
                )
                {
                    ModelState.AddModelError(
                        "SelectedTestIds",
                        "One or more selected tests are invalid or inactive."
                    );
                }
            }


            // =================================================
            // VALIDATION FAILED
            // =================================================

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();


                ViewBag.SelectedTestIds =
                    SelectedTestIds;


                appointment.Patient =
                    await _context.Patients
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.Id ==
                            appointment.PatientId);


                return View(appointment);
            }


            // =================================================
            // UPDATE APPOINTMENT
            // =================================================

            try
            {
                var existingAppointment =
                    await _context.Appointments
                        .FirstOrDefaultAsync(a =>
                            a.Id == id);


                if (existingAppointment == null)
                {
                    return NotFound();
                }


                existingAppointment.PatientId =
                    appointment.PatientId;

                existingAppointment.ReferralDoctorId =
                    appointment.ReferralDoctorId;

                existingAppointment.BranchId =
                    appointment.BranchId;

                existingAppointment.AppointmentDate =
                    appointment.AppointmentDate;

                existingAppointment.AppointmentTime =
                    appointment.AppointmentTime;

                existingAppointment.Remarks =
                    appointment.Remarks;


                await _context.SaveChangesAsync();


                // =============================================
                // DELETE OLD TESTS
                // =============================================

                await _context.Database
                    .ExecuteSqlRawAsync(
                        @"
                        DELETE FROM AppointmentTests
                        WHERE AppointmentId = {0}
                        ",
                        id
                    );


                // =============================================
                // INSERT NEW TESTS
                // =============================================

                foreach (
                    var testId in
                    SelectedTestIds.Distinct())
                {
                    await _context.Database
                        .ExecuteSqlRawAsync(
                            @"
                            INSERT INTO AppointmentTests
                            (
                                AppointmentId,
                                TestId,
                                SampleStatusId,
                                CreatedAt
                            )
                            VALUES
                            (
                                {0},
                                {1},
                                {2},
                                {3}
                            )
                            ",
                            id,
                            testId,
                            1,
                            DateTime.Now
                        );
                }


                TempData["Success"] =
                    "Appointment updated successfully.";


                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(
                    appointment.Id))
                {
                    return NotFound();
                }


                throw;
            }
        }


        // =====================================================
        // DELETE - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var appointment =
                await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (appointment == null)
            {
                return NotFound();
            }


            return View(appointment);
        }


        // =====================================================
        // DELETE - POST
        // =====================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            DeleteConfirmed(int id)
        {
            var appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (appointment != null)
            {
                // =============================================
                // DELETE TESTS FIRST
                // =============================================

                await _context.Database
                    .ExecuteSqlRawAsync(
                        @"
                        DELETE FROM AppointmentTests
                        WHERE AppointmentId = {0}
                        ",
                        id
                    );


                // =============================================
                // DELETE APPOINTMENT
                // =============================================

                _context.Appointments.Remove(
                    appointment
                );


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Appointment deleted successfully.";
            }


            return RedirectToAction(
                nameof(Index)
            );
        }


        // =====================================================
        // LOAD DROPDOWNS
        // =====================================================

        private async Task LoadDropdowns()
        {
            // =================================================
            // BRANCHES
            // =================================================

            ViewBag.Branches =
                new SelectList(
                    await _context.Branches
                        .OrderBy(b =>
                            b.BranchName)
                        .ToListAsync(),
                    "Id",
                    "BranchName"
                );


            // =================================================
            // APPOINTMENT STATUSES
            // =================================================

            ViewBag.AppointmentStatuses =
                new SelectList(
                    await _context
                        .AppointmentStatuses
                        .OrderBy(s =>
                            s.Id)
                        .ToListAsync(),
                    "Id",
                    "StatusName"
                );


            // =================================================
            // ACTIVE TESTS
            // =================================================

            ViewBag.Tests =
                await _context.Tests
                    .Where(t =>
                        t.IsActive == true)
                    .OrderBy(t =>
                        t.TestName)
                    .ToListAsync();
        }


        // =====================================================
        // APPOINTMENT EXISTS
        // =====================================================

        private bool AppointmentExists(
            int id)
        {
            return _context.Appointments
                .Any(a =>
                    a.Id == id);
        }


        // =====================================================
        // APPOINTMENT TEST DETAILS VIEW MODEL
        // =====================================================

        public class AppointmentTestDetailsViewModel
        {
            public int AppointmentId
            {
                get;
                set;
            }


            public int TestId
            {
                get;
                set;
            }


            public string? TestCode
            {
                get;
                set;
            }


            public string TestName
            {
                get;
                set;
            }
                = string.Empty;


            public decimal Price
            {
                get;
                set;
            }


            public string? Unit
            {
                get;
                set;
            }


            public int? TurnAroundTime
            {
                get;
                set;
            }
        }
    }
}