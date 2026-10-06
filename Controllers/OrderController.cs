using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> History(OrderStatus? status)
        {
            var userId = _userManager.GetUserId(User)!;
            var query = _context.Orders.Include(o => o.OrderDetails)
                .Where(o => o.UserId == userId).AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.SelectedStatus = status;
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();

            if (order.Status == OrderStatus.ChoXacNhan || order.Status == OrderStatus.DaXacNhan)
            {
                order.Status = OrderStatus.DaHuy;
                order.UpdatedDate = DateTime.Now;

                // Hoan lai ton kho
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock += detail.Quantity;
                        product.SoldCount = Math.Max(0, product.SoldCount - detail.Quantity);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hủy đơn hàng thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
