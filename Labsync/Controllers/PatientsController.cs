using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // PATIENT LIST
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(patients);
        }


        // =====================================================
        // ADD PATIENT - GET
        // =====================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =====================================================
        // ADD PATIENT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            if (ModelState.IsValid)
            {
                // Automatically generate patient code
                if (string.IsNullOrWhiteSpace(patient.PatientCode))
                {
                    patient.PatientCode =
                        "PAT" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                patient.CreatedAt = DateTime.Now;

                _context.Patients.Add(patient);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Patient added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }


        // =====================================================
        // VIEW PATIENT DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }


        // =====================================================
        // EDIT PATIENT - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FindAsync(id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }


        // =====================================================
        // EDIT PATIENT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Patient updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }


        // =====================================================
        // DELETE PATIENT - GET CONFIRMATION
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }


        // =====================================================
        // DELETE PATIENT - POST
        // =====================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients
                .FindAsync(id);

            if (patient != null)
            {
                _context.Patients.Remove(patient);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Patient deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // CHECK PATIENT EXISTS
        // =====================================================

        private bool PatientExists(int id)
        {
            return _context.Patients
                .Any(e => e.Id == id);
        }
    }
}