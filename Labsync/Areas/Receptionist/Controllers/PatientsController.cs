using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.Receptionist.Controllers
{
    [Area("Receptionist")]
    [Authorize(Roles = "Receptionist")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================
        // PATIENT LIST
        // GET: /Receptionist/Patients
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? patientId,
            DateTime? registrationDate)
        {
            var query = _context.Patients
                .AsNoTracking()
                .AsQueryable();


            // =========================================
            // SEARCH BY PATIENT CODE / NAME / PHONE
            // =========================================

            if (!string.IsNullOrWhiteSpace(patientId))
            {
                patientId = patientId.Trim();

                query = query.Where(p =>
                    (p.PatientCode != null &&
                     p.PatientCode.Contains(patientId))
                    ||
                    p.FirstName.Contains(patientId)
                    ||
                    p.LastName.Contains(patientId)
                    ||
                    p.Phone.Contains(patientId));
            }


            // =========================================
            // FILTER BY REGISTRATION DATE
            // =========================================

            if (registrationDate.HasValue)
            {
                var date = registrationDate.Value.Date;
                var nextDate = date.AddDays(1);

                query = query.Where(p =>
                    p.CreatedAt >= date &&
                    p.CreatedAt < nextDate);
            }


            // =========================================
            // NEWEST PATIENT FIRST
            // =========================================

            var patients = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();


            // Keep filter values
            ViewBag.PatientId = patientId;

            ViewBag.RegistrationDate =
                registrationDate?.ToString("yyyy-MM-dd");


            return View(patients);
        }


        // =========================================
        // CREATE PATIENT - GET
        // =========================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Patient());
        }


        // =========================================
        // CREATE PATIENT - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            // PatientCode is generated automatically.
            ModelState.Remove(nameof(Patient.PatientCode));

            // Email is optional.
            ModelState.Remove(nameof(Patient.Email));

            // NID is optional.
            ModelState.Remove(nameof(Patient.NID));


            if (!ModelState.IsValid)
            {
                return View(patient);
            }


            try
            {
                // =========================================
                // REGISTRATION DATE
                // =========================================

                patient.CreatedAt = DateTime.Now;


                // =========================================
                // GENERATE NEXT PATIENT CODE
                //
                // Existing:
                // PAT1
                // PAT2
                // ...
                // PAT12
                //
                // New:
                // PAT13
                // =========================================

                var existingCodes = await _context.Patients
                    .AsNoTracking()
                    .Where(p =>
                        p.PatientCode != null &&
                        p.PatientCode.StartsWith("PAT"))
                    .Select(p => p.PatientCode!)
                    .ToListAsync();


                int maxPatientNumber = 0;


                foreach (var code in existingCodes)
                {
                    if (code.Length <= 3)
                    {
                        continue;
                    }


                    var numberPart =
                        code.Substring(3);


                    if (int.TryParse(
                        numberPart,
                        out int number))
                    {
                        if (number > maxPatientNumber)
                        {
                            maxPatientNumber = number;
                        }
                    }
                }


                int nextPatientNumber =
                    maxPatientNumber + 1;


                patient.PatientCode =
                    $"PAT{nextPatientNumber}";


                // =========================================
                // SAVE PATIENT
                // =========================================

                _context.Patients.Add(patient);

                await _context.SaveChangesAsync();


                // =========================================
                // SUCCESS MESSAGE
                // =========================================

                TempData["Success"] =
                    $"Patient registered successfully. " +
                    $"Patient ID: {patient.PatientCode}";


                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }


        // =========================================
        // PATIENT DETAILS
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);


            if (patient == null)
            {
                return NotFound();
            }


            return View(patient);
        }


        // =========================================
        // EDIT PATIENT - GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);


            if (patient == null)
            {
                return NotFound();
            }


            return View(patient);
        }


        // =========================================
        // EDIT PATIENT - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Patient patient)
        {
            if (id != patient.Id)
            {
                return BadRequest();
            }


            // PatientCode should never be changed manually.
            ModelState.Remove(nameof(Patient.PatientCode));


            // Optional fields
            ModelState.Remove(nameof(Patient.Email));
            ModelState.Remove(nameof(Patient.NID));


            if (!ModelState.IsValid)
            {
                return View(patient);
            }


            try
            {
                var existingPatient =
                    await _context.Patients
                        .FirstOrDefaultAsync(p => p.Id == id);


                if (existingPatient == null)
                {
                    return NotFound();
                }


                // =========================================
                // DO NOT CHANGE:
                //
                // Id
                // PatientCode
                // CreatedAt
                // =========================================

                existingPatient.FirstName =
                    patient.FirstName;

                existingPatient.LastName =
                    patient.LastName;

                existingPatient.Phone =
                    patient.Phone;

                existingPatient.Email =
                    patient.Email;

                existingPatient.Address =
                    patient.Address;

                existingPatient.CityId =
                    patient.CityId;

                existingPatient.DateOfBirth =
                    patient.DateOfBirth;

                existingPatient.Gender =
                    patient.Gender;

                existingPatient.Religion =
                    patient.Religion;

                existingPatient.MaritalStatus =
                    patient.MaritalStatus;

                existingPatient.BloodGroup =
                    patient.BloodGroup;

                existingPatient.FatherName =
                    patient.FatherName;

                existingPatient.MotherName =
                    patient.MotherName;

                existingPatient.NID =
                    patient.NID;

                existingPatient.Occupation =
                    patient.Occupation;


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Patient updated successfully.";


                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists =
                    await _context.Patients
                        .AnyAsync(p => p.Id == id);


                if (!exists)
                {
                    return NotFound();
                }


                throw;
            }
        }


        // =========================================
        // DELETE PATIENT - GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);


            if (patient == null)
            {
                TempData["Error"] =
                    "Patient record was not found.";

                return RedirectToAction(nameof(Index));
            }


            return View(patient);
        }


        // =========================================
        // DELETE PATIENT - POST
        // =========================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);


            if (patient == null)
            {
                TempData["Error"] =
                    $"Patient ID {id} was not found.";

                return RedirectToAction(nameof(Index));
            }


            try
            {
                _context.Patients.Remove(patient);

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    $"Patient {patient.PatientCode} deleted successfully.";


                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                var errorMessage =
                    ex.GetBaseException().Message;


                TempData["Error"] =
                    "Patient could not be deleted. " +
                    "Database error: " +
                    errorMessage;


                return RedirectToAction(
                    nameof(Delete),
                    new { id }
                );
            }
        }
    }
}