using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanCameraGiamSat.Helpers;
using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Dung EnsureCreated thay vi Migrate: tu tao toan bo bang theo dung Model hien tai,
            // khong can chay Add-Migration/Update-Database bang tay -> tranh loi "Invalid object name"
            // khi ai do quen buoc tao migration truoc khi chay du an.
            context.Database.EnsureCreated();

            await SeedRolesAndUsersAsync(userManager, roleManager);
            var categories = await SeedCategoriesAsync(context);
            var brands = await SeedBrandsAsync(context);
            await SeedProductsAsync(context, categories, brands);
            await SeedVouchersAsync(context);
            await SeedBannersAsync(context);
            await SeedNewsAsync(context);
            await SeedDemoOrderAndReviewAsync(context, userManager);
        }

        private static async Task SeedRolesAndUsersAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "KhachHang" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            if (await userManager.FindByEmailAsync("admin@camprosecurity.vn") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@camprosecurity.vn",
                    Email = "admin@camprosecurity.vn",
                    EmailConfirmed = true,
                    FullName = "Quản trị viên CamPro",
                    PhoneNumber = "0987654321",
                    Address = "41A Phú Diễn, Bắc Từ Liêm, Hà Nội",
                    CreatedDate = DateTime.Now
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (await userManager.FindByEmailAsync("khachhang@gmail.com") == null)
            {
                var customer = new ApplicationUser
                {
                    UserName = "khachhang@gmail.com",
                    Email = "khachhang@gmail.com",
                    EmailConfirmed = true,
                    FullName = "Trần Văn Khách",
                    PhoneNumber = "0912345678",
                    Address = "Số 10, Ngõ 25 Cầu Giấy, Hà Nội",
                    CreatedDate = DateTime.Now
                };
                var result = await userManager.CreateAsync(customer, "Khach@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(customer, "KhachHang");
            }
        }

        private static async Task<Dictionary<string, Category>> SeedCategoriesAsync(ApplicationDbContext context)
        {
            if (!context.Categories.Any())
            {
                var list = new List<Category>
                {
                    new() { Name = "Camera Wifi Trong Nhà", Slug = "camera-wifi-trong-nha", IconClass = "bi-camera-video", Description = "Camera WiFi thông minh lắp đặt trong nhà, xoay 360 độ, đàm thoại 2 chiều.", ImageUrl = "/images/categories/cat-wifi-trong-nha.jpg", DisplayOrder = 1 },
                    new() { Name = "Camera Wifi Ngoài Trời", Slug = "camera-wifi-ngoai-troi", IconClass = "bi-cloud-rain", Description = "Camera WiFi chống nước chuẩn IP67, chuyên dùng ngoài trời.", ImageUrl = "/images/categories/cat-wifi-ngoai-troi.jpg", DisplayOrder = 2 },
                    new() { Name = "Camera IP PoE", Slug = "camera-ip-poe", IconClass = "bi-hdd-network", Description = "Camera IP truyền hình ảnh và nguồn qua một dây mạng PoE, ổn định cho hệ thống lớn.", ImageUrl = "/images/categories/cat-ip-poe.jpg", DisplayOrder = 3 },
                    new() { Name = "Camera Analog HD", Slug = "camera-analog-hd", IconClass = "bi-camera", Description = "Camera Analog HD-CVI/TVI/AHD, chi phí thấp, dễ lắp đặt.", ImageUrl = "/images/categories/cat-analog.jpg", DisplayOrder = 4 },
                    new() { Name = "Đầu Ghi Hình NVR/DVR", Slug = "dau-ghi-hinh", IconClass = "bi-server", Description = "Đầu ghi hình lưu trữ dữ liệu camera, hỗ trợ xem qua điện thoại.", ImageUrl = "/images/categories/cat-daughi.jpg", DisplayOrder = 5 },
                    new() { Name = "Ổ Cứng Chuyên Dụng", Slug = "o-cung-chuyen-dung", IconClass = "bi-hdd", Description = "Ổ cứng chuyên dụng cho hệ thống camera giám sát hoạt động 24/7.", ImageUrl = "/images/categories/cat-ocung.jpg", DisplayOrder = 6 },
                    new() { Name = "Phụ Kiện Camera", Slug = "phu-kien-camera", IconClass = "bi-plug", Description = "Nguồn, dây cáp, đầu nối và các phụ kiện đi kèm hệ thống camera.", ImageUrl = "/images/categories/cat-phukien.jpg", DisplayOrder = 7 },
                };
                context.Categories.AddRange(list);
                await context.SaveChangesAsync();
            }
            return await context.Categories.ToDictionaryAsync(c => c.Slug, c => c);
        }

        private static async Task<Dictionary<string, Brand>> SeedBrandsAsync(ApplicationDbContext context)
        {
            if (!context.Brands.Any())
            {
                var list = new List<Brand>
                {
                    new() { Name = "Hikvision", Slug = "hikvision", LogoUrl = "/images/brands/brand-hikvision.jpg", Description = "Thương hiệu camera giám sát hàng đầu thế giới." },
                    new() { Name = "Dahua", Slug = "dahua", LogoUrl = "/images/brands/brand-dahua.jpg", Description = "Giải pháp camera an ninh chuyên nghiệp đến từ Trung Quốc." },
                    new() { Name = "KBVision", Slug = "kbvision", LogoUrl = "/images/brands/brand-kbvision.jpg", Description = "Camera giám sát thương hiệu Hàn Quốc, giá tốt." },
                    new() { Name = "Imou", Slug = "imou", LogoUrl = "/images/brands/brand-imou.jpg", Description = "Camera thông minh cho gia đình, dễ sử dụng." },
                    new() { Name = "EZVIZ", Slug = "ezviz", LogoUrl = "/images/brands/brand-ezviz.jpg", Description = "Camera thông minh kết nối qua ứng dụng di động." },
                    new() { Name = "Vantech", Slug = "vantech", LogoUrl = "/images/brands/brand-vantech.jpg", Description = "Thương hiệu camera Việt Nam quen thuộc." },
                    new() { Name = "Questek", Slug = "questek", LogoUrl = "/images/brands/brand-questek.jpg", Description = "Camera an ninh phổ thông, phù hợp hộ gia đình." },
                    new() { Name = "TP-Link Tapo", Slug = "tp-link-tapo", LogoUrl = "/images/brands/brand-tapo.jpg", Description = "Dòng camera thông minh của TP-Link." },
                    new() { Name = "Western Digital", Slug = "western-digital", LogoUrl = "/images/brands/brand-wd.jpg", Description = "Ổ cứng chuyên dụng cho hệ thống camera (WD Purple)." },
                    new() { Name = "Seagate", Slug = "seagate", LogoUrl = "/images/brands/brand-seagate.jpg", Description = "Ổ cứng SkyHawk chuyên dụng giám sát an ninh." },
                };
                context.Brands.AddRange(list);
                await context.SaveChangesAsync();
            }
            return await context.Brands.ToDictionaryAsync(b => b.Slug, b => b);
        }

        private static async Task SeedProductsAsync(ApplicationDbContext context, Dictionary<string, Category> cat, Dictionary<string, Brand> brand)
        {
            if (context.Products.Any()) return;

            var products = new List<Product>
            {
                // Camera Wifi Trong Nha
                NewProduct(1, "Camera Imou Ranger 2 2MP Wifi Xoay 360", cat["camera-wifi-trong-nha"], brand["imou"], "CAM-IMOU-R2", 590000, 490000, 50,
                    "2MP Full HD 1080P", "Wifi 2.4GHz", "Trong nhà", "10m hồng ngoại", "Thẻ nhớ Micro SD (tối đa 256GB) / Cloud",
                    "Camera xoay 360 độ, phát hiện chuyển động thông minh, đàm thoại 2 chiều.", true),
                NewProduct(2, "Camera EZVIZ C6N 2MP Xoay Thông Minh", cat["camera-wifi-trong-nha"], brand["ezviz"], "CAM-EZVIZ-C6N", 690000, 590000, 40,
                    "2MP Full HD 1080P", "Wifi 2.4GHz", "Trong nhà", "12m hồng ngoại", "Thẻ nhớ Micro SD / Cloud EZVIZ",
                    "Theo dõi qua app EZVIZ, cảnh báo AI phát hiện người, hỗ trợ đàm thoại 2 chiều.", false),
                NewProduct(3, "Camera TP-Link Tapo C200 Xoay 360", cat["camera-wifi-trong-nha"], brand["tp-link-tapo"], "CAM-TAPO-C200", 550000, null, 60,
                    "2MP Full HD 1080P", "Wifi 2.4GHz", "Trong nhà", "9m hồng ngoại", "Thẻ nhớ Micro SD (tối đa 128GB) / Cloud",
                    "Dễ lắp đặt qua app Tapo, hỗ trợ quét theo chuyển động, tương thích Alexa/Google Home.", false),
                NewProduct(4, "Camera Vantech VP-6602B Wifi Mini", cat["camera-wifi-trong-nha"], brand["vantech"], "CAM-VANTECH-6602B", 450000, 390000, 35,
                    "2MP Full HD 1080P", "Wifi 2.4GHz", "Trong nhà", "8m hồng ngoại", "Thẻ nhớ Micro SD (tối đa 128GB)",
                    "Thiết kế nhỏ gọn, giá rẻ, phù hợp giám sát cửa hàng nhỏ, văn phòng.", false),

                // Camera Wifi Ngoai Troi
                NewProduct(5, "Camera Imou Bullet 2C 4MP Ngoài Trời", cat["camera-wifi-ngoai-troi"], brand["imou"], "CAM-IMOU-BULLET2C", 890000, 750000, 30,
                    "4MP 2K QHD", "Wifi 2.4GHz", "Ngoài trời (IP67)", "30m hồng ngoại", "Thẻ nhớ Micro SD (tối đa 256GB) / Cloud",
                    "Chống nước IP67, hình ảnh sắc nét 4MP, phát hiện người bằng AI chính xác.", true),
                NewProduct(6, "Camera EZVIZ C3N 2MP Chống Nước IP67", cat["camera-wifi-ngoai-troi"], brand["ezviz"], "CAM-EZVIZ-C3N", 990000, null, 25,
                    "2MP Full HD 1080P", "Wifi 2.4GHz", "Ngoài trời (IP67)", "30m hồng ngoại", "Thẻ nhớ Micro SD / Cloud EZVIZ",
                    "Vỏ hợp kim chống chịu thời tiết, cảnh báo chuyển động chủ động, còi hú tích hợp.", false),
                NewProduct(7, "Camera Hikvision DS-2CV2021 2MP Wifi", cat["camera-wifi-ngoai-troi"], brand["hikvision"], "CAM-HIK-2CV2021", 1290000, 1090000, 20,
                    "2MP Full HD 1080P", "Wifi 2.4GHz", "Ngoài trời (IP66)", "30m hồng ngoại", "Thẻ nhớ Micro SD (tối đa 128GB)",
                    "Thương hiệu Hikvision chính hãng, ổn định, hình ảnh sắc nét cả ban đêm.", true),
                NewProduct(8, "Camera TP-Link Tapo C310 3MP Outdoor", cat["camera-wifi-ngoai-troi"], brand["tp-link-tapo"], "CAM-TAPO-C310", 850000, null, 28,
                    "3MP 2K QHD", "Wifi 2.4GHz", "Ngoài trời (IP66)", "30m hồng ngoại", "Thẻ nhớ Micro SD (tối đa 256GB) / Cloud",
                    "Chống nước IP66, phát hiện chuyển động thông minh, xem trực tiếp qua app Tapo.", false),

                // Camera IP PoE
                NewProduct(9, "Camera Hikvision DS-2CD1323G0E-I 2MP PoE", cat["camera-ip-poe"], brand["hikvision"], "CAM-HIK-2CD1323G0E", 1450000, 1290000, 40,
                    "2MP Full HD 1080P", "IP PoE (dây mạng)", "Trong nhà/Ngoài trời (IP67)", "30m hồng ngoại", "Ghi qua đầu ghi NVR",
                    "Camera IP PoE ổn định cho hệ thống camera doanh nghiệp, hỗ trợ thẻ nhớ tại chỗ.", true),
                NewProduct(10, "Camera Dahua IPC-HFW1230S 2MP PoE", cat["camera-ip-poe"], brand["dahua"], "CAM-DAHUA-HFW1230S", 1390000, null, 38,
                    "2MP Full HD 1080P", "IP PoE (dây mạng)", "Ngoài trời (IP67)", "30m hồng ngoại", "Ghi qua đầu ghi NVR",
                    "Camera thân trụ chống nước, độ bền cao, hình ảnh ổn định trong điều kiện thiếu sáng.", false),
                NewProduct(11, "Camera KBVision KX-2004N2 2MP IP", cat["camera-ip-poe"], brand["kbvision"], "CAM-KB-2004N2", 1190000, 990000, 33,
                    "2MP Full HD 1080P", "IP PoE (dây mạng)", "Trong nhà/Ngoài trời", "25m hồng ngoại", "Ghi qua đầu ghi NVR",
                    "Giá thành hợp lý, phù hợp lắp đặt cho cửa hàng, nhà xưởng quy mô vừa.", false),
                NewProduct(12, "Camera Hikvision DS-2CD2043G0-I 4MP", cat["camera-ip-poe"], brand["hikvision"], "CAM-HIK-2CD2043G0", 2390000, null, 15,
                    "4MP 2K QHD", "IP PoE (dây mạng)", "Ngoài trời (IP67)", "30m hồng ngoại", "Ghi qua đầu ghi NVR",
                    "Độ phân giải cao 4MP, phù hợp khu vực cần nhận diện chi tiết như biển số xe.", false),

                // Camera Analog
                NewProduct(13, "Camera Dahua HAC-HFW1200T 2MP", cat["camera-analog-hd"], brand["dahua"], "CAM-DAHUA-HFW1200T", 590000, 490000, 55,
                    "2MP Full HD 1080P", "Analog HDCVI (dây đồng trục)", "Ngoài trời (IP67)", "20m hồng ngoại", "Ghi qua đầu ghi DVR",
                    "Lựa chọn tiết kiệm chi phí cho hệ thống camera analog phổ thông.", false),
                NewProduct(14, "Camera KBVision KX-2K11C4 4MP", cat["camera-analog-hd"], brand["kbvision"], "CAM-KB-2K11C4", 890000, null, 42,
                    "4MP 2K QHD", "Analog HDCVI (dây đồng trục)", "Ngoài trời (IP67)", "20m hồng ngoại", "Ghi qua đầu ghi DVR",
                    "Độ phân giải cao trên nền tảng analog, dễ nâng cấp từ hệ thống cũ.", false),
                NewProduct(15, "Camera Questek Win-6153S 2MP", cat["camera-analog-hd"], brand["questek"], "CAM-QUESTEK-6153S", 550000, 470000, 47,
                    "2MP Full HD 1080P", "Analog HDTVI (dây đồng trục)", "Trong nhà/Ngoài trời", "20m hồng ngoại", "Ghi qua đầu ghi DVR",
                    "Camera phổ thông giá rẻ, phù hợp hộ gia đình, cửa hàng nhỏ.", false),
                NewProduct(16, "Camera Vantech VP-2000C 2MP", cat["camera-analog-hd"], brand["vantech"], "CAM-VANTECH-2000C", 490000, 420000, 60,
                    "2MP Full HD 1080P", "Analog HDCVI (dây đồng trục)", "Ngoài trời (IP66)", "20m hồng ngoại", "Ghi qua đầu ghi DVR",
                    "Thân camera kim loại chắc chắn, chống chịu thời tiết tốt.", false),

                // Dau ghi hinh
                NewProduct(17, "Đầu Ghi Hình Hikvision DS-7108HGHI-K1 8 Kênh", cat["dau-ghi-hinh"], brand["hikvision"], "DVR-HIK-7108K1", 1690000, null, 20,
                    "Hỗ trợ xuất HDMI/VGA Full HD", "Cổng BNC x8 + LAN", "Trong nhà", "-", "Khay ổ cứng SATA (tối đa 6TB)",
                    "Đầu ghi 8 kênh hỗ trợ camera Analog, xem từ xa qua điện thoại bằng app Hik-Connect.", true),
                NewProduct(18, "Đầu Ghi Hình Dahua XVR5104HS-I3 4 Kênh", cat["dau-ghi-hinh"], brand["dahua"], "DVR-DAHUA-5104I3", 1290000, null, 25,
                    "Hỗ trợ xuất HDMI/VGA Full HD", "Cổng BNC x4 + LAN", "Trong nhà", "-", "Khay ổ cứng SATA (tối đa 6TB)",
                    "Tích hợp AI cơ bản, phù hợp hộ gia đình quy mô nhỏ 4 camera.", false),
                NewProduct(19, "Đầu Ghi Hình KBVision KX-8108D6 8 Kênh", cat["dau-ghi-hinh"], brand["kbvision"], "DVR-KB-8108D6", 1590000, 1390000, 18,
                    "Hỗ trợ xuất HDMI/VGA Full HD", "Cổng BNC x8 + LAN", "Trong nhà", "-", "Khay ổ cứng SATA (tối đa 8TB)",
                    "Đầu ghi 8 kênh ổn định, hỗ trợ xem qua điện thoại, cảnh báo qua app.", false),
                NewProduct(20, "Đầu Ghi Hình Hikvision DS-7616NI-K2 16 Kênh NVR", cat["dau-ghi-hinh"], brand["hikvision"], "NVR-HIK-7616K2", 4590000, null, 10,
                    "Hỗ trợ xuất HDMI/VGA 4K", "Cổng LAN PoE x16", "Trong nhà", "-", "Khay ổ cứng SATA x2 (tối đa 20TB)",
                    "NVR 16 kênh chuyên nghiệp cho doanh nghiệp, hỗ trợ nhận diện khuôn mặt AI.", false),

                // O cung
                NewProduct(21, "Ổ Cứng Western Digital Purple 1TB Surveillance", cat["o-cung-chuyen-dung"], brand["western-digital"], "HDD-WD-PURPLE-1TB", 990000, null, 50,
                    "-", "SATA III", "-", "-", "1TB - chuyên dụng ghi hình camera 24/7",
                    "Ổ cứng WD Purple tối ưu cho hệ thống giám sát, hoạt động liên tục ổn định.", false),
                NewProduct(22, "Ổ Cứng Western Digital Purple 2TB Surveillance", cat["o-cung-chuyen-dung"], brand["western-digital"], "HDD-WD-PURPLE-2TB", 1590000, 1450000, 40,
                    "-", "SATA III", "-", "-", "2TB - chuyên dụng ghi hình camera 24/7",
                    "Dung lượng lớn, lưu trữ dữ liệu camera lâu dài, độ bền cao.", true),
                NewProduct(23, "Ổ Cứng Seagate SkyHawk 2TB", cat["o-cung-chuyen-dung"], brand["seagate"], "HDD-SEAGATE-SKYHAWK-2TB", 1550000, null, 35,
                    "-", "SATA III", "-", "-", "2TB - chuyên dụng ghi hình camera 24/7",
                    "Ổ cứng chuyên dụng cho đầu ghi camera, tối ưu hiệu năng ghi liên tục.", false),
                NewProduct(24, "Ổ Cứng Seagate SkyHawk 4TB", cat["o-cung-chuyen-dung"], brand["seagate"], "HDD-SEAGATE-SKYHAWK-4TB", 2690000, 2490000, 20,
                    "-", "SATA III", "-", "-", "4TB - chuyên dụng ghi hình camera 24/7",
                    "Dung lượng lớn 4TB, phù hợp hệ thống nhiều camera ghi hình dài ngày.", false),

                // Phu kien
                NewProduct(25, "Nguồn Adapter 12V 2A Cho Camera", cat["phu-kien-camera"], brand["vantech"], "PK-ADAPTER-12V2A", 65000, null, 200,
                    "-", "-", "-", "-", "-",
                    "Nguồn adapter chính hãng, ổn áp, đủ dòng cho camera hoạt động bền bỉ.", false),
                NewProduct(26, "Bộ Nguồn Tổng 9 Kênh 5A Cho Hệ Thống Camera", cat["phu-kien-camera"], brand["vantech"], "PK-NGUONTONG-9CH5A", 350000, 290000, 60,
                    "-", "-", "-", "-", "-",
                    "Cấp nguồn tập trung cho 9 camera, tiết kiệm dây điện, dễ quản lý.", false),
                NewProduct(27, "Dây Cáp Đồng Trục RG59 Có Nguồn 100m", cat["phu-kien-camera"], brand["questek"], "PK-CAPDONGTRUC-100M", 690000, null, 45,
                    "-", "-", "-", "-", "-",
                    "Dây cáp tín hiệu kèm dây nguồn, thuận tiện đi dây cho camera analog.", false),
                NewProduct(28, "Đầu Chuyển Đổi HDMI To VGA Cho Đầu Ghi", cat["phu-kien-camera"], brand["kbvision"], "PK-HDMI2VGA", 190000, null, 80,
                    "-", "-", "-", "-", "-",
                    "Chuyển đổi tín hiệu HDMI sang VGA, hỗ trợ kết nối đầu ghi với màn hình đời cũ.", false),
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Seed anh phu (gallery) tu chinh anh chinh, tao them 1-2 goc chup demo
            var images = new List<ProductImage>();
            foreach (var p in products)
            {
                images.Add(new ProductImage { ProductId = p.Id, ImageUrl = p.MainImageUrl!, DisplayOrder = 0 });
            }
            context.ProductImages.AddRange(images);
            await context.SaveChangesAsync();
        }

        private static Product NewProduct(int id, string name, Category cat, Brand brand, string sku,
            decimal price, decimal? discountPrice, int stock,
            string resolution, string connectionType, string installLocation, string nightVision, string storage,
            string shortDesc, bool featured)
        {
            string slug = SlugHelper.GenerateSlug(name);
            return new Product
            {
                // Khong gan Id thu cong: cot Id la IDENTITY (tu tang) trong SQL Server,
                // gan tay se gay loi "Cannot insert explicit value for identity column".
                // Tham so "id" chi dung de danh so anh (product{id}.jpg) va tinh SoldCount/ViewCount mau.
                Name = name,
                Slug = slug,
                CategoryId = cat.Id,
                BrandId = brand.Id,
                SKU = sku,
                Price = price,
                DiscountPrice = discountPrice,
                Stock = stock,
                SoldCount = Math.Max(0, (id * 7) % 65),
                ViewCount = Math.Max(0, (id * 23) % 500),
                MainImageUrl = $"/images/products/product{id}.jpg",
                Resolution = resolution,
                ConnectionType = connectionType,
                InstallLocation = installLocation,
                NightVisionRange = nightVision,
                StorageType = storage,
                WarrantyMonths = 24,
                ShortDescription = shortDesc,
                Description = $"<p>{shortDesc}</p><ul>" +
                    $"<li><strong>Độ phân giải:</strong> {resolution}</li>" +
                    $"<li><strong>Kết nối:</strong> {connectionType}</li>" +
                    $"<li><strong>Vị trí lắp đặt:</strong> {installLocation}</li>" +
                    $"<li><strong>Hồng ngoại ban đêm:</strong> {nightVision}</li>" +
                    $"<li><strong>Lưu trữ:</strong> {storage}</li>" +
                    $"<li><strong>Bảo hành:</strong> 24 tháng chính hãng</li></ul>",
                IsFeatured = featured,
                IsActive = true,
                CreatedDate = DateTime.Now.AddDays(-id)
            };
        }

        private static async Task SeedVouchersAsync(ApplicationDbContext context)
        {
            if (context.Vouchers.Any()) return;

            context.Vouchers.AddRange(new List<Voucher>
            {
                new() { Code = "CAMPRO50", Description = "Giảm 5% cho đơn hàng từ 1.000.000đ, tối đa 200.000đ", Type = DiscountType.Percent, Value = 5, MinOrderAmount = 1000000, MaxDiscountAmount = 200000, StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddMonths(2), UsageLimit = 500, IsActive = true },
                new() { Code = "FREESHIP", Description = "Miễn phí vận chuyển cho đơn hàng từ 300.000đ", Type = DiscountType.FixedAmount, Value = 30000, MinOrderAmount = 300000, StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddMonths(3), UsageLimit = 1000, IsActive = true },
                new() { Code = "NEWUSER100", Description = "Giảm ngay 100.000đ cho khách hàng mới, đơn từ 2.000.000đ", Type = DiscountType.FixedAmount, Value = 100000, MinOrderAmount = 2000000, StartDate = DateTime.Now.AddDays(-5), EndDate = DateTime.Now.AddMonths(1), UsageLimit = 200, IsActive = true },
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedBannersAsync(ApplicationDbContext context)
        {
            if (context.Banners.Any()) return;

            context.Banners.AddRange(new List<Banner>
            {
                new() { Title = "Camera Giám Sát Chính Hãng", SubTitle = "Bảo hành 24 tháng - Đổi trả trong 7 ngày", ImageUrl = "/images/banners/banner1.jpg", LinkUrl = "/san-pham", DisplayOrder = 1, IsActive = true },
                new() { Title = "Miễn Phí Lắp Đặt", SubTitle = "Áp dụng nội thành Hà Nội cho đơn từ 3 camera", ImageUrl = "/images/banners/banner2.jpg", LinkUrl = "/san-pham", DisplayOrder = 2, IsActive = true },
                new() { Title = "Trả Góp 0% Lãi Suất", SubTitle = "Duyệt hồ sơ nhanh chóng trong 15 phút", ImageUrl = "/images/banners/banner3.jpg", LinkUrl = "/san-pham", DisplayOrder = 3, IsActive = true },
            });
            await context.SaveChangesAsync();
        }

        private static async Task SeedNewsAsync(ApplicationDbContext context)
        {
            if (context.NewsPosts.Any()) return;

            var posts = new List<NewsPost>
            {
                new()
                {
                    Title = "Hướng Dẫn Chọn Mua Camera Giám Sát Phù Hợp Với Nhu Cầu",
                    Summary = "Những tiêu chí quan trọng cần lưu ý khi lựa chọn camera giám sát cho gia đình và cửa hàng.",
                    Content = "<p>Việc lựa chọn camera giám sát phù hợp phụ thuộc vào nhiều yếu tố như vị trí lắp đặt, độ phân giải, khả năng lưu trữ và ngân sách.</p><p>Với không gian trong nhà, camera WiFi xoay 360 độ là lựa chọn linh hoạt. Với khu vực ngoài trời, nên ưu tiên camera có chuẩn chống nước IP66/IP67 trở lên.</p><p>Đối với hệ thống nhiều camera, camera IP PoE hoặc Analog kết hợp đầu ghi hình sẽ tiết kiệm chi phí vận hành lâu dài hơn.</p>",
                    ImageUrl = "/images/banners/news1.jpg",
                    IsPublished = true,
                    CreatedDate = DateTime.Now.AddDays(-10)
                },
                new()
                {
                    Title = "5 Lý Do Nên Sử Dụng Camera IP Thay Vì Camera Analog",
                    Summary = "So sánh ưu nhược điểm giữa camera IP và camera Analog truyền thống.",
                    Content = "<p>Camera IP cho chất lượng hình ảnh sắc nét hơn, dễ mở rộng hệ thống và hỗ trợ nhiều tính năng AI thông minh như nhận diện khuôn mặt, phát hiện chuyển động chính xác.</p><p>Trong khi đó, camera Analog vẫn là lựa chọn tiết kiệm chi phí ban đầu, phù hợp với các hệ thống quy mô nhỏ hoặc nâng cấp từ hệ thống cũ.</p>",
                    ImageUrl = "/images/banners/news2.jpg",
                    IsPublished = true,
                    CreatedDate = DateTime.Now.AddDays(-6)
                },
                new()
                {
                    Title = "Cách Lắp Đặt Đầu Ghi Hình NVR/DVR Tại Nhà Đơn Giản",
                    Summary = "Hướng dẫn từng bước kết nối đầu ghi hình với camera và điện thoại di động.",
                    Content = "<p>Bước 1: Kết nối camera với đầu ghi hình qua dây mạng (NVR) hoặc dây đồng trục (DVR).</p><p>Bước 2: Kết nối đầu ghi hình với TV/màn hình qua cổng HDMI hoặc VGA để kiểm tra hình ảnh.</p><p>Bước 3: Kết nối đầu ghi hình với mạng Internet, tải ứng dụng hãng và quét mã QR để xem từ xa qua điện thoại.</p>",
                    ImageUrl = "/images/banners/news3.jpg",
                    IsPublished = true,
                    CreatedDate = DateTime.Now.AddDays(-2)
                }
            };

            foreach (var p in posts) p.Slug = SlugHelper.GenerateSlug(p.Title);
            context.NewsPosts.AddRange(posts);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDemoOrderAndReviewAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.Orders.Any()) return;

            var customer = await userManager.FindByEmailAsync("khachhang@gmail.com");
            // Tim theo SKU thay vi Id, vi Id gio duoc SQL Server tu sinh (khong con co dinh bang 1)
            var product = await context.Products.FirstOrDefaultAsync(p => p.SKU == "CAM-IMOU-R2");
            if (customer == null || product == null) return;

            var order = new Order
            {
                OrderCode = "DH" + DateTime.Now.AddDays(-15).ToString("yyMMddHHmmss"),
                UserId = customer.Id,
                OrderDate = DateTime.Now.AddDays(-15),
                ReceiverName = customer.FullName,
                ReceiverPhone = customer.PhoneNumber ?? "0912345678",
                ShippingAddress = customer.Address ?? "Hà Nội",
                SubTotal = product.FinalPrice * 2,
                ShippingFee = 30000,
                DiscountAmount = 0,
                TotalAmount = product.FinalPrice * 2 + 30000,
                Status = OrderStatus.DaGiaoHang,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.DaThanhToan,
                UpdatedDate = DateTime.Now.AddDays(-12)
            };
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.MainImageUrl,
                Quantity = 2,
                UnitPrice = product.FinalPrice,
                SubTotal = product.FinalPrice * 2
            });

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            context.Reviews.Add(new Review
            {
                ProductId = product.Id,
                UserId = customer.Id,
                OrderId = order.Id,
                Rating = 5,
                Comment = "Camera dùng rất ổn định, hình ảnh nét, đóng gói cẩn thận. Giao hàng nhanh, sẽ ủng hộ shop tiếp!",
                CreatedDate = DateTime.Now.AddDays(-10),
                IsApproved = true
            });
            await context.SaveChangesAsync();
        }
    }
}
