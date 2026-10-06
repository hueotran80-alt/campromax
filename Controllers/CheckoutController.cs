using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Models.ViewModels;
using WebBanCameraGiamSat.Services;

namespace WebBanCameraGiamSat.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IVnPayService _vnPayService;

        private const decimal DefaultShippingFee = 30000;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IVnPayService vnPayService)
        {
            _context = context;
            _userManager = userManager;
            _vnPayService = vnPayService;
        }

        private async Task<CartViewModel> BuildCartAsync(string userId)
        {
            var items = await _context.CartItems.Include(c => c.Product)
                .Where(c => c.UserId == userId && c.Product != null).ToListAsync();

            return new CartViewModel
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
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var cart = await BuildCartAsync(userId);
            if (!cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            var vm = new CheckoutViewModel
            {
                Cart = cart,
                ReceiverName = user?.FullName ?? "",
                ReceiverPhone = user?.PhoneNumber ?? "",
                ShippingAddress = user?.Address ?? "",
                ShippingFee = DefaultShippingFee
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyVoucher(string code)
        {
            var userId = _userManager.GetUserId(User)!;
            var cart = await BuildCartAsync(userId);

            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code && v.IsActive);
            if (voucher == null || voucher.StartDate > DateTime.Now || voucher.EndDate < DateTime.Now)
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });

            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });

            if (cart.SubTotal < voucher.MinOrderAmount)
                return Json(new { success = false, message = $"Đơn hàng tối thiểu {voucher.MinOrderAmount:N0}đ để áp dụng mã này." });

            decimal discount = voucher.Type == DiscountType.Percent
                ? Math.Round(cart.SubTotal * voucher.Value / 100)
                : voucher.Value;

            if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                discount = voucher.MaxDiscountAmount.Value;

            if (discount > cart.SubTotal) discount = cart.SubTotal;

            decimal total = Math.Max(0, cart.SubTotal + DefaultShippingFee - discount);

            return Json(new
            {
                success = true,
                message = "Áp dụng mã giảm giá thành công!",
                discount,
                total,
                subTotal = cart.SubTotal,
                shippingFee = DefaultShippingFee
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            var cart = await BuildCartAsync(userId);

            if (!cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrWhiteSpace(model.ReceiverName) || string.IsNullOrWhiteSpace(model.ReceiverPhone) || string.IsNullOrWhiteSpace(model.ShippingAddress))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin giao hàng.";
                model.Cart = cart;
                return View("Index", model);
            }

            // Kiem tra lai ton kho truoc khi dat hang
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    TempData["Error"] = $"Sản phẩm \"{item.ProductName}\" không đủ số lượng tồn kho.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            Voucher? voucher = null;
            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(model.VoucherCode))
            {
                voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == model.VoucherCode && v.IsActive);
                if (voucher != null && cart.SubTotal >= voucher.MinOrderAmount && voucher.EndDate >= DateTime.Now
                    && (!voucher.UsageLimit.HasValue || voucher.UsedCount < voucher.UsageLimit.Value))
                {
                    discount = voucher.Type == DiscountType.Percent ? Math.Round(cart.SubTotal * voucher.Value / 100) : voucher.Value;
                    if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                        discount = voucher.MaxDiscountAmount.Value;
                    if (discount > cart.SubTotal) discount = cart.SubTotal;
                }
                else
                {
                    voucher = null;
                }
            }

            var order = new Order
            {
                OrderCode = "DH" + DateTime.Now.ToString("yyMMddHHmmss") + new Random().Next(10, 99),
                UserId = userId,
                OrderDate = DateTime.Now,
                ReceiverName = model.ReceiverName,
                ReceiverPhone = model.ReceiverPhone,
                ShippingAddress = model.ShippingAddress,
                Note = model.Note,
                SubTotal = cart.SubTotal,
                ShippingFee = DefaultShippingFee,
                DiscountAmount = discount,
                TotalAmount = Math.Max(0, cart.SubTotal + DefaultShippingFee - discount),
                Status = OrderStatus.ChoXacNhan,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = PaymentStatus.ChuaThanhToan,
                VoucherCode = voucher?.Code
            };

            foreach (var item in cart.Items)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.LineTotal
                });

                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    product.SoldCount += item.Quantity;
                }
            }

            if (voucher != null) voucher.UsedCount += 1;

            _context.Orders.Add(order);

            var cartItemsToRemove = _context.CartItems.Where(c => c.UserId == userId);
            _context.CartItems.RemoveRange(cartItemsToRemove);

            await _context.SaveChangesAsync();

            // Dieu huong theo phuong thuc thanh toan
            if (model.PaymentMethod == PaymentMethod.VNPay)
            {
                string paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, order);
                return Redirect(paymentUrl);
            }
            else if (model.PaymentMethod == PaymentMethod.MoMo)
            {
                return RedirectToAction("MoMoSimulate", "Payment", new { orderCode = order.OrderCode });
            }

            return RedirectToAction("Success", new { orderCode = order.OrderCode });
        }

        public async Task<IActionResult> Success(string orderCode)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _context.Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
