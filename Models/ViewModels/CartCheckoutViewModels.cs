using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(i => i.LineTotal);
        public int TotalQuantity => Items.Sum(i => i.Quantity);
    }

    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();

        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? Note { get; set; }

        public string? VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingFee { get; set; } = 30000;
        public decimal TotalAmount => Math.Max(0, Cart.SubTotal + ShippingFee - DiscountAmount);

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    }
}
