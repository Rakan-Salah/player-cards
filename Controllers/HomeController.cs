using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlayerCards.Data;
using PlayerCards.Entities;
using PlayerCards.Models;
using System.Security.Claims;

namespace PlayerCards.Controllers
{
    [Authorize] // require login for all Home endpoints
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HomeController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Helper to get current logged-in user Id (int)
        private int GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            // Check current user role
            var currentUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Id == userId);
            bool isSuperAdmin = currentUser != null && currentUser.Role == "SuperAdmin";

            IQueryable<PlayerCard> cardsQuery;

            if (isSuperAdmin)
            {
                // SuperAdmin sees all their own cards
                cardsQuery = _context.PlayerCards
                    .Include(c => c.CategoryLink)
                    .Include(c => c.Tags)
                    .Where(c => c.UserAccountId == userId);
            }
            else
            {
                // Normal users see only cards created by SuperAdmin(s)
                cardsQuery = _context.PlayerCards
                    .Include(c => c.CategoryLink)
                    .Include(c => c.Tags)
                    .Where(c => c.UserAccount != null && c.UserAccount.Role == "SuperAdmin");
            }

            if (categoryId.HasValue)
            {
                cardsQuery = cardsQuery.Where(c => c.CategoryId == categoryId.Value);
            }

            // --- **LOGIC FOR IsLiked / IsInCart** ---
            // We must load this info for the view model
            var likedCardIds = await _context.LikedItems
                .Where(li => li.UserAccountId == userId)
                .Select(li => li.PlayerCardId)
                .ToListAsync();

            var cartCardIds = await _context.CartItems
                .Where(ci => ci.UserAccountId == userId)
                .Select(ci => ci.PlayerCardId)
                .ToListAsync();

            var cards = await cardsQuery.ToListAsync();

            // **THIS IS NEW**: We must manually set the Isliked/IsInCart properties
            // Your original model in the loop didn't have this info, so the buttons
            // wouldn't know their state on page load.
            foreach (var card in cards)
            {
                card.Isliked = likedCardIds.Contains(card.Id);
                card.IsInCart = cartCardIds.Contains(card.Id);
            }
            // --- End of new logic ---

            // Build category dropdown
            var categoriesWithCards = cards
                .Where(c => c.CategoryId != null)
                .Select(c => new
                {
                    CategoryId = c.CategoryId.Value,
                    CategoryName = c.CategoryLink != null ? c.CategoryLink.Name : "(Unnamed)"
                })
                .Distinct()
                .ToList();

            ViewBag.Categories = categoriesWithCards
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = x.CategoryId.ToString(),
                    Text = x.CategoryName,
                    Selected = categoryId.HasValue && x.CategoryId == categoryId.Value
                })
                .ToList();

            ViewBag.SelectedCategory = categoryId?.ToString();

            ViewBag.IsSuperAdmin = isSuperAdmin; // pass to view

            return View(cards);
        }

        [HttpGet]
        public IActionResult Create(int? id)
        {
            var categories = _context.PlayerCategories
                .Select(c => new { c.Id, c.Name })
                .ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Tags = _context.Tags.ToList();
            if (id.HasValue)
            {
                var card = _context.PlayerCards
                    .FirstOrDefault(c => c.Id == id.Value && c.UserAccountId == GetCurrentUserId());
                if (card != null)
                {
                    return View(card);
                }
            }
            return View(new PlayerCard());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlayerCard card, IFormFile imageFile, List<string> selectedTags)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            // --- 1. Handle Image Upload (No Change) ---
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                Directory.CreateDirectory(uploadsFolder);
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Only image files (.jpg, .jpeg, .png, .gif, .webp) are allowed.";
                    return RedirectToAction("Index");
                }
                var fileName = Guid.NewGuid().ToString() + extension;
                var path = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                card.ImagePath = "/images/" + fileName;
            }

            // --- 2. Resolve Tag Entities (Find existing or Create new) ---

            // Clean and normalize selectedTags
            selectedTags = selectedTags?.Where(t => !string.IsNullOrWhiteSpace(t))
                                        .Select(t => t.Trim())
                                        .Distinct(StringComparer.InvariantCultureIgnoreCase)
                                        .ToList() ?? new List<string>();

            var tagEntities = new List<Tag>();
            foreach (var tname in selectedTags)
            {
                // Use async FirstOrDefault and ToLower() for case-insensitive lookup
                var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tname.ToLower());

                if (existingTag != null)
                {
                    tagEntities.Add(existingTag);
                }
                else
                {
                    // Create and track the new tag
                    var newTag = new Tag { Name = tname };
                    _context.Tags.Add(newTag);
                    tagEntities.Add(newTag);
                }
            }

            // --- 3. CRITICAL FIX: Save new tags to the database NOW ---
            // This ensures that any new tags added to the context get their IDs 
            // before we try to link them to the PlayerCard (many-to-many relationship).
            // This only needs to run if there are new tags being added.
            await _context.SaveChangesAsync();

            // --- 4. Create or Update PlayerCard ---

            if (card.Id == 0)
            {
                // --- CREATE LOGIC ---
                card.UserAccountId = userId;
                // Assign the fully resolved list of tags (which now all have IDs)
                card.Tags = tagEntities;

                _context.PlayerCards.Add(card);
            }
            else
            {
                // --- EDIT LOGIC ---
                var existing = await _context.PlayerCards
                    .Include(c => c.Tags) // Must include Tags to modify the collection
                    .FirstOrDefaultAsync(c => c.Id == card.Id && c.UserAccountId == userId);

                if (existing == null) return Forbid();

                // Update basic properties
                existing.Name = card.Name;
                existing.Description = card.Description;
                existing.Price = card.Price;
                existing.CategoryId = card.CategoryId;
                if (!string.IsNullOrEmpty(card.ImagePath))
                    existing.ImagePath = card.ImagePath;

                // Update Tags (Clear existing links, establish new ones)
                existing.Tags.Clear();
                foreach (var tag in tagEntities)
                {
                    existing.Tags.Add(tag);
                }
                // Note: EF Core tracks the 'existing' entity, so it knows to update it.
            }

            // --- 5. Final Save ---
            // This saves the new PlayerCard or updates the existing one and
            // saves the changes to the many-to-many join table (PlayerCardTags).
            await _context.SaveChangesAsync();

            // Refresh ViewBag.Tags if it's needed for the Create view on error paths (though not likely here)
            ViewBag.Tags = _context.Tags.ToList();

            // Success message is often helpful here
            TempData["Success"] = card.Id == 0 ? "Player Card created successfully." : "Player Card updated successfully.";

            return RedirectToAction("Index");
        }

        // [Your existing POST Delete method]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // ... (no changes)
            var userId = GetCurrentUserId();
            var card = await _context.PlayerCards.FirstOrDefaultAsync(c => c.Id == id && c.UserAccountId == userId);
            if (card != null)
            {
                _context.PlayerCards.Remove(card);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Love(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(); // Use Unauthorized for API calls

            var existing = await _context.LikedItems
                .FirstOrDefaultAsync(li => li.UserAccountId == userId && li.PlayerCardId == id);

            bool isNowLiked;
            if (existing != null)
            {
                _context.LikedItems.Remove(existing);
                isNowLiked = false;
            }
            else
            {
                _context.LikedItems.Add(new LikedItem
                {
                    UserAccountId = userId,
                    PlayerCardId = id
                });
                isNowLiked = true;
            }

            await _context.SaveChangesAsync();

            // **CHANGED**: Return a JSON object instead of redirecting
            return Ok(new { liked = isNowLiked });
        }



        // add to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized(); // Use Unauthorized for API calls

            var exists = await _context.CartItems
                .AnyAsync(ci => ci.UserAccountId == userId && ci.PlayerCardId == id);

            if (!exists)
            {
                _context.CartItems.Add(new CartItem
                {
                    UserAccountId = userId,
                    PlayerCardId = id
                });
                await _context.SaveChangesAsync();
            }

            // **CHANGED**: Return a JSON object instead of redirecting
            return Ok(new { inCart = true });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserAccountId == userId && ci.PlayerCardId == id);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            // This action is probably called from the Cart page,
            // so redirecting back to the Cart is correct here.
            return RedirectToAction("Cart");
        }



        // show user's liked cards
        public async Task<IActionResult> Liked()
        {
            var userId = GetCurrentUserId();
            var likedCards = await _context.LikedItems
                .Where(li => li.UserAccountId == userId)
                .Include(li => li.PlayerCard)
                    .ThenInclude(pc => pc.Tags) // Eager load tags
                .Include(li => li.PlayerCard)
                    .ThenInclude(pc => pc.CategoryLink) // Eager load category
                .Select(li => li.PlayerCard)
                .ToListAsync();

            // Manually set IsLiked/IsInCart for this list
            var cartCardIds = await _context.CartItems
                .Where(ci => ci.UserAccountId == userId)
                .Select(ci => ci.PlayerCardId)
                .ToListAsync();

            foreach (var card in likedCards)
            {
                card.Isliked = true; // They are all liked, this is the liked page
                card.IsInCart = cartCardIds.Contains(card.Id);
            }


            ViewBag.IsSuperAdmin = User.IsInRole("SuperAdmin");

            // **NEW**: Re-use the Index view to show these cards
            ViewData["Title"] = "My Wishlist";
            return View("Index", likedCards);
        }



        // show user's cart
        public async Task<IActionResult> Cart()
        {
            var userId = GetCurrentUserId();
            var cartCards = await _context.CartItems
                .Where(ci => ci.UserAccountId == userId)
                .Include(ci => ci.PlayerCard)
                    .ThenInclude(pc => pc.Tags) // Eager load tags
                .Include(ci => ci.PlayerCard)
                    .ThenInclude(pc => pc.CategoryLink) // Eager load category
                .Select(ci => ci.PlayerCard)
                .ToListAsync();

            // Manually set IsLiked/IsInCart for this list
            var likedCardIds = await _context.LikedItems
                .Where(li => li.UserAccountId == userId)
                .Select(li => li.PlayerCardId)
                .ToListAsync();

            foreach (var card in cartCards)
            {
                card.Isliked = likedCardIds.Contains(card.Id);
                card.IsInCart = true; // They are all in the cart
            }

            ViewBag.IsSuperAdmin = User.IsInRole("SuperAdmin");

            // **NEW**: Re-use the Index view to show these cards
            ViewData["Title"] = "My Cart";
            return View("Index", cartCards);
        }

        /* ... (Your other methods like Privacy, MostLiked, AssignTags are fine) ... */
        public IActionResult Privacy() => View();

        // ... (AssignTags, etc)
        // [Your existing AssignTags GET/POST methods]
        [HttpGet]
        public IActionResult AssignTags()
        {
            // ... (no changes)
            var cards = _context.PlayerCards.OrderBy(c => c.Name).ToList();
            var tags = _context.Tags.OrderBy(t => t.Name).ToList();
            ViewBag.Cards = cards;
            ViewBag.Tags = tags;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTags(int[] selectedCards, List<string> selectedTags)
        {
            // --- 1. Initial Validation ---
            if (selectedCards == null || selectedCards.Length == 0 || selectedTags == null || selectedTags.Count == 0)
            {
                TempData["Error"] = "Please select at least one card and one tag.";
                return RedirectToAction("AssignTags");
            }

            // Clean and normalize tags
            selectedTags = selectedTags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            // --- 2. Resolve Tag Entities and Save New Ones ---
            var tagEntities = new List<Tag>();
            foreach (var tagName in selectedTags)
            {
                // Use async for database check
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());

                if (tag == null)
                {
                    // Create a new tag if it doesn't exist
                    tag = new Tag { Name = tagName };
                    _context.Tags.Add(tag);
                }
                tagEntities.Add(tag);
            }

            // CRITICAL FIX: Save the new tags to the database NOW. 
            // This gives them IDs, preventing Entity Framework from attempting to insert a 
            // Tag entity without a primary key when it creates the join table records.
            await _context.SaveChangesAsync();

            // --- 3. Link Tags to Selected Cards ---

            // Get all selected cards in one go, including their existing tags
            var cardsToUpdate = await _context.PlayerCards
                .Include(c => c.Tags)
                .Where(c => selectedCards.Contains(c.Id))
                .ToListAsync();

            // Apply tags to each card
            foreach (var card in cardsToUpdate)
            {
                // For assignment, we typically want to replace the current tags with the selected set.
                // We will only add new ones to avoid overwriting existing tag sets completely,
                // which gives the user more granular control.

                foreach (var tag in tagEntities)
                {
                    // Only add the tag if it's NOT ALREADY linked to the card
                    if (!card.Tags.Any(t => t.Id == tag.Id))
                    {
                        card.Tags.Add(tag);
                    }
                }
            }

            // --- 4. Final Save: Save the changes to the join table (PlayerCardTags) ---
            // This call inserts the new rows into the many-to-many join table.
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tags assigned/updated on {selectedCards.Length} card(s).";
            return RedirectToAction("AssignTags");
        }

    }
}