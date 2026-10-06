using System.ComponentModel.DataAnnotations;

namespace WebBanCameraGiamSat.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public int OrderId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = true;
        public string? AdminReply { get; set; }
        public DateTime? AdminReplyDate { get; set; }
    }
}
