using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var items = await _context.WishlistItems.Include(w => w.Product).ThenInclude(p => p!.Reviews)
                .Where(w => w.UserId == userId && w.Product != null)
                .OrderByDescending(w => w.AddedDate).ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User)!;
            var existing = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            bool added;
            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                added = false;
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId, AddedDate = DateTime.Now });
                added = true;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, added });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
