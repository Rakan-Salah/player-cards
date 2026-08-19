using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayerCards.Data;
using PlayerCards.Entities;
using System.Security.Claims;

[Authorize(Roles = "User")]
public class UserController : Controller
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        // 1) Try session then fallback to claim
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var cid))
            {
                // no user id, force login
                return RedirectToAction("Login", "Account");
            }
            userId = cid;
        }

        // 2) Load unread announcements for the current user (include announcement)
        var announcements = await _context.UserAnnouncements
            .Include(ua => ua.Announcement)
            .Where(ua => ua.UserAccountId == userId && !ua.IsRead)
            .OrderByDescending(ua => ua.Id)
            .ToListAsync();

        ViewBag.Announcements = announcements;

        // OPTIONAL debug: you can see how many were returned in the view
        return View();
    }
}
