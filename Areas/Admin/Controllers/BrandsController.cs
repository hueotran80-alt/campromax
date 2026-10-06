using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Helpers;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BrandsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands.Include(b => b.Products).ToListAsync();
            return View(brands);
        }

        public IActionResult Create() => View(new Brand { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand model)
        {
            ModelState.Remove(nameof(Brand.Slug));
            if (!ModelState.IsValid) return View(model);

            model.Slug = SlugHelper.GenerateSlug(model.Name);
            if (await _context.Brands.AnyAsync(b => b.Slug == model.Slug))
                model.Slug += "-" + DateTime.Now.Ticks.ToString().Substring(10);

            _context.Brands.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm thương hiệu thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove(nameof(Brand.Slug));
            if (!ModelState.IsValid) return View(model);

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            brand.Name = model.Name;
            brand.Description = model.Description;
            brand.LogoUrl = model.LogoUrl;
            brand.IsActive = model.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thương hiệu thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            bool hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                TempData["Error"] = "Không thể xóa thương hiệu đang có sản phẩm.";
            }
            else
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa thương hiệu.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
