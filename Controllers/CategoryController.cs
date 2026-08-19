using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for Find() and List()
using PlayerCards.Data;
using PlayerCards.Models;

namespace PlayerCards.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // List all categories
        public IActionResult Index()
        {
            // Note: ToList() executes the query synchronously
            var categories = _context.PlayerCategories.ToList();
            return View(categories);
        }

        // GET Create
        public IActionResult Create()
        {
            return View();
        }

        // POST Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PlayerCategory category)
        {
            if (ModelState.IsValid)
            {
                _context.PlayerCategories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // -------------------------------------------------------------------
        // DELETE ACTION (POST ONLY)
        // -------------------------------------------------------------------

        // POST: /Category/Delete/5 (Performs the actual deletion)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            // Find category to delete
            var playerCategory = _context.PlayerCategories.Find(id);

            if (playerCategory != null)
            {
                _context.PlayerCategories.Remove(playerCategory);
                _context.SaveChanges(); // Synchronous save
            }

            // Redirect back to the list view after deletion
            return RedirectToAction(nameof(Index));
        }
    }
}