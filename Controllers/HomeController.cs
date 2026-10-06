using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners.Where(b => b.IsActive).OrderBy(b => b.DisplayOrder).ToListAsync();
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            var featuredProducts = await _context.Products
                .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
                .Where(p => p.IsActive && p.IsFeatured).Take(8).ToListAsync();
            var bestSellers = await _context.Products
                .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
                .Where(p => p.IsActive).OrderByDescending(p => p.SoldCount).Take(8).ToListAsync();
            var newProducts = await _context.Products
                .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Reviews)
                .Where(p => p.IsActive).OrderByDescending(p => p.CreatedDate).Take(8).ToListAsync();
            var newsPosts = await _context.NewsPosts.Where(n => n.IsPublished).OrderByDescending(n => n.CreatedDate).Take(3).ToListAsync();
            var brands = await _context.Brands.Where(b => b.IsActive).ToListAsync();

            ViewBag.Banners = banners;
            ViewBag.Categories = categories;
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.BestSellers = bestSellers;
            ViewBag.NewProducts = newProducts;
            ViewBag.NewsPosts = newsPosts;
            ViewBag.Brands = brands;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactMessage());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.CreatedDate = DateTime.Now;
            model.IsRead = false;
            _context.ContactMessages.Add(model);
            await _context.SaveChangesAsync();

            TempData["ContactSuccess"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Policy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Search(string keyword)
        {
            return RedirectToAction("Index", "Product", new { keyword });
        }
    }
}
