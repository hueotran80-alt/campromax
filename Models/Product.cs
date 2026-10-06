using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanCameraGiamSat.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        [Required]
        [StringLength(220)]
        public string Slug { get; set; }

        [Required(ErrorMessage = "Chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Chọn thương hiệu")]
        [Display(Name = "Thương hiệu")]
        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Mã SKU")]
        public string SKU { get; set; }

        [StringLength(300)]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Giá gốc (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Giá khuyến mãi (VNĐ)")]
        public decimal? DiscountPrice { get; set; }

        [Required]
        [Display(Name = "Số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm")]
        public int Stock { get; set; }

        [Display(Name = "Đã bán")]
        public int SoldCount { get; set; } = 0;

        [Display(Name = "Lượt xem")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Hình ảnh chính")]
        public string? MainImageUrl { get; set; }

        // Thông số kỹ thuật đặc trưng của camera giám sát
        [StringLength(100)]
        [Display(Name = "Độ phân giải")]
        public string? Resolution { get; set; }

        [StringLength(100)]
        [Display(Name = "Kiểu kết nối")]
        public string? ConnectionType { get; set; }

        [StringLength(50)]
        [Display(Name = "Vị trí lắp đặt")]
        public string? InstallLocation { get; set; }

        [StringLength(100)]
        [Display(Name = "Khoảng cách hồng ngoại")]
        public string? NightVisionRange { get; set; }

        [StringLength(150)]
        [Display(Name = "Lưu trữ")]
        public string? StorageType { get; set; }

        [Display(Name = "Bảo hành (tháng)")]
        public int WarrantyMonths { get; set; } = 24;

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Đang kinh doanh")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal FinalPrice => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price ? DiscountPrice.Value : Price;

        [NotMapped]
        public int DiscountPercent => (DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price)
            ? (int)Math.Round((1 - (DiscountPrice.Value / Price)) * 100)
            : 0;

        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any(r => r.IsApproved) ? Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0;

        [NotMapped]
        public int ReviewCount => Reviews?.Count(r => r.IsApproved) ?? 0;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
