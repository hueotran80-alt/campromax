using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Data;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Services
{
    public class AiChatService : IAiChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private const decimal DefaultShippingFee = 30000;

        public AiChatService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<AiChatResponse> ProcessMessageAsync(string userId, string message)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AiChatResponse { Reply = "Vui lòng đăng nhập để tiếp tục trò chuyện với trợ lý CamPro AI." };
            }

            var cleanMsg = message.Trim().ToLowerInvariant();

            // 1. KIỂM TRA Ý ĐỊNH ĐẶT HÀNG HỘ (ORDER INTENT)
            // Ví dụ: "đặt hộ camera A32", "mua ngay camera imou ranger 2", "đặt hàng giúp tôi camera ezviz c6n", "chốt đơn camera..."
            if (cleanMsg.Contains("đặt hộ") || cleanMsg.Contains("mua hộ") || cleanMsg.Contains("chốt đơn") || 
                cleanMsg.Contains("đặt hàng giúp") || cleanMsg.Contains("đặt mua giúp") || (cleanMsg.StartsWith("mua ngay") || cleanMsg.StartsWith("đặt ngay")))
            {
                return await HandleOrderRequestAsync(user, message);
            }

            // 2. TÌM KIẾM SẢN PHẨM PHÙ HỢP TỪ DATABASE LÀM CONTEXT CHO AI
            var allProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .Take(25)
                .ToListAsync();

            // Tìm gợi ý sản phẩm liên quan đến câu hỏi
            var matchedProducts = allProducts.Where(p =>
                cleanMsg.Contains(p.Name.ToLowerInvariant()) ||
                (!string.IsNullOrEmpty(p.Brand?.Name) && cleanMsg.Contains(p.Brand.Name.ToLowerInvariant())) ||
                (!string.IsNullOrEmpty(p.Category?.Name) && cleanMsg.Contains(p.Category.Name.ToLowerInvariant())) ||
                (cleanMsg.Contains("ngoài trời") && p.InstallLocation?.ToLowerInvariant().Contains("ngoài trời") == true) ||
                (cleanMsg.Contains("trong nhà") && p.InstallLocation?.ToLowerInvariant().Contains("trong nhà") == true) ||
                (cleanMsg.Contains("wifi") && p.ConnectionType?.ToLowerInvariant().Contains("wifi") == true)
            ).Take(3).ToList();

            if (!matchedProducts.Any())
            {
                matchedProducts = allProducts.OrderBy(r => Guid.NewGuid()).Take(3).ToList();
            }

            // 3. GỌI API AI MODEL THẬT (GEMINI / OPENAI HOẶC CHẾ ĐỘ THÔNG MINH CAMPRO ENGINE)
            var aiReply = await CallRealAiApiAsync(user, message, allProducts);

            return new AiChatResponse
            {
                Reply = aiReply,
                Suggestions = matchedProducts.Select(p => new AiProductSuggestion
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Price = p.FinalPrice,
                    ImageUrl = p.MainImageUrl
                }).ToList()
            };
        }

        private async Task<string> CallRealAiApiAsync(ApplicationUser user, string userMessage, List<Product> products)
        {
            var apiKey = _configuration["AiChat:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            var provider = _configuration["AiChat:Provider"] ?? "Gemini"; // Gemini | OpenAI

            // Tóm tắt danh mục sản phẩm cho AI làm Prompt Context
            var productCatalog = string.Join("\n", products.Take(15).Select(p => 
                $"- [{p.Id}] {p.Name} | Hãng: {p.Brand?.Name} | Giá: {p.FinalPrice:N0}đ | Vị trí: {p.InstallLocation} | Độ phân giải: {p.Resolution}"));

            var systemPrompt = $@"Bạn là trợ lý AI thông minh của hệ thống bán camera giám sát 'Nhóm 8 - CamPro'.
Tên khách hàng đang trò chuyện: {user.FullName}.
Khách hàng ĐÃ ĐĂNG NHẬP.
Bạn có nhiệm vụ:
1. Tư vấn giải pháp lắp camera (trong nhà, ngoài trời, ban đêm có màu, xoay 360, độ nét 2K/4K...).
2. Báo giá chính xác dựa theo danh sách sản phẩm sau:
{productCatalog}
3. Hướng dẫn khách: Nếu muốn đặt hàng trực tiếp qua chat, khách chỉ cần gõ cú pháp ví dụ: 'Đặt hộ tôi camera [Tên]' hoặc 'Chốt đơn camera [Mã hoặc Tên]'.
Trả lời thân thiện, súc tích, định dạng gạch đầu dòng rõ ràng bằng tiếng Việt.";

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                    {
                        var reqBody = new
                        {
                            model = "gpt-4o-mini",
                            messages = new[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = userMessage }
                            },
                            temperature = 0.7
                        };

                        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        req.Content = new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");

                        var res = await _httpClient.SendAsync(req);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var content = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();
                            if (!string.IsNullOrWhiteSpace(content)) return content;
                        }
                    }
                    else
                    {
                        // Google Gemini API (gemini-1.5-flash)
                        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                        var reqBody = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    role = "user",
                                    parts = new[]
                                    {
                                        new { text = systemPrompt + "\n\nKhách hàng hỏi: " + userMessage }
                                    }
                                }
                            }
                        };

                        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                        req.Content = new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");

                        var res = await _httpClient.SendAsync(req);
                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var candidates = doc.RootElement.GetProperty("candidates");
                            if (candidates.GetArrayLength() > 0)
                            {
                                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                                if (!string.IsNullOrWhiteSpace(text)) return text;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback tự động khi mất kết nối mạng bên ngoài
                }
            }

            // FALLBACK ENGINE CHUYÊN BIỆT TÍCH HỢP SẴN: Trả lời tự động thông minh chuẩn xác theo từng câu hỏi
            return GenerateSmartCamProReply(user, userMessage, products);
        }

        private string GenerateSmartCamProReply(ApplicationUser user, string message, List<Product> products)
        {
            var msg = message.ToLowerInvariant();

            if (msg.Contains("chào") || msg.Contains("hello") || msg.Contains("hi"))
            {
                return $"Chào anh/chị {user.FullName}! Em là CamPro AI – trợ lý tư vấn camera an ninh chính hãng. Em có thể tư vấn chọn mẫu camera phù hợp nhất, báo giá hoặc hỗ trợ anh/chị đặt đơn hàng tự động ngay tại khung chat này nhé!";
            }

            if (msg.Contains("ngoài trời") || msg.Contains("mưa") || msg.Contains("chống nước"))
            {
                var p = products.FirstOrDefault(x => x.InstallLocation?.ToLowerInvariant().Contains("ngoài trời") == true) ?? products.First();
                return $"Dạ, đối với khu vực ngoài trời, anh/chị nên chọn các dòng camera đạt chuẩn chống nước IP67/IP66, có đèn rọi ban đêm có màu và còi hú báo động.\n\n" +
                       $"⭐ **Gợi ý hàng đầu:** {p.Name} (Giá ưu đãi: {p.FinalPrice:N0}đ)\n" +
                       $"• Góc nhìn rộng, tầm nhìn ban đêm sắc nét.\n" +
                       $"• Đàm thoại 2 chiều và cảnh báo chuyển động tức thì.\n\n" +
                       $"👉 Nếu ưng ý, anh/chị chỉ cần nhắn: **'Đặt hộ tôi {p.Name}'**, em sẽ tạo đơn giao tận nơi ngay ạ!";
            }

            if (msg.Contains("trong nhà") || msg.Contains("phòng khách") || msg.Contains("trẻ em") || msg.Contains("người già"))
            {
                var p = products.FirstOrDefault(x => x.InstallLocation?.ToLowerInvariant().Contains("trong nhà") == true) ?? products.First();
                return $"Dạ, lắp đặt trong nhà anh/chị nên ưu tiên các dòng camera Wifi xoay 360 độ, phát hiện tiếng khóc trẻ nhỏ và theo dõi chuyển động thông minh.\n\n" +
                       $"⭐ **Mẫu bán chạy nhất:** {p.Name} (Giá: {p.FinalPrice:N0}đ)\n" +
                       $"• Độ phân giải {p.Resolution}, góc quay quét toàn cảnh 360°.\n" +
                       $"• Đàm thoại 2 chiều to rõ như gọi điện thoại.\n\n" +
                       $"👉 Anh/chị chỉ cần gõ: **'Đặt hộ tôi {p.Name}'** để em hỗ trợ lên đơn nhanh chóng nhé!";
            }

            if (msg.Contains("giá") || msg.Contains("bao nhiêu") || msg.Contains("rẻ"))
            {
                var cheapest = products.OrderBy(p => p.FinalPrice).Take(3).ToList();
                var listText = string.Join("\n", cheapest.Select(c => $"• **{c.Name}**: {c.FinalPrice:N0}đ (Bảo hành {c.WarrantyMonths} tháng)"));
                return $"Dạ, CamPro đang có các mẫu camera giá cực kỳ ưu đãi như sau ạ:\n\n{listText}\n\n" +
                       $"Anh/chị muốn chốt mẫu nào chỉ cần nhắn: **'Đặt hộ tôi [Tên camera]'** là xong ngay ạ!";
            }

            // Phản hồi mặc định tổng quan
            var recommended = products.FirstOrDefault() ?? new Product { Name = "Camera Wifi Thông Minh", Price = 790000 };
            return $"Dạ anh/chị {user.FullName}, với nhu cầu của anh/chị, mẫu **{recommended.Name}** (Giá: {recommended.FinalPrice:N0}đ) hiện đang là lựa chọn rất tốt với chế độ bảo hành chính hãng 24 tháng.\n\n" +
                   $"Anh/chị cần tư vấn thêm tính năng gì hay muốn em **đặt hàng hộ** mẫu này luôn thì nhắn em nhé!";
        }

        private async Task<AiChatResponse> HandleOrderRequestAsync(ApplicationUser user, string message)
        {
            var msg = message.ToLowerInvariant();
            var allProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();

            // Tìm sản phẩm mà người dùng muốn đặt
            Product? targetProduct = null;

            // Kiểm tra theo ID
            var idMatch = Regex.Match(message, @"(?:id|mã|sp)\s*[:=]?\s*(\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int pid))
            {
                targetProduct = allProducts.FirstOrDefault(p => p.Id == pid);
            }

            // Nếu không có ID, tìm theo tên sản phẩm có độ trùng khớp cao nhất
            if (targetProduct == null)
            {
                targetProduct = allProducts
                    .Where(p => msg.Contains(p.Name.ToLowerInvariant()) || 
                                (p.SKU != null && msg.Contains(p.SKU.ToLowerInvariant())))
                    .OrderByDescending(p => p.Name.Length)
                    .FirstOrDefault();
            }

            // Nếu vẫn chưa tìm thấy từ cụ thể, tìm theo từ khóa nổi bật (c6n, ranger, a32, h8c, v.v...)
            if (targetProduct == null)
            {
                var keywords = new[] { "c6n", "ranger", "a32", "h8c", "c3w", "c3tn", "b2a21", "hfw1200", "imou", "ezviz", "hikvision", "dahua", "tapo" };
                foreach (var kw in keywords)
                {
                    if (msg.Contains(kw))
                    {
                        targetProduct = allProducts.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains(kw) || p.Slug.Contains(kw));
                        if (targetProduct != null) break;
                    }
                }
            }

            // Nếu khách nói "đặt hộ" mà không chỉ rõ sản phẩm nào
            if (targetProduct == null)
            {
                var top3 = allProducts.Take(3).ToList();
                return new AiChatResponse
                {
                    Reply = $"Dạ anh/chị ơi, anh/chị muốn đặt hộ sản phẩm camera nào ạ? Hãy nêu rõ tên sản phẩm, ví dụ: **'Đặt hộ tôi camera EZVIZ C6N'** hoặc **'Đặt hộ mã SP 1'** để em lên đơn giúp anh/chị nhé!",
                    Suggestions = top3.Select(p => new AiProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.FinalPrice,
                        ImageUrl = p.MainImageUrl
                    }).ToList()
                };
            }

            // Kiểm tra số lượng tồn kho
            if (targetProduct.Stock <= 0)
            {
                return new AiChatResponse
                {
                    Reply = $"Rất tiếc, sản phẩm **{targetProduct.Name}** hiện tại đang tạm hết hàng. Em xin gợi ý anh/chị các mẫu tương đương dưới đây ạ:",
                    Suggestions = allProducts.Where(p => p.Id != targetProduct.Id && p.CategoryId == targetProduct.CategoryId).Take(3).Select(p => new AiProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.FinalPrice,
                        ImageUrl = p.MainImageUrl
                    }).ToList()
                };
            }

            // Trích xuất số lượng mua (mặc định là 1)
            int quantity = 1;
            var qtyMatch = Regex.Match(message, @"(?:số lượng|sl|mua)\s*(\d+)", RegexOptions.IgnoreCase);
            if (qtyMatch.Success && int.TryParse(qtyMatch.Groups[1].Value, out int parsedQty) && parsedQty > 0)
            {
                quantity = Math.Min(parsedQty, targetProduct.Stock);
            }

            // Thông tin nhận hàng (lấy từ tài khoản người dùng hoặc mặc định)
            var receiverName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.UserName ?? "Khách hàng CamPro";
            var receiverPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : "0987654321";
            var shippingAddress = !string.IsNullOrWhiteSpace(user.Address) ? user.Address : "Số 41A Phú Diễn, Bắc Từ Liêm, Hà Nội (Địa chỉ mặc định tài khoản)";

            decimal subTotal = targetProduct.FinalPrice * quantity;
            decimal totalAmount = subTotal + DefaultShippingFee;

            // TẠO ĐƠN HÀNG THẬT TRONG CƠ SỞ DỮ LIỆU
            var orderCode = "AI" + DateTime.Now.ToString("yyMMddHHmmss") + new Random().Next(10, 99);
            var order = new Order
            {
                OrderCode = orderCode,
                UserId = user.Id,
                OrderDate = DateTime.Now,
                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ShippingAddress = shippingAddress,
                Note = $"Đơn hàng được đặt tự động bởi Trợ lý AI theo yêu cầu của khách hàng qua chat: \"{message}\"",
                SubTotal = subTotal,
                ShippingFee = DefaultShippingFee,
                DiscountAmount = 0,
                TotalAmount = totalAmount,
                Status = OrderStatus.ChoXacNhan,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.ChuaThanhToan
            };

            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = targetProduct.Id,
                ProductName = targetProduct.Name,
                ProductImageUrl = targetProduct.MainImageUrl,
                Quantity = quantity,
                UnitPrice = targetProduct.FinalPrice,
                SubTotal = subTotal
            });

            // Cập nhật tồn kho
            targetProduct.Stock -= quantity;
            targetProduct.SoldCount += quantity;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var successReply = 
                $"🎉 **ĐẶT HÀNG THÀNH CÔNG HỘ ANH/CHỊ RỒI Ạ!**\n\n" +
                $"• **Mã đơn hàng**: `{order.OrderCode}`\n" +
                $"• **Sản phẩm**: {targetProduct.Name} (x{quantity})\n" +
                $"• **Người nhận**: {order.ReceiverName} - {order.ReceiverPhone}\n" +
                $"• **Địa chỉ giao**: {order.ShippingAddress}\n" +
                $"• **Phương thức**: Thanh toán khi nhận hàng (COD)\n" +
                $"• **Tổng thanh toán**: **{totalAmount:N0} đ** (Đã gồm {DefaultShippingFee:N0}đ phí ship)\n\n" +
                $"Bộ phận chăm sóc khách hàng của Nhóm 8 - CamPro sẽ liên hệ xác nhận và giao hàng sớm nhất cho anh/chị. Anh/chị có thể vào mục **'Tra cứu đơn hàng'** hoặc [Xem chi tiết đơn hàng tại đây](/Order/Details/{order.Id}) bất kỳ lúc nào ạ!";

            return new AiChatResponse
            {
                Reply = successReply,
                OrderPlaced = true,
                OrderCode = order.OrderCode,
                TotalAmount = totalAmount,
                Suggestions = new List<AiProductSuggestion>
                {
                    new AiProductSuggestion
                    {
                        Id = targetProduct.Id,
                        Name = targetProduct.Name,
                        Slug = targetProduct.Slug,
                        Price = targetProduct.FinalPrice,
                        ImageUrl = targetProduct.MainImageUrl
                    }
                }
            };
        }
    }
}
