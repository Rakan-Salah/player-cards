using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlayerCards.Data;
using PlayerCards.Entities;
using PlayerCards.Models;
using PlayerCards.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PlayerCards.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender; // custom service for sending emails

        public AccountController(AppDbContext appDbContext, IConfiguration config, IEmailSender emailSender)
        {
            _context = appDbContext;
            _config = config;
            _emailSender = emailSender;
        }

        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registration(Registration model)
        {
            if (ModelState.IsValid)
            {
                UserAccount account = new UserAccount
                {
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Password = model.Password, // WARNING: plain text password (acceptable only for now)
                    UserName = model.UserName,
                    Role = "User",
                    IsActive = true,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber
                };

                try
                {
                    _context.UserAccounts.Add(account);
                    _context.SaveChanges();
                    ModelState.Clear();
                    ViewBag.Message = $"{account.FirstName} {account.LastName} registered successfully. Please Login.";
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Please enter a unique Email or Username.");
                    return View(model);
                }

                return View("Login"); // Optionally: return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login model)
        {
            // Try to match a real account first (e.g. seeded SuperAdmin: superadmin / superpass)
            var user = _context.UserAccounts
                .FirstOrDefault(x =>
                    (x.UserName == model.UserNameOrEmail || x.Email == model.UserNameOrEmail) &&
                    x.Password == model.Password);

            // DEMO MODE: if no match, sign in as a guest so recruiters can always get in
            string name = user?.FirstName ?? "Guest";
            string email = user?.Email ?? "guest@demo.com";
            string role = user?.Role ?? "User";
            int userId = user?.Id ?? 0;

            HttpContext.Session.SetInt32("UserId", userId);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, email),
                new Claim("Name", name),
                new Claim(ClaimTypes.Role, role),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Redirect based on role; guests and users land on Home
            return role switch
            {
                "SuperAdmin" => RedirectToAction("Dashboard", "SuperAdmin"),
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                _ => RedirectToAction("Index", "Home"),
            };
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == model.Email);

            // Don't reveal whether user exists; show same message either way
            if (user == null)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Generate secure random token (URL-safe)
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = WebEncoders.Base64UrlEncode(tokenBytes);

            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var resetUrl = Url.Action("ResetPassword", "Account",
                new { token = token, email = user.Email }, Request.Scheme);

            var html = $@"
            <p>Hi {user.FirstName},</p>
            <p>We received a request to reset your password. Click the link below to reset it (expires in 1 hour):</p>
            <p><a href='{resetUrl}'>Reset Password</a></p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            ";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, "Password Reset - PlayerCards", html);
            }
            catch (Exception)
            {
                // log error (do NOT include token)
                TempData["Error"] = "Unable to send email. Please try again later.";
                return RedirectToAction("ForgotPassword");
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // GET Reset
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            var vm = new ResetPasswordViewModel { Token = token, Email = email };
            return View(vm);
        }

        // POST Reset
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);

            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Invalid or expired token.");
                return View(model);
            }

            user.Password = model.NewPassword;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully. You can now log in";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
    }
}
