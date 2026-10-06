using System.ComponentModel.DataAnnotations;

namespace WebBanCameraGiamSat.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu")]
        [StringLength(100)]
        [Display(Name = "Tên thương hiệu")]
        public string Name { get; set; }

        [StringLength(120)]
        public string Slug { get; set; }

        [Display(Name = "Logo")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
