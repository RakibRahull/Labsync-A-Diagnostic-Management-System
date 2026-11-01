
using Labsync.Models.Data;
using LabSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // APPOINTMENT LIST
        // =====================================================

        public async Task<IActionResult> Index(DateTime? selectedDate)
        {
            // First visit: automatically select today's date.
            // If the user clears the date, selectedDate will be null.
            if (!Request.Query.ContainsKey("selectedDate"))
            {
                selectedDate = DateTime.Today;
            }

            var appointmentsQuery = _context.Appointments
                .AsQueryable();

            // Apply date filter only when a date is selected.
            // If selectedDate is null, show all appointments.
            if (selectedDate.HasValue)
            {
                var date = selectedDate.Value.Date;
                var nextDate = date.AddDays(1);

                appointmentsQuery = appointmentsQuery.Where(a =>
                    a.AppointmentDate >= date &&
                    a.AppointmentDate < nextDate
                );
            }

            var appointments = await appointmentsQuery
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // Send selected date to the View.
            // Empty string means all appointments.
            ViewBag.SelectedDate =
                selectedDate?.ToString("yyyy-MM-dd") ?? "";

            return View(appointments);
        }


        // 
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound();

            return View(appointment);
        }


        //

        public async Task<IActionResult> Create()
        {
            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            return View();
        }


        // 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                appointment.CreatedAt = DateTime.Now;

                _context.Appointments.Add(appointment);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Appointment created successfully.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            return View(appointment);
        }


        // 

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var appointment = await _context.Appointments
                .FindAsync(id);

            if (appointment == null)
                return NotFound();

            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            return View(appointment);
        }


        // 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Appointment appointment)
        {
            if (id != appointment.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);

                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Appointment updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                        return NotFound();

                    throw;
                }
            }

            ViewBag.Patients = await _context.Patients
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            return View(appointment);
        }


        // 

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound();

            return View(appointment);
        }


        // 

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments
                .FindAsync(id);

            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Appointment deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }


        // 

        private bool AppointmentExists(int id)
        {
            return _context.Appointments
                .Any(e => e.Id == id);
        }
    }
}