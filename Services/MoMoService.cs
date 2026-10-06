namespace WebBanCameraGiamSat.Services
{
    /// <summary>
    /// LƯU Ý: Đây là service MÔ PHỎNG luồng thanh toán MoMo (demo) vì dự án chưa có
    /// tài khoản merchant MoMo thật. Luồng hiển thị mã QR giả lập để minh họa quy trình
    /// thanh toán, sau đó người dùng bấm "Tôi đã thanh toán" để xác nhận đơn hàng (demo).
    /// Nếu có tài khoản merchant MoMo thật (PartnerCode/AccessKey/SecretKey), có thể thay
    /// thế bằng lời gọi API thật tới MoMoSettings:Endpoint theo tài liệu MoMo Payment Gateway.
    /// </summary>
    public class MoMoService : IMoMoService
    {
        private readonly IConfiguration _config;

        public MoMoService(IConfiguration config)
        {
            _config = config;
        }

        public MoMoRequestResult CreateMoMoRequest(string orderCode, decimal amount, string orderInfo)
        {
            return new MoMoRequestResult
            {
                OrderCode = orderCode,
                RequestId = Guid.NewGuid().ToString("N"),
                Amount = amount,
                OrderInfo = orderInfo,
                QrContent = $"MOMO|{orderCode}|{amount}|{DateTime.Now:yyyyMMddHHmmss}"
            };
        }
    }
}
