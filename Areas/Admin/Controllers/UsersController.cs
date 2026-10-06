using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? keyword)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(u => u.FullName.Contains(keyword) || (u.Email != null && u.Email.Contains(keyword)));

            var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();

            var roleDict = new Dictionary<string, IList<string>>();
            foreach (var u in users)
                roleDict[u.Id] = await _userManager.GetRolesAsync(u);

            ViewBag.Roles = roleDict;
            ViewBag.Keyword = keyword;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var orders = await _context.Orders.Where(o => o.UserId == id).OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.Orders = orders;
            ViewBag.Roles = await _userManager.GetRolesAsync(user);
            return View(user);
        }
    }
}
