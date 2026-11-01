using System.Security.Claims;

using LabSync.Models;
using LabSync.Services;
using LabSync.ViewModels;
using Labsync.Models.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabSync.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAuditLogService _auditLogService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            IAuditLogService auditLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }


        // =========================================================
        // LOGIN - GET
        // =========================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }


        // =========================================================
        // LOGIN - POST
        // =========================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            // =====================================================
            // MODEL VALIDATION
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // =====================================================
            // FIND ACTIVE USER
            // =====================================================

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == model.Email &&
                    u.Role.RoleName == model.Role &&
                    u.IsActive);


            // =====================================================
            // USER NOT FOUND
            // =====================================================

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email, password, or role."
                );

                return View(model);
            }


            // =====================================================
            // VERIFY PASSWORD
            // =====================================================

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password
                );


            // =====================================================
            // PASSWORD FAILED
            // =====================================================

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password."
                );

                return View(model);
            }


            // =====================================================
            // CREATE CLAIMS
            // =====================================================

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.Email ?? string.Empty
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.RoleName
                )
            };


            // =====================================================
            // CREATE IDENTITY
            // =====================================================

            var claimsIdentity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults
                        .AuthenticationScheme
                );


            // =====================================================
            // AUTHENTICATION PROPERTIES
            // =====================================================

            var authProperties =
                new AuthenticationProperties
                {
                    IsPersistent = false,

                    ExpiresUtc =
                        DateTimeOffset.UtcNow
                            .AddMinutes(60)
                };


            // =====================================================
            // SIGN IN
            // =====================================================

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,

                new ClaimsPrincipal(
                    claimsIdentity
                ),

                authProperties
            );


            // =====================================================
            // AUDIT LOG - LOGIN
            // =====================================================

            try
            {
                await _auditLogService.LogAsync(
                    user.Id,
                    "Login",
                    "User",
                    user.Id,
                    $"User {user.Email ?? "Unknown"} logged in successfully."
                );
            }
            catch
            {
                // -------------------------------------------------
                // Login should not fail if audit logging fails.
                // -------------------------------------------------
            }


            // =====================================================
            // ROLE BASED REDIRECT
            // =====================================================

            switch (user.Role.RoleName)
            {
                // =================================================
                // ADMIN
                // =================================================

                case "Admin":

                    return RedirectToAction(
                        "Index",
                        "Admin"
                    );


                // =================================================
                // RECEPTIONIST
                // =================================================

                case "Receptionist":

                    return RedirectToAction(
                        "Index",
                        "Patients",
                        new
                        {
                            area = "Receptionist"
                        }
                    );


                // =================================================
                // LAB TECHNICIAN
                // =================================================

                case "LabTechnician":

                    return RedirectToAction(
                        "Index",
                        "Dashboard",
                        new
                        {
                            area = "LabTechnician"
                        }
                    );


                // =================================================
                // INVALID ROLE
                // =================================================

                default:

                    await HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme
                    );

                    ModelState.AddModelError(
                        "",
                        "Invalid user role."
                    );

                    return View(model);
            }
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // =====================================================
            // GET CURRENT USER ID
            // =====================================================

            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            // =====================================================
            // GET CURRENT USER EMAIL
            // =====================================================

            var email =
                User.FindFirst(
                    ClaimTypes.Name
                )?.Value;

            // AUDIT LOG - LOGOUT

            if (int.TryParse(
                userIdClaim,
                out int userId))
            {
                try
                {
                    await _auditLogService.LogAsync(
                        userId,
                        "Logout",
                        "User",
                        userId,
                        $"User {email ?? "Unknown"} logged out."
                    );
                }
                catch
                {
                    
                }
            }


            // =====================================================
            // SIGN OUT
            // =====================================================

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );


            // =====================================================
            // REDIRECT TO LOGIN
            // =====================================================

            return RedirectToAction(
                "Login",
                "Account",
                new
                {
                    area = ""
                }
            );
        }
    }
}