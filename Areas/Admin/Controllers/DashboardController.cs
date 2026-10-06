using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Models.ViewModels;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var paidOrders = _context.Orders.Where(o => o.Status != OrderStatus.DaHuy);

            var todayRevenueList = await paidOrders.Where(o => o.OrderDate >= today && o.OrderDate < today.AddDays(1)).Select(o => o.TotalAmount).ToListAsync();
            var monthRevenueList = await paidOrders.Where(o => o.OrderDate >= monthStart).Select(o => o.TotalAmount).ToListAsync();
            var yearRevenueList = await paidOrders.Where(o => o.OrderDate >= yearStart).Select(o => o.TotalAmount).ToListAsync();

            var vm = new DashboardViewModel
            {
                RevenueToday = todayRevenueList.Sum(),
                RevenueThisMonth = monthRevenueList.Sum(),
                RevenueThisYear = yearRevenueList.Sum(),
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.ChoXacNhan),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalCustomers = (await _userManager.GetUsersInRoleAsync("KhachHang")).Count,
                LowStockCount = await _context.Products.CountAsync(p => p.IsActive && p.Stock <= 5)
            };

            // Bieu do doanh thu 7 ngay gan nhat
            var sevenDaysAgo = today.AddDays(-6);
            var recentOrders = await paidOrders
                .Where(o => o.OrderDate >= sevenDaysAgo)
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var revenue = recentOrders.Where(o => o.OrderDate.Date == date).Sum(o => o.TotalAmount);
                vm.RevenueChartLabels.Add(date.ToString("dd/MM"));
                vm.RevenueChartData.Add(revenue);
            }

            vm.TopSellingProducts = await _context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.SoldCount)
                .Take(5)
                .Select(p => new TopProductVM
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    ImageUrl = p.MainImageUrl,
                    SoldCount = p.SoldCount,
                    Revenue = p.SoldCount * (p.DiscountPrice ?? p.Price)
                }).ToListAsync();

            vm.RecentOrders = await _context.Orders.OrderByDescending(o => o.OrderDate).Take(8).ToListAsync();
            vm.LowStockProducts = await _context.Products.Where(p => p.IsActive && p.Stock <= 5).OrderBy(p => p.Stock).Take(6).ToListAsync();

            var statusGroups = await _context.Orders.GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

            vm.OrderStatusCounts = statusGroups.Select(g => new OrderStatusCountVM
            {
                StatusName = OrderStatusText(g.Status),
                Count = g.Count
            }).ToList();

            return View(vm);
        }

        public static string OrderStatusText(OrderStatus status) => status switch
        {
            OrderStatus.ChoXacNhan => "Chờ xác nhận",
            OrderStatus.DaXacNhan => "Đã xác nhận",
            OrderStatus.DangDongGoi => "Đang đóng gói",
            OrderStatus.DangVanChuyen => "Đang vận chuyển",
            OrderStatus.DaGiaoHang => "Đã giao hàng",
            OrderStatus.DaHuy => "Đã hủy",
            _ => status.ToString()
        };
    }
}
