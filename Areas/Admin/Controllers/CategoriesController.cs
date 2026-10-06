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
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoriesController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.Products).OrderBy(c => c.DisplayOrder).ToListAsync();
            return View(categories);
        }

        public IActionResult Create() => View(new Category { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            ModelState.Remove(nameof(Category.Slug));
            if (!ModelState.IsValid) return View(model);

            model.Slug = SlugHelper.GenerateSlug(model.Name);
            if (await _context.Categories.AnyAsync(c => c.Slug == model.Slug))
                model.Slug += "-" + DateTime.Now.Ticks.ToString().Substring(10);

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove(nameof(Category.Slug));
            if (!ModelState.IsValid) return View(model);

            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            cat.Name = model.Name;
            cat.Description = model.Description;
            cat.ImageUrl = model.ImageUrl;
            cat.IconClass = model.IconClass;
            cat.DisplayOrder = model.DisplayOrder;
            cat.IsActive = model.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm. Hãy ẩn (Không hoạt động) thay vì xóa.";
            }
            else
            {
                _context.Categories.Remove(cat);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa danh mục.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
