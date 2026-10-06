using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BannersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BannersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync();
            return View(banners);
        }

        public IActionResult Create() => View(new Banner { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Banners.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm banner thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            banner.Title = model.Title;
            banner.SubTitle = model.SubTitle;
            banner.ImageUrl = model.ImageUrl;
            banner.LinkUrl = model.LinkUrl;
            banner.DisplayOrder = model.DisplayOrder;
            banner.IsActive = model.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật banner thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
