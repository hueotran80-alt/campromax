using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanCameraGiamSat.Models
{
    public class Voucher
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã voucher")]
        [StringLength(50)]
        [Display(Name = "Mã voucher")]
        public string Code { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Loại giảm giá")]
        public DiscountType Type { get; set; } = DiscountType.Percent;

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Giá trị giảm")]
        public decimal Value { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Đơn hàng tối thiểu")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Giảm tối đa")]
        public decimal? MaxDiscountAmount { get; set; }

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(1);

        [Display(Name = "Giới hạn lượt dùng")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Đã sử dụng")]
        public int UsedCount { get; set; } = 0;

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;
    }
}
