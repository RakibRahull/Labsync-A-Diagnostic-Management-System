using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Areas.Receptionist.Controllers
{
    [Area("Receptionist")]
    [Authorize(Roles = "Receptionist")]
    public class ReferralDoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReferralDoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // SEARCH BY DOCTOR NAME
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            IQueryable<ReferralDoctor> query =
                _context.ReferralDoctors
                    .AsNoTracking();

            // Search by Doctor Name
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(d =>
                    d.DoctorName != null &&
                    d.DoctorName.Contains(search));
            }

            var doctors = await query
                .OrderBy(d => d.DoctorName)
                .ToListAsync();

            ViewBag.Search = search ?? "";

            return View(doctors);
        }

        // =====================================================
        // DETAILS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.ReferralDoctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }
    }
}
