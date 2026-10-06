using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanCameraGiamSat.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal SubTotal { get; set; }
    }
}
