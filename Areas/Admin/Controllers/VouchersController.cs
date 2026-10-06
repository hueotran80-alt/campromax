using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VouchersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public VouchersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var vouchers = await _context.Vouchers.OrderByDescending(v => v.Id).ToListAsync();
            return View(vouchers);
        }

        public IActionResult Create() => View(new Voucher { StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1), IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Code = model.Code.Trim().ToUpper();
            if (await _context.Vouchers.AnyAsync(v => v.Code == model.Code))
            {
                ModelState.AddModelError(nameof(Voucher.Code), "Mã voucher đã tồn tại.");
                return View(model);
            }

            _context.Vouchers.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm voucher thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            voucher.Description = model.Description;
            voucher.Type = model.Type;
            voucher.Value = model.Value;
            voucher.MinOrderAmount = model.MinOrderAmount;
            voucher.MaxDiscountAmount = model.MaxDiscountAmount;
            voucher.StartDate = model.StartDate;
            voucher.EndDate = model.EndDate;
            voucher.UsageLimit = model.UsageLimit;
            voucher.IsActive = model.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật voucher thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa voucher.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
