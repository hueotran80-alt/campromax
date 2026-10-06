using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, Order order);
        VnPayResponse PaymentExecute(IQueryCollection collections);
    }

    public class VnPayResponse
    {
        public bool Success { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string VnPayResponseCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
