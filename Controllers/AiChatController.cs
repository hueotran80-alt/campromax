using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanCameraGiamSat.Models;
using WebBanCameraGiamSat.Services;

namespace WebBanCameraGiamSat.Controllers
{
    public class AiChatController : Controller
    {
        private readonly IAiChatService _aiChatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AiChatController(IAiChatService aiChatService, UserManager<ApplicationUser> userManager)
        {
            _aiChatService = aiChatService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestModel model)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    message = "Bạn cần đăng nhập tài khoản để sử dụng tính năng Chatbox AI và Đặt hàng tự động!"
                });
            }

            if (model == null || string.IsNullOrWhiteSpace(model.Message))
            {
                return Json(new { success = false, message = "Vui lòng nhập câu hỏi hoặc yêu cầu tư vấn." });
            }

            var userId = _userManager.GetUserId(User)!;
            var result = await _aiChatService.ProcessMessageAsync(userId, model.Message);

            return Json(new
            {
                success = true,
                reply = result.Reply,
                orderPlaced = result.OrderPlaced,
                orderCode = result.OrderCode,
                totalAmount = result.TotalAmount,
                addedToCart = result.AddedToCart,
                cartCount = result.CartCount,
                suggestions = result.Suggestions
            });
        }
    }

    public class ChatRequestModel
    {
        public string Message { get; set; } = string.Empty;
    }
}
