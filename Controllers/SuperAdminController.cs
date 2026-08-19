using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayerCards.Data;
using PlayerCards.Entities;
using PlayerCards.Models;

namespace PlayerCards.Controllers
{
    // [Authorize] removed for portfolio demo: the dashboard is publicly viewable.
    public class SuperAdminController : Controller
    {
        private readonly AppDbContext _context;

        public SuperAdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string searchString, int page = 1, int pageSize = 8)
        {
            var usersQuery = _context.UserAccounts.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                usersQuery = usersQuery.Where(u =>
                    (!string.IsNullOrEmpty(u.UserName) && u.UserName.ToLower().Contains(searchString)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(searchString))
                );
            }

            // Count for pagination
            var totalUsers = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            // Get paged data
            var users = await usersQuery
                .OrderBy(u => u.UserName) // optional: sort alphabetically
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pass pagination info to view
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentFilter"] = searchString;
            ViewBag.Users = users;

            return View(users);
        }

        // GET: SuperAdmin/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new UserAccount()); //This avoids the null error
        }

        // POST: SuperAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAccount user, IFormFile avatarFile)
        {
            user.Role = "User";

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                //  Get sanitized filename if middleware modified it
                var sanitizedName = HttpContext.Items["SanitizedFileName_" + avatarFile.Name] as string ?? avatarFile.FileName;

                // Use a GUID to prevent collisions, keep extension safe
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(sanitizedName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                user.AvatarPath = "/avatars/" + uniqueFileName;
            }

            _context.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        public IActionResult UserList()
        {
            var users = _context.UserAccounts.ToList();
            return View(users); // View that displays all users with roles
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = "Admin";
            _context.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        public IActionResult PromoteToSuperAdmin(int id)
        {
            var user = _context.UserAccounts.Find(id);
            if (user != null && user.Role != "SuperAdmin")
            {
                user.Role = "SuperAdmin";
                _context.SaveChanges();
            }
            return RedirectToAction("UserList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            _context.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = true;
            _context.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserAccount editedUser)
        {
            var user = await _context.UserAccounts.FindAsync(editedUser.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserName = editedUser.UserName;
            user.Email = editedUser.Email;
            user.Role = editedUser.Role;
            user.IsActive = editedUser.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        public IActionResult Cards(string searchString, string offerFilter)
        {
            var cards = _context.PlayerCards
                .Include(c => c.UserAccount)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                cards = cards.Where(c =>
                    c.Name.Contains(searchString) ||
                    (c.UserAccount != null && c.UserAccount.UserName.Contains(searchString)));
            }

            if (offerFilter == "WithOffer")
            {
                cards = cards.Where(c => !string.IsNullOrEmpty(c.Offer));
            }
            else if (offerFilter == "WithoutOffer")
            {
                cards = cards.Where(c => string.IsNullOrEmpty(c.Offer));
            }

            ViewBag.SearchString = searchString;
            ViewBag.OfferFilter = offerFilter;
            return View(cards.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCard(PlayerCard card)
        {
            var existing = _context.PlayerCards.Find(card.Id);
            if (existing != null)
            {
                existing.Name = card.Name;
                existing.Description = card.Description;
                existing.Offer = card.Offer;

                //  Apply the discount logic
                if (!string.IsNullOrEmpty(card.Offer) && decimal.TryParse(card.Offer, out var offerValue))
                {
                    if (offerValue > 0)
                    {
                        existing.Price = (existing.Price ?? 0) - offerValue;
                        if (existing.Price < 0) existing.Price = 0;
                    }
                }

                _context.SaveChanges();
            }
            return RedirectToAction("Cards");
        }

        [HttpPost]
        public async Task<IActionResult> ImportUsers(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid Excel file";
                return RedirectToAction("Dashboard");
            }

            // Get sanitized name from middleware if available
            var sanitizedName = HttpContext.Items["SanitizedFileName_" + file.Name] as string ?? file.FileName;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            foreach (var row in worksheet.RowsUsed().Skip(1)) // skip header row
            {
                try
                {
                    var user = new UserAccount
                    {
                        FirstName = row.Cell(1).GetValue<string>(),
                        LastName = row.Cell(2).GetValue<string>(),
                        Email = row.Cell(3).GetValue<string>(),
                        UserName = row.Cell(4).GetValue<string>(),
                        Password = row.Cell(5).GetValue<string>(), //  hash if needed
                        Address = row.Cell(6).GetValue<string>(),
                        PhoneNumber = row.Cell(7).GetValue<string>(),
                        Role = "User",
                        IsActive = true
                    };

                    // Check duplicate email/username before adding
                    if (!_context.UserAccounts.Any(u => u.Email == user.Email || u.UserName == user.UserName))
                    {
                        _context.UserAccounts.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error processing row: {ex.Message}";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Users from {sanitizedName} imported successfully!";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(string Title, string Content, List<int> UserIds)
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Content) || UserIds == null || UserIds.Count == 0)
            {
                TempData["Error"] = "Please fill in all fields.";
                return RedirectToAction("Dashboard");
            }

            var announcement = new Announcement
            {
                Title = Title,
                Content = Content
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            foreach (var userId in UserIds)
            {
                var userAnnouncement = new UserAnnouncement
                {
                    UserAccountId = userId,
                    AnnouncementId = announcement.Id
                };
                _context.UserAnnouncements.Add(userAnnouncement);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Announcement sent successfully!";
            return RedirectToAction("Dashboard");
        }
    }
}
