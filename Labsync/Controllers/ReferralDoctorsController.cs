using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    public class ReferralDoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReferralDoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReferralDoctors
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.ReferralDoctors
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(doctors);
        }

        // GET: ReferralDoctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.ReferralDoctors
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        // GET: ReferralDoctors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ReferralDoctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReferralDoctor doctor)
        {
            if (ModelState.IsValid)
            {
                doctor.CreatedAt = DateTime.Now;

                _context.ReferralDoctors.Add(doctor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(doctor);
        }

        // GET: ReferralDoctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.ReferralDoctors
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        // POST: ReferralDoctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReferralDoctor doctor)
        {
            if (id != doctor.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var existingDoctor = await _context.ReferralDoctors
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (existingDoctor == null)
                    return NotFound();

                existingDoctor.DoctorName = doctor.DoctorName;
                existingDoctor.Specialization = doctor.Specialization;
                existingDoctor.HospitalName = doctor.HospitalName;
                existingDoctor.Phone = doctor.Phone;
                existingDoctor.Email = doctor.Email;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(doctor);
        }

        // GET: ReferralDoctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await _context.ReferralDoctors
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        // POST: ReferralDoctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.ReferralDoctors
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor != null)
            {
                _context.ReferralDoctors.Remove(doctor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
