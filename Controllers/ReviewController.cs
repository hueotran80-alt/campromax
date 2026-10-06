using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var userId = _userManager.GetUserId(User)!;
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return NotFound();

            // Tim don hang da giao thanh cong co san pham nay va chua duoc danh gia (khong tin tuong du lieu tu client)
            var eligibleOrderDetail = await _context.OrderDetails
                .Where(od => od.ProductId == productId
                    && od.Order!.UserId == userId
                    && od.Order.Status == OrderStatus.DaGiaoHang
                    && !_context.Reviews.Any(r => r.ProductId == productId && r.OrderId == od.OrderId && r.UserId == userId))
                .OrderByDescending(od => od.OrderId)
                .FirstOrDefaultAsync();

            if (eligibleOrderDetail == null)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua, nhận hàng thành công và chưa đánh giá trước đó.";
                return RedirectToAction("Details", "Product", new { slug = product.Slug });
            }

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Vui lòng chọn số sao và nhập nội dung đánh giá.";
                return RedirectToAction("Details", "Product", new { slug = product.Slug });
            }

            _context.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = userId,
                OrderId = eligibleOrderDetail.OrderId,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedDate = DateTime.Now,
                IsApproved = true
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", "Product", new { slug = product.Slug });
        }
    }
}
