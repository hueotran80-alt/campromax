using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Models.ViewModels;

namespace WebBanCameraGiamSat.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<CartViewModel> BuildCartAsync(string userId)
        {
            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && c.Product != null)
                .OrderByDescending(c => c.AddedDate)
                .ToListAsync();

            var vm = new CartViewModel
            {
                Items = items.Select(c => new CartItemViewModel
                {
                    CartItemId = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product!.Name,
                    Slug = c.Product.Slug,
                    ImageUrl = c.Product.MainImageUrl,
                    UnitPrice = c.Product.FinalPrice,
                    Quantity = c.Quantity,
                    Stock = c.Product.Stock
                }).ToList()
            };
            return vm;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var vm = await BuildCartAsync(userId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
            }
            var userId = _userManager.GetUserId(User)!;
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            if (quantity < 1) quantity = 1;

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (cartItem == null)
            {
                cartItem = new CartItem { UserId = userId, ProductId = productId, Quantity = quantity, AddedDate = DateTime.Now };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            if (cartItem.Quantity > product.Stock)
                cartItem.Quantity = product.Stock;

            await _context.SaveChangesAsync();

            int cartCount = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng.", cartCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _context.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item == null) return Json(new { success = false });

            if (quantity < 1) quantity = 1;
            if (item.Product != null && quantity > item.Product.Stock) quantity = item.Product.Stock;

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            var cart = await BuildCartAsync(userId);
            return Json(new { success = true, lineTotal = item.Quantity * (item.Product?.FinalPrice ?? 0), subTotal = cart.SubTotal, totalQuantity = cart.TotalQuantity });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            var cart = await BuildCartAsync(userId);
            return Json(new { success = true, subTotal = cart.SubTotal, totalQuantity = cart.TotalQuantity, isEmpty = !cart.Items.Any() });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartCount()
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new { count = 0 });
            var userId = _userManager.GetUserId(User)!;
            int count = await _context.CartItems.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
            return Json(new { count });
        }
    }
}
