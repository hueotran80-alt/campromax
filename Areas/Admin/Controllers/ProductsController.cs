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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? keyword, int? categoryId, int page = 1)
        {
            var query = _context.Products.Include(p => p.Category).Include(p => p.Brand).AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Name.Contains(keyword) || p.SKU.Contains(keyword));
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            int pageSize = 15;
            int total = await query.CountAsync();
            var products = await query.OrderByDescending(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new Product { IsActive = true, WarrantyMonths = 24 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? imageFile)
        {
            ModelState.Remove(nameof(Product.Slug));
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            model.Slug = SlugHelper.GenerateSlug(model.Name);
            if (await _context.Products.AnyAsync(p => p.Slug == model.Slug))
                model.Slug += "-" + DateTime.Now.Ticks.ToString().Substring(10);

            if (imageFile != null && imageFile.Length > 0)
                model.MainImageUrl = await SaveImageAsync(imageFile);
            else if (string.IsNullOrWhiteSpace(model.MainImageUrl))
                model.MainImageUrl = "/images/products/no-image.jpg";

            model.CreatedDate = DateTime.Now;
            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            await LoadDropdowns();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? imageFile)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove(nameof(Product.Slug));
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(model);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = model.Name;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;
            product.SKU = model.SKU;
            product.ShortDescription = model.ShortDescription;
            product.Description = model.Description;
            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.Stock = model.Stock;
            product.Resolution = model.Resolution;
            product.ConnectionType = model.ConnectionType;
            product.InstallLocation = model.InstallLocation;
            product.NightVisionRange = model.NightVisionRange;
            product.StorageType = model.StorageType;
            product.WarrantyMonths = model.WarrantyMonths;
            product.IsFeatured = model.IsFeatured;
            product.IsActive = model.IsActive;

            if (imageFile != null && imageFile.Length > 0)
                product.MainImageUrl = await SaveImageAsync(imageFile);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            bool hasOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == id);
            if (hasOrders)
            {
                product.IsActive = false;
                TempData["Success"] = "Sản phẩm đã có đơn hàng nên được ẩn thay vì xóa hẳn.";
            }
            else
            {
                _context.Products.Remove(product);
                TempData["Success"] = "Đã xóa sản phẩm.";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Brands = await _context.Brands.Where(b => b.IsActive).ToListAsync();
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            string fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/uploads/" + fileName;
        }
    }
}
