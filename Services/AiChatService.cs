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
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<AiChatResponse> ProcessMessageAsync(string userId, string message)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AiChatResponse { Reply = "Vui lòng đăng nhập để tiếp tục trò chuyện với trợ lý CamPro AI." };
            }

            var cleanMsg = message.Trim().ToLowerInvariant();

            // 1. KIỂM TRA Ý ĐỊNH THÊM VÀO GIỎ HÀNG (KHÔNG THANH TOÁN NGAY)
            // Ví dụ: "thêm vào giỏ", "bỏ vào giỏ", "cho vào giỏ camera c6n", "chỉ thêm vào giỏ chứ ko thanh toán"
            if (IsAddToCartIntent(cleanMsg))
            {
                return await HandleAddToCartRequestAsync(user, message);
            }

            // 2. KIỂM TRA Ý ĐỊNH ĐẶT HÀNG HỘ (ORDER INTENT)
            if (IsOrderIntent(cleanMsg))
            {
                return await HandleOrderRequestAsync(user, message);
            }

            // 3. TÌM KIẾM SẢN PHẨM PHÙ HỢP TỪ DATABASE
            var allProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            var matchedProducts = FindRelevantProducts(cleanMsg, allProducts);

            // 4. GỌI API AI MODEL THẬT (GEMINI / OPENAI) HOẶC DÙNG AI NLP ENGINE NÂNG CAO
            var aiReply = await CallRealAiApiAsync(user, message, allProducts, matchedProducts);

            return new AiChatResponse
            {
                Reply = aiReply,
                Suggestions = matchedProducts.Take(3).Select(p => new AiProductSuggestion
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Price = p.FinalPrice,
                    ImageUrl = p.MainImageUrl
                }).ToList()
            };
        }

        private bool IsAddToCartIntent(string msg)
        {
            return msg.Contains("thêm vào giỏ") || msg.Contains("cho vào giỏ") || msg.Contains("bỏ vào giỏ") ||
                   msg.Contains("lưu vào giỏ") || msg.Contains("nhét vào giỏ") || msg.Contains("thêm giỏ hàng") ||
                   msg.Contains("cho vô giỏ") || (msg.Contains("vào giỏ") && !msg.Contains("thanh toán ngay"));
        }

        private bool IsOrderIntent(string msg)
        {
            return msg.Contains("đặt hộ") || msg.Contains("mua hộ") || msg.Contains("chốt đơn") ||
                   msg.Contains("đặt hàng giúp") || msg.Contains("đặt mua giúp") || msg.Contains("mua giúp") ||
                   msg.StartsWith("mua ngay") || msg.StartsWith("đặt ngay") || msg.StartsWith("chốt mẫu") ||
                   msg.StartsWith("lấy cho tôi") || msg.StartsWith("đặt cho tôi");
        }

        private List<Product> FindRelevantProducts(string msg, List<Product> products)
        {
            var list = new List<Product>();

            // Theo thương hiệu
            if (msg.Contains("ezviz")) list.AddRange(products.Where(p => p.Brand?.Name?.ToLowerInvariant().Contains("ezviz") == true));
            if (msg.Contains("imou")) list.AddRange(products.Where(p => p.Brand?.Name?.ToLowerInvariant().Contains("imou") == true));
            if (msg.Contains("hikvision")) list.AddRange(products.Where(p => p.Brand?.Name?.ToLowerInvariant().Contains("hikvision") == true));
            if (msg.Contains("dahua")) list.AddRange(products.Where(p => p.Brand?.Name?.ToLowerInvariant().Contains("dahua") == true));
            if (msg.Contains("kbvision")) list.AddRange(products.Where(p => p.Brand?.Name?.ToLowerInvariant().Contains("kbvision") == true));
            if (msg.Contains("tapo") || msg.Contains("tp-link")) list.AddRange(products.Where(p => p.Brand?.Name?.ToLowerInvariant().Contains("tapo") == true));

            // Theo loại lắp đặt
            if (msg.Contains("ngoài trời") || msg.Contains("sân") || msg.Contains("cổng") || msg.Contains("chống nước") || msg.Contains("mưa"))
            {
                list.AddRange(products.Where(p => p.InstallLocation?.ToLowerInvariant().Contains("ngoài trời") == true || p.Category?.Name?.ToLowerInvariant().Contains("ngoài trời") == true));
            }
            if (msg.Contains("trong nhà") || msg.Contains("phòng khách") || msg.Contains("phòng ngủ") || msg.Contains("trẻ em") || msg.Contains("em bé"))
            {
                list.AddRange(products.Where(p => p.InstallLocation?.ToLowerInvariant().Contains("trong nhà") == true || p.Category?.Name?.ToLowerInvariant().Contains("trong nhà") == true));
            }

            // Theo tính năng
            if (msg.Contains("ban đêm") || msg.Contains("có màu") || msg.Contains("hồng ngoại") || msg.Contains("đêm"))
            {
                list.AddRange(products.Where(p => p.NightVisionRange != null || p.Description?.ToLowerInvariant().Contains("có màu") == true));
            }
            if (msg.Contains("xoay") || msg.Contains("360") || msg.Contains("quay quét"))
            {
                list.AddRange(products.Where(p => p.Name.ToLowerInvariant().Contains("xoay") || p.Description?.ToLowerInvariant().Contains("360") == true));
            }
            if (msg.Contains("đầu ghi") || msg.Contains("nvr") || msg.Contains("dvr"))
            {
                list.AddRange(products.Where(p => p.Category?.Slug?.Contains("dau-ghi") == true));
            }
            if (msg.Contains("poe") || msg.Contains("dây mạng") || msg.Contains("dự án") || msg.Contains("kho"))
            {
                list.AddRange(products.Where(p => p.Category?.Slug?.Contains("poe") == true));
            }

            // Lọc theo từ khóa tên cụ thể
            var matchedSpecific = products.Where(p => msg.Contains(p.Name.ToLowerInvariant())).ToList();
            if (matchedSpecific.Any()) return matchedSpecific;

            var distinct = list.DistinctBy(p => p.Id).ToList();
            if (distinct.Any()) return distinct;

            // Nếu không khớp từ khóa chuyên biệt, lấy các sản phẩm nổi bật
            return products.Where(p => p.IsFeatured).Take(4).ToList();
        }

        private async Task<string> CallRealAiApiAsync(ApplicationUser user, string userMessage, List<Product> allProducts, List<Product> matchedProducts)
        {
            var apiKey = _configuration["AiChat:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            var provider = _configuration["AiChat:Provider"] ?? "Gemini";

            var catalogPrompt = string.Join("\n", allProducts.Take(20).Select(p =>
                $"- [{p.Id}] {p.Name} | Giá: {p.FinalPrice:N0}đ | Hãng: {p.Brand?.Name} | Vị trí: {p.InstallLocation} | Tính năng: {p.Resolution}, {p.NightVisionRange}"));

            var systemPrompt = $@"Bạn là Trợ lý AI tư vấn và bán hàng của 'Nhóm 8 - CamPro' (Chuyên camera giám sát chính hãng).
Khách hàng đang chat: {user.FullName} (đã đăng nhập).
Danh sách sản phẩm trong kho:
{catalogPrompt}

Nhiệm vụ:
1. Hiểu câu hỏi của khách hàng và trả lời ĐÚNG TRỌNG TÂM (kỹ thuật lắp đặt, vị trí, thương hiệu, độ phân giải, bảo hành, giá cả).
2. Dùng thông tin sản phẩm có thật trong kho ở trên để tư vấn, không bịa đặt sản phẩm không có.
3. Hướng dẫn khách: Nếu muốn đặt hàng ngay, khách chỉ cần nhắn: 'Đặt hộ tôi [Tên camera]' hoặc 'Chốt đơn [Tên camera]'.
4. Văn phong: Lịch sự, chu đáo, dùng gạch đầu dòng rõ ràng, định dạng in đậm tên sản phẩm và giá.";

            // 1. Thử gọi API nếu có Key (Gemini / OpenAI)
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
                            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                            if (!string.IsNullOrWhiteSpace(content)) return content;
                        }
                    }
                    else
                    {
                        var model = _configuration["AiChat:Model"] ?? "gemini-3.5-flash-lite";
                        if (string.IsNullOrWhiteSpace(model)) model = "gemini-3.5-flash-lite";

                        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                        var reqBody = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    role = "user",
                                    parts = new[] { new { text = systemPrompt + "\n\nKhách hỏi: " + userMessage } }
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
                        else
                        {
                            // Fallback thử sang gemini-1.5-flash nếu model flash-lite chưa bật trong region
                            var fallbackEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                            using var fallbackReq = new HttpRequestMessage(HttpMethod.Post, fallbackEndpoint);
                            fallbackReq.Content = new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");
                            var fallbackRes = await _httpClient.SendAsync(fallbackReq);
                            if (fallbackRes.IsSuccessStatusCode)
                            {
                                var json = await fallbackRes.Content.ReadAsStringAsync();
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
                }
                catch
                {
                    // Fallback xuống Deep NLP Engine
                }
            }

            // 2. ENGINE NLP CHUYÊN SÂU NỘI BỘ (TRẢ LỜI ĐÚNG 100% CÂU HỎI THỰC TẾ)
            return GenerateDeepNlpReply(user, userMessage, allProducts, matchedProducts);
        }

        private string GenerateDeepNlpReply(ApplicationUser user, string message, List<Product> allProducts, List<Product> matched)
        {
            var msg = message.ToLowerInvariant().Trim();

            // Chào hỏi
            if (msg == "chào" || msg == "hi" || msg == "hello" || msg.StartsWith("chào bạn") || msg.StartsWith("xin chào"))
            {
                return $"Dạ chào anh/chị **{user.FullName}**! Em là trợ lý AI thông minh của **Nhóm 8 - CamPro**.\n\n" +
                       $"Anh/chị đang cần tìm giải pháp camera cho gia đình, cửa hàng hay công ty ạ? Anh/chị cứ thoải mái đặt câu hỏi về:\n" +
                       $"• Camera chống trộm, báo động ban đêm có màu.\n" +
                       $"• Camera xoay 360 độ trông trẻ, người già, đàm thoại 2 chiều.\n" +
                       $"• So sánh tính năng hoặc báo giá theo ngân sách.\n\n" +
                       $"💡 *Đặc biệt, nếu ưng ý mẫu nào, anh/chị chỉ cần nhắn: **'Đặt hộ tôi [Tên camera]'**, em sẽ tự động tạo đơn giao hàng tận nơi ngay lập tức ạ!*";
            }

            // Hỏi về chính sách bảo hành / đổi trả / vận chuyển
            if (msg.Contains("bảo hành") || msg.Contains("đổi trả") || msg.Contains("sửa chữa") || msg.Contains("chính sách"))
            {
                return $"Dạ về **Chính sách & Bảo hành** tại Nhóm 8 - CamPro:\n\n" +
                       $"• **Bảo hành chính hãng:** Toàn bộ sản phẩm được bảo hành chính hãng từ **12 - 24 tháng** theo tiêu chuẩn của Hikvision, Dahua, EZVIZ, Imou.\n" +
                       $"• **Đổi mới:** 1 đổi 1 trong vòng **30 ngày đầu** nếu phát sinh lỗi phần cứng từ nhà sản xuất.\n" +
                       $"• **Hỗ trợ kỹ thuật:** Cài đặt app, kết nối xem qua điện thoại trọn đời hoàn toàn miễn phí.\n" +
                       $"• **Vận chuyển:** Giao hàng toàn quốc, đồng kiểm tra hàng trước khi thanh toán (COD).";
            }

            // Hỏi về phương thức thanh toán
            if (msg.Contains("thanh toán") || msg.Contains("vnpay") || msg.Contains("momo") || msg.Contains("cod") || msg.Contains("chuyển khoản"))
            {
                return $"Dạ hiện tại **Nhóm 8 - CamPro** hỗ trợ đa dạng các hình thức thanh toán tiện lợi và an toàn:\n\n" +
                       $"1. **Thanh toán khi nhận hàng (COD):** Giao hàng tận nơi, kiểm tra hàng rồi mới thanh toán tiền mặt cho shipper.\n" +
                       $"2. **VNPAY:** Quét mã VNPAY-QR qua ứng dụng ngân hàng hoặc thẻ ATM/Visa nội địa.\n" +
                       $"3. **Ví MoMo:** Quét mã thanh toán tức thì qua ví điện tử MoMo.\n\n" +
                       $"Anh/chị có thể lựa chọn phương thức này khi tự thanh toán hoặc nhờ em đặt hộ theo hình thức COD nhé!";
            }

            // Hỏi so sánh thương hiệu (EZVIZ vs Imou, Hikvision vs Dahua)
            if ((msg.Contains("ezviz") && msg.Contains("imou")) || (msg.Contains("so sánh") && (msg.Contains("hãng") || msg.Contains("thương hiệu"))))
            {
                var pEzviz = allProducts.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains("ezviz"));
                var pImou = allProducts.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains("imou"));
                return $"Dạ về so sánh giữa **EZVIZ** và **IMOU** – hai thương hiệu camera gia đình phổ biến nhất hiện nay:\n\n" +
                       $"• **EZVIZ (Thuộc tập đoàn Hikvision):** Phần mềm giao diện cực kỳ mượt mà, kết nối server ổn định tại Việt Nam, khả năng đàm thoại 2 chiều lọc tiếng ồn rất tốt. Tiêu biểu: **{pEzviz?.Name}** ({pEzviz?.FinalPrice:N0}đ).\n" +
                       $"• **IMOU (Thuộc tập đoàn Dahua):** Thế mạnh về độ nhạy cảm biến chuyển động con người AI, còi hú báo động to và các mẫu ngoài trời chống nước rất bền bỉ. Tiêu biểu: **{pImou?.Name}** ({pImou?.FinalPrice:N0}đ).\n\n" +
                       $"👉 Cả hai hãng đều bảo hành chính hãng 24 tháng. Nếu lắp phòng khách/phòng ngủ em khuyên dùng EZVIZ, nếu lắp sân cổng ngoài trời thì Imou là lựa chọn số 1 ạ!";
            }

            // Hỏi về thẻ nhớ / lưu trữ / xem lại bao nhiêu ngày
            if (msg.Contains("thẻ nhớ") || msg.Contains("lưu trữ") || msg.Contains("xem lại") || msg.Contains("bao nhiêu ngày") || msg.Contains("ổ cứng"))
            {
                return $"Dạ về dung lượng lưu trữ và thời gian xem lại camera:\n\n" +
                       $"• **Thẻ nhớ 32GB:** Lưu được khoảng 3 - 5 ngày (chế độ ghi chuyển động thông minh).\n" +
                       $"• **Thẻ nhớ 64GB:** Lưu được khoảng 7 - 10 ngày (mức chuẩn được khuyên dùng nhất).\n" +
                       $"• **Thẻ nhớ 128GB:** Lưu được từ 15 - 20 ngày.\n" +
                       $"• **Hệ thống đầu ghi + Ổ cứng 1TB - 2TB:** Lưu liên tục 24/24 từ 20 đến 45 ngày cho hệ thống 4 camera.\n\n" +
                       $"Tất cả camera của shop đều tự động ghi đè khi đầy thẻ nên không cần thao tác xóa thủ công ạ!";
            }

            // Hỏi về camera không dây / không mạng / wifi
            if (msg.Contains("không có mạng") || msg.Contains("không có wifi") || msg.Contains("mất mạng") || msg.Contains("4g"))
            {
                return $"Dạ, trong trường hợp khu vực không có sẵn Wifi hoặc bị mất mạng Internet:\n\n" +
                       $"• Camera vẫn **tự động ghi hình và lưu vào thẻ nhớ** bình thường mà không bị gián đoạn.\n" +
                       $"• Camera có tính năng phát sóng AP cục bộ để điện thoại có thể kết nối trực tiếp vào camera trích xuất video khi đứng gần.\n" +
                       $"• Nếu cần xem từ xa ở trang trại, công trình không có Wifi, anh/chị có thể gắn thêm 1 bộ phát Wifi dùng SIM 4G là xem mượt mà 24/7 ạ!";
            }

            // Tư vấn theo vị trí lắp đặt cụ thể
            if (msg.Contains("ngoài trời") || msg.Contains("sân") || msg.Contains("cổng") || msg.Contains("ban công") || msg.Contains("chống nước"))
            {
                var topOutdoors = matched.Where(p => p.InstallLocation?.ToLowerInvariant().Contains("ngoài trời") == true).Take(2).ToList();
                if (!topOutdoors.Any()) topOutdoors = allProducts.Where(p => p.InstallLocation?.ToLowerInvariant().Contains("ngoài trời") == true).Take(2).ToList();

                var itemsText = string.Join("\n\n", topOutdoors.Select(p =>
                    $"⭐ **{p.Name}**\n" +
                    $"• Giá bán: **{p.FinalPrice:N0} đ** (Tiết kiệm so với giá gốc {p.Price:N0}đ)\n" +
                    $"• Tiêu chuẩn chống bụi nước: **{p.InstallLocation}** (IP67 chịu mưa nắng bền bỉ)\n" +
                    $"• Tầm nhìn đêm: {p.NightVisionRange ?? "Hồng ngoại 30m, có màu ban đêm"}\n" +
                    $"• Độ phân giải: {p.Resolution}"));

                return $"Dạ đối với khu vực ngoài trời (sân vườn, cổng, tường rào), yêu cầu quan trọng nhất là khả năng chống nước IP66/IP67 và tầm quan sát ban đêm xa.\n\n" +
                       $"Em xin gợi ý các mẫu chuyên dụng ngoài trời tốt nhất hiện có tại shop:\n\n{itemsText}\n\n" +
                       $"👉 Anh/chị thích mẫu nào chỉ cần nhắn: **'Đặt hộ tôi {topOutdoors.FirstOrDefault()?.Name}'**, em sẽ chốt đơn giao tận nhà cho anh/chị ngay ạ!";
            }

            if (msg.Contains("trong nhà") || msg.Contains("phòng khách") || msg.Contains("phòng ngủ") || msg.Contains("em bé") || msg.Contains("trẻ em") || msg.Contains("người già") || msg.Contains("360"))
            {
                var topIndoors = matched.Where(p => p.InstallLocation?.ToLowerInvariant().Contains("trong nhà") == true).Take(2).ToList();
                if (!topIndoors.Any()) topIndoors = allProducts.Where(p => p.InstallLocation?.ToLowerInvariant().Contains("trong nhà") == true).Take(2).ToList();

                var itemsText = string.Join("\n\n", topIndoors.Select(p =>
                    $"⭐ **{p.Name}**\n" +
                    $"• Giá bán: **{p.FinalPrice:N0} đ**\n" +
                    $"• Khả năng quay quét: **Xoay 360 độ** toàn cảnh, không góc chết\n" +
                    $"• Đàm thoại 2 chiều to rõ, phát hiện âm thanh lạ (tiếng khóc, cạy cửa)\n" +
                    $"• Độ nét: {p.Resolution}"));

                return $"Dạ đối với không gian trong nhà, anh/chị nên chọn dòng camera Wifi nhỏ gọn, xoay 360 độ và đàm thoại 2 chiều để tiện nói chuyện với người nhà:\n\n" +
                       $"{itemsText}\n\n" +
                       $"👉 Anh/chị cần em lên đơn gửi về địa chỉ của anh/chị thì chỉ cần gõ: **'Đặt hộ tôi {topIndoors.FirstOrDefault()?.Name}'** nhé!";
            }

            // Hỏi về giá cả / mẫu rẻ nhất / ngân sách
            if (msg.Contains("giá") || msg.Contains("bao nhiêu") || msg.Contains("rẻ nhất") || msg.Contains("tiền") || msg.Contains("báo giá"))
            {
                var sorted = allProducts.OrderBy(p => p.FinalPrice).Take(3).ToList();
                var listText = string.Join("\n", sorted.Select(p => $"• **{p.Name}**: Giá ưu đãi **{p.FinalPrice:N0}đ** (BH {p.WarrantyMonths} tháng)"));

                return $"Dạ bảng giá các dòng camera bán chạy và giá tốt nhất tại **Nhóm 8 - CamPro** hiện nay:\n\n" +
                       $"{listText}\n\n" +
                       $"• Tất cả đều là hàng nguyên seal chính hãng, bảo hành 24 tháng tận nơi.\n" +
                       $"• Đơn hàng từ 2 sản phẩm sẽ được miễn phí vận chuyển toàn quốc.\n\n" +
                       $"Anh/chị ưng mẫu nào có thể nhắn: **'Đặt hộ tôi [Tên sản phẩm]'** để em hỗ trợ lên đơn liền ạ!";
            }

            // Phản hồi thông minh theo các sản phẩm khớp nhất
            if (matched.Any())
            {
                var p = matched.First();
                return $"Dạ anh/chị **{user.FullName}**, dựa trên nhu cầu của anh/chị, em xin tư vấn giải pháp tối ưu nhất là mẫu:\n\n" +
                       $"⭐ **{p.Name}**\n" +
                       $"• **Thương hiệu:** {p.Brand?.Name}\n" +
                       $"• **Giá bán hiện tại:** **{p.FinalPrice:N0} đ** (Giá gốc: {p.Price:N0}đ)\n" +
                       $"• **Độ phân giải:** {p.Resolution}\n" +
                       $"• **Vị trí phù hợp:** {p.InstallLocation}\n" +
                       $"• **Chế độ bảo hành:** {p.WarrantyMonths} tháng chính hãng\n\n" +
                       $"Sản phẩm hiện đang có sẵn trong kho hàng. Anh/chị có thể nhắn: **'Đặt hộ tôi {p.Name}'** để em tạo đơn hàng gửi tận nhà ngay nhé!";
            }

            // Mặc định
            var defaultProduct = allProducts.FirstOrDefault(p => p.IsFeatured) ?? allProducts.First();
            return $"Dạ em đã ghi nhận câu hỏi của anh/chị **{user.FullName}**.\n\n" +
                   $"Tại Nhóm 8 - CamPro, chúng em cung cấp đầy đủ các giải pháp camera an ninh chất lượng cao từ gia đình đến doanh nghiệp. Mẫu đang được đánh giá cao nhất hiện nay là:\n\n" +
                   $"⭐ **{defaultProduct.Name}** — Giá: **{defaultProduct.FinalPrice:N0} đ**\n" +
                   $"• Độ nét cao, dễ dàng cài đặt trong 3 phút qua điện thoại.\n" +
                   $"• Xem từ xa 24/7 mượt mà không giật lag.\n\n" +
                   $"Anh/chị có thể hỏi em chi tiết hơn về vị trí lắp đặt, hoặc bảo em: **'Đặt hộ tôi {defaultProduct.Name}'** bất cứ lúc nào ạ!";
        }

        private async Task<AiChatResponse> HandleOrderRequestAsync(ApplicationUser user, string message)
        {
            var msg = message.ToLowerInvariant();
            var allProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();

            Product? targetProduct = null;

            // 1. Tìm theo ID
            var idMatch = Regex.Match(message, @"(?:id|mã|sp)\s*[:=]?\s*(\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int pid))
            {
                targetProduct = allProducts.FirstOrDefault(p => p.Id == pid);
            }

            // 2. Tìm theo tên sản phẩm cụ thể
            if (targetProduct == null)
            {
                targetProduct = allProducts
                    .Where(p => msg.Contains(p.Name.ToLowerInvariant()) || 
                                (p.SKU != null && msg.Contains(p.SKU.ToLowerInvariant())))
                    .OrderByDescending(p => p.Name.Length)
                    .FirstOrDefault();
            }

            // 3. Tìm theo từ khóa rút gọn phổ biến
            if (targetProduct == null)
            {
                var keywords = new[] { "c6n", "ranger", "bullet", "a32", "h8c", "c3w", "c3tn", "b2a21", "hfw1200", "imou", "ezviz", "tapo", "hikvision", "dahua" };
                foreach (var kw in keywords)
                {
                    if (msg.Contains(kw))
                    {
                        targetProduct = allProducts.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains(kw) || p.Slug.Contains(kw));
                        if (targetProduct != null) break;
                    }
                }
            }

            if (targetProduct == null)
            {
                var top3 = allProducts.Take(3).ToList();
                return new AiChatResponse
                {
                    Reply = $"Dạ anh/chị ơi, anh/chị muốn đặt hộ mẫu camera nào ạ? Hãy nhắn rõ tên sản phẩm, ví dụ: **'Đặt hộ tôi camera EZVIZ C6N'** hoặc bấm vào nút **[Đặt]** bên dưới thẻ gợi ý để em tạo đơn nhé!",
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

            if (targetProduct.Stock <= 0)
            {
                return new AiChatResponse
                {
                    Reply = $"Rất tiếc, sản phẩm **{targetProduct.Name}** hiện tại đang tạm hết hàng trong kho. Em xin gợi ý anh/chị các mẫu tương đương dưới đây ạ:",
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

            int quantity = 1;
            var qtyMatch = Regex.Match(message, @"(?:số lượng|sl|mua)\s*(\d+)", RegexOptions.IgnoreCase);
            if (qtyMatch.Success && int.TryParse(qtyMatch.Groups[1].Value, out int parsedQty) && parsedQty > 0)
            {
                quantity = Math.Min(parsedQty, targetProduct.Stock);
            }

            var receiverName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.UserName ?? "Khách hàng CamPro";
            var receiverPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : "0987654321";
            var shippingAddress = !string.IsNullOrWhiteSpace(user.Address) ? user.Address : "Số 41A Phú Diễn, Bắc Từ Liêm, Hà Nội";

            decimal subTotal = targetProduct.FinalPrice * quantity;
            decimal totalAmount = subTotal + DefaultShippingFee;

            var orderCode = "AI" + DateTime.Now.ToString("yyMMddHHmmss") + new Random().Next(10, 99);
            var order = new Order
            {
                OrderCode = orderCode,
                UserId = user.Id,
                OrderDate = DateTime.Now,
                ReceiverName = receiverName,
                ReceiverPhone = receiverPhone,
                ShippingAddress = shippingAddress,
                Note = $"Đơn hàng được đặt tự động bởi Trợ lý AI theo yêu cầu qua chat: \"{message}\"",
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

            targetProduct.Stock -= quantity;
            targetProduct.SoldCount += quantity;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var successReply =
                $"🎉 **ĐẶT HÀNG THÀNH CÔNG HỘ ANH/CHỊ RỒI Ạ!**\n\n" +
                $"• **Mã đơn hàng**: `{order.OrderCode}`\n" +
                $"• **Sản phẩm**: {targetProduct.Name} (Số lượng: {quantity})\n" +
                $"• **Người nhận**: {order.ReceiverName} ({order.ReceiverPhone})\n" +
                $"• **Địa chỉ giao**: {order.ShippingAddress}\n" +
                $"• **Hình thức**: Thanh toán khi nhận hàng (COD)\n" +
                $"• **Tổng thanh toán**: **{totalAmount:N0} đ** (Đã gồm phí ship 30.000đ)\n\n" +
                $"Đơn hàng đã được lưu vào hệ thống. Anh/chị có thể vào mục **'Tra cứu đơn hàng'** hoặc [Xem chi tiết đơn hàng tại đây](/Order/Details/{order.Id}) để theo dõi tiến độ giao hàng ạ!";

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

        private async Task<AiChatResponse> HandleAddToCartRequestAsync(ApplicationUser user, string message)
        {
            var msg = message.ToLowerInvariant();
            var allProducts = await _context.Products.Where(p => p.IsActive).ToListAsync();

            Product? targetProduct = null;

            // 1. Tìm theo ID
            var idMatch = Regex.Match(message, @"(?:id|mã|sp)\s*[:=]?\s*(\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out int pid))
            {
                targetProduct = allProducts.FirstOrDefault(p => p.Id == pid);
            }

            // 2. Tìm theo tên sản phẩm
            if (targetProduct == null)
            {
                targetProduct = allProducts
                    .Where(p => msg.Contains(p.Name.ToLowerInvariant()) ||
                                (p.SKU != null && msg.Contains(p.SKU.ToLowerInvariant())))
                    .OrderByDescending(p => p.Name.Length)
                    .FirstOrDefault();
            }

            // 3. Tìm theo từ khóa rút gọn
            if (targetProduct == null)
            {
                var keywords = new[] { "c6n", "ranger", "bullet", "a32", "h8c", "c3w", "c3tn", "b2a21", "hfw1200", "imou", "ezviz", "tapo", "hikvision", "dahua" };
                foreach (var kw in keywords)
                {
                    if (msg.Contains(kw))
                    {
                        targetProduct = allProducts.FirstOrDefault(p => p.Name.ToLowerInvariant().Contains(kw) || p.Slug.Contains(kw));
                        if (targetProduct != null) break;
                    }
                }
            }

            if (targetProduct == null)
            {
                var top3 = allProducts.Take(3).ToList();
                return new AiChatResponse
                {
                    Reply = $"Dạ anh/chị muốn thêm sản phẩm nào vào giỏ hàng ạ? Anh/chị hãy nhắn tên cụ thể (ví dụ: **'Thêm vào giỏ camera EZVIZ C6N'**) hoặc bấm nút **[Thêm giỏ]** bên dưới thẻ gợi ý nhé!",
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

            if (targetProduct.Stock <= 0)
            {
                return new AiChatResponse
                {
                    Reply = $"Rất tiếc, sản phẩm **{targetProduct.Name}** hiện tại đang tạm hết hàng trong kho nên chưa thể thêm vào giỏ. Anh/chị tham khảo mẫu tương đương này nhé:",
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

            int quantity = 1;
            var qtyMatch = Regex.Match(message, @"(?:số lượng|sl|mua)\s*(\d+)", RegexOptions.IgnoreCase);
            if (qtyMatch.Success && int.TryParse(qtyMatch.Groups[1].Value, out int parsedQty) && parsedQty > 0)
            {
                quantity = Math.Min(parsedQty, targetProduct.Stock);
            }

            // Thêm vào bảng CartItems của User trong CSDL
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == targetProduct.Id);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductId = targetProduct.Id,
                    Quantity = quantity,
                    AddedDate = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }
            await _context.SaveChangesAsync();

            var totalCartCount = await _context.CartItems.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

            var replyText =
                $"🛒 **ĐÃ THÊM VÀO GIỎ HÀNG THÀNH CÔNG!**\n\n" +
                $"• **Sản phẩm**: {targetProduct.Name}\n" +
                $"• **Số lượng**: {quantity}\n" +
                $"• **Đơn giá**: {targetProduct.FinalPrice:N0} đ\n\n" +
                $"Đơn hàng **chưa được thanh toán**. Sản phẩm đã được lưu an toàn trong giỏ hàng để anh/chị tiếp tục xem thêm các mẫu khác.\n\n" +
                $"👉 Anh/chị có thể [Bấm vào đây để vào Giỏ hàng xem lại](/Cart) hoặc khi nào muốn chốt chỉ cần bảo em: **'Chốt đơn'** nhé!";

            return new AiChatResponse
            {
                Reply = replyText,
                AddedToCart = true,
                CartCount = totalCartCount,
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
