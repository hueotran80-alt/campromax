using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrdersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index(OrderStatus? status, string? keyword, int page = 1)
        {
            var query = _context.Orders.Include(o => o.User).AsQueryable();
            if (status.HasValue) query = query.Where(o => o.Status == status);
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(o => o.OrderCode.Contains(keyword) || o.ReceiverName.Contains(keyword) || o.ReceiverPhone.Contains(keyword));

            int pageSize = 15;
            int total = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.OrderDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Status = status;
            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders.Include(o => o.User).Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (status == OrderStatus.DaHuy && order.Status != OrderStatus.DaHuy)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.Stock += detail.Quantity;
                        product.SoldCount = Math.Max(0, product.SoldCount - detail.Quantity);
                    }
                }
            }

            order.Status = status;
            if (status == OrderStatus.DaGiaoHang && order.PaymentMethod == PaymentMethod.COD)
                order.PaymentStatus = PaymentStatus.DaThanhToan;

            order.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật trạng thái đơn hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
