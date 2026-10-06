using System.ComponentModel.DataAnnotations;

namespace WebBanCameraGiamSat.Models
{
    public class NewsPost
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(220)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsPublished { get; set; } = true;
        public int ViewCount { get; set; } = 0;
    }
}
