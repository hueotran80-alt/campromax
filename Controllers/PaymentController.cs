using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Services;

namespace WebBanCameraGiamSat.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IMoMoService _moMoService;

        public PaymentController(ApplicationDbContext context, IVnPayService vnPayService, IMoMoService moMoService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _moMoService = moMoService;
        }

        // Callback VNPay tra ve sau khi thanh toan tren cong Sandbox
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == response.OrderCode);

            if (order == null)
            {
                ViewBag.Message = "Không tìm thấy đơn hàng tương ứng.";
                ViewBag.Success = false;
                return View();
            }

            if (response.Success)
            {
                order.PaymentStatus = PaymentStatus.DaThanhToan;
                order.TransactionId = response.TransactionId;
                if (order.Status == OrderStatus.ChoXacNhan)
                    order.Status = OrderStatus.DaXacNhan;
            }
            else
            {
                order.PaymentStatus = PaymentStatus.ThatBai;
            }
            order.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            ViewBag.Success = response.Success;
            ViewBag.Message = response.Message;
            ViewBag.OrderCode = order.OrderCode;
            return View();
        }

        // Mo phong luong thanh toan MoMo (demo - chua co merchant that)
        public async Task<IActionResult> MoMoSimulate(string orderCode)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null) return NotFound();

            var result = _moMoService.CreateMoMoRequest(order.OrderCode, order.TotalAmount, $"Thanh toan don hang {order.OrderCode}");
            ViewBag.Order = order;
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> MoMoConfirm(string orderCode)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null) return NotFound();

            order.PaymentStatus = PaymentStatus.DaThanhToan;
            order.TransactionId = "MOMO" + DateTime.Now.Ticks;
            if (order.Status == OrderStatus.ChoXacNhan)
                order.Status = OrderStatus.DaXacNhan;
            order.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Success", "Checkout", new { orderCode = order.OrderCode });
        }
    }
}
