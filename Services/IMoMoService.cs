namespace WebBanCameraGiamSat.Services
{
    public interface IMoMoService
    {
        MoMoRequestResult CreateMoMoRequest(string orderCode, decimal amount, string orderInfo);
    }

    public class MoMoRequestResult
    {
        public string OrderCode { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string QrContent { get; set; } = string.Empty;
    }
}
