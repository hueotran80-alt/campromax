using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Models.ViewModels;

namespace WebBanCameraGiamSat.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /san-pham
        public async Task<IActionResult> Index(int? categoryId, int? brandId, string? keyword,
            decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1)
        {
            var query = _context.Products.Include(p => p.Category).Include(p => p.Brand)
                .Include(p => p.Reviews).Where(p => p.IsActive).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Name.Contains(keyword) || (p.ShortDescription != null && p.ShortDescription.Contains(keyword)));

            if (minPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);

            query = sortBy switch
            {
                "price-asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
                "price-desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                "best-selling" => query.OrderByDescending(p => p.SoldCount),
                "newest" => query.OrderByDescending(p => p.CreatedDate),
                _ => query.OrderByDescending(p => p.CreatedDate)
            };

            int pageSize = 12;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;

            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new ProductListViewModel
            {
                Products = products,
                Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(),
                Brands = await _context.Brands.Where(b => b.IsActive).ToListAsync(),
                SelectedCategoryId = categoryId,
                SelectedBrandId = brandId,
                Keyword = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = Math.Max(1, totalPages),
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View("Index", vm);
        }

        // GET /danh-muc/{slug}
        public async Task<IActionResult> Category(string slug, int? brandId, string? sortBy, int page = 1)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
            if (category == null) return NotFound();

            return await Index(category.Id, brandId, null, null, null, sortBy, page);
        }

        // GET /san-pham/{slug}
        public async Task<IActionResult> Details(string slug)
        {
            var product = await _context.Products
                .Include(p => p.Category).Include(p => p.Brand)
                .Include(p => p.Images)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null) return NotFound();

            product.ViewCount += 1;
            await _context.SaveChangesAsync();

            var related = await _context.Products
                .Include(p => p.Reviews)
                .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4).ToListAsync();

            bool canReview = false;
            bool inWishlist = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                canReview = await _context.OrderDetails.AnyAsync(od =>
                    od.ProductId == product.Id &&
                    od.Order!.UserId == userId &&
                    od.Order.Status == OrderStatus.DaGiaoHang &&
                    !_context.Reviews.Any(r => r.ProductId == product.Id && r.UserId == userId && r.OrderId == od.OrderId));

                inWishlist = await _context.WishlistItems.AnyAsync(w => w.ProductId == product.Id && w.UserId == userId);
            }

            var breakdown = new Dictionary<int, int>();
            for (int i = 5; i >= 1; i--)
                breakdown[i] = product.Reviews.Count(r => r.IsApproved && r.Rating == i);

            var vm = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related,
                Reviews = product.Reviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedDate).ToList(),
                CanReview = canReview,
                AlreadyInWishlist = inWishlist,
                RatingBreakdown = breakdown
            };

            return View(vm);
        }

        // AJAX autocomplete tim kiem
        [HttpGet]
        public async Task<IActionResult> SearchSuggest(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var results = await _context.Products
                .Where(p => p.IsActive && p.Name.Contains(q))
                .Take(6)
                .Select(p => new
                {
                    name = p.Name,
                    slug = p.Slug,
                    image = p.MainImageUrl,
                    price = p.DiscountPrice ?? p.Price
                }).ToListAsync();

            return Json(results);
        }
    }
}
