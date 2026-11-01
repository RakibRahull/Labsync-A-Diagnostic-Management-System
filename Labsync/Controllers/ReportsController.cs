using Labsync.Models.Data;
using LabSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{

    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }



        // Retrive Reports

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Reports
                .Include(r => r.Appointment)
                .Include(r => r.GeneratedByUser)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.ApprovalStatus)
                .AsQueryable();


            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.Appointment.AppointmentCode!
                        .Contains(search) ||

                    r.GeneratedByUser.Email
                        .Contains(search));
            }


            var reports = await query
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();


            ViewBag.Search = search;

            return View(reports);
        }


        // DETAILS

        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Appointment)
                .Include(r => r.GeneratedByUser)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.ApprovalStatus)
                .FirstOrDefaultAsync(r => r.Id == id);


            if (report == null)
            {
                return NotFound();
            }


            return View(report);
        }


       
        // APPROVE

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id);


            if (report == null)
            {
                TempData["Error"] = "Report not found.";
                return RedirectToAction(nameof(Index));
            }


            var approvedStatus = await _context.ReportApprovalStatuses
                .FirstOrDefaultAsync(s =>
                    s.StatusName.ToLower() == "approved");


            if (approvedStatus == null)
            {
                TempData["Error"] =
                    "Approved status was not found in database.";

                return RedirectToAction(nameof(Index));
            }


            report.ApprovalStatusId = approvedStatus.Id;

            report.ApprovedBy = GetCurrentUserId();

            report.ApprovalDate = DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Report approved successfully.";


            return RedirectToAction(nameof(Index));
        }


       
        // REJECT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id);


            if (report == null)
            {
                TempData["Error"] = "Report not found.";
                return RedirectToAction(nameof(Index));
            }


            var rejectedStatus = await _context.ReportApprovalStatuses
                .FirstOrDefaultAsync(s =>
                    s.StatusName.ToLower() == "rejected");


            if (rejectedStatus == null)
            {
                TempData["Error"] =
                    "Rejected status was not found in database.";

                return RedirectToAction(nameof(Index));
            }


            report.ApprovalStatusId = rejectedStatus.Id;

            report.ApprovedBy = GetCurrentUserId();

            report.ApprovalDate = DateTime.Now;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Report rejected successfully.";


            return RedirectToAction(nameof(Index));
        }


        // =========================================
        // CURRENT USER
        // =========================================

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirst("UserId")?.Value;

            if (int.TryParse(userId, out int id))
            {
                return id;
            }

            return null;
        }
    }
}