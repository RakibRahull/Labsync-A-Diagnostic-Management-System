using LabSync.Models;
using Labsync.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UsersController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // =====================================================
        // LOAD ROLES
        // =====================================================
        private async Task LoadDropdownsAsync()
        {
            ViewBag.Roles = await _context.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        // =====================================================
        // USER LIST
        // =====================================================
        public async Task<IActionResult> Index(string search = "")
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(u =>
                    u.Email.Contains(search));
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserManagementViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null
                        ? u.Role.RoleName
                        : "No Role",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            ViewBag.Search = search;

            return View(users);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            return View();
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string email,
            string password,
            int roleId,
            bool isActive = true)
        {
            // Email validation
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is required.");
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "Password",
                    "Password is required.");
            }

            // Role validation
            var roleExists = await _context.Roles
                .AnyAsync(r => r.Id == roleId);

            if (!roleExists)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select a valid role.");
            }

            // Duplicate email validation
            if (!string.IsNullOrWhiteSpace(email))
            {
                var normalizedEmail = email.Trim().ToLower();

                var emailExists = await _context.Users
                    .AnyAsync(u =>
                        u.Email.ToLower() == normalizedEmail);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email already exists.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();

                return View();
            }

            var user = new User
            {
                Email = email.Trim(),
                RoleId = roleId,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            TempData["Success"] = "User created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            await LoadDropdownsAsync();

            var model = new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            UserManagementViewModel model,
            string? newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Email validation
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is required.");
            }
            else
            {
                var normalizedEmail =
                    model.Email.Trim().ToLower();

                var emailExists = await _context.Users
                    .AnyAsync(u =>
                        u.Id != model.Id &&
                        u.Email.ToLower() == normalizedEmail);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email already exists.");
                }
            }

            // Role validation
            var roleExists = await _context.Roles
                .AnyAsync(r => r.Id == model.RoleId);

            if (!roleExists)
            {
                ModelState.AddModelError(
                    "RoleId",
                    "Please select a valid role.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();

                return View(model);
            }

            // Update user information
            user.Email = model.Email.Trim();
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;

            // Update password only when entered
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    newPassword);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "User updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // TOGGLE ACTIVE / INACTIVE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive
                ? "User activated successfully."
                : "User deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                TempData["Success"] = "User deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "This user cannot be deleted because other records depend on this account. Deactivate the user instead.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}