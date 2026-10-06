using WebBanCameraGiamSat.Models;

namespace WebBanCameraGiamSat.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        private IConfigurationSection GetVnPaySection()
        {
            var section = _config.GetSection("VnPay");
            if (section.Exists() && !string.IsNullOrEmpty(section["TmnCode"]))
                return section;
            return _config.GetSection("VnPaySettings");
        }

        public string CreatePaymentUrl(HttpContext context, Order order)
        {
            var timeNow = DateTime.Now;
            var pay = new VnPayLibrary();
            var settings = GetVnPaySection();

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", settings["TmnCode"] ?? "");
            pay.AddRequestData("vnp_Amount", ((long)order.TotalAmount * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {order.OrderCode}");
            pay.AddRequestData("vnp_OrderType", "other");

            string returnUrl = settings["ReturnUrl"] ?? "";
            if (string.IsNullOrEmpty(returnUrl) || returnUrl.Contains("localhost"))
            {
                var request = context.Request;
                returnUrl = $"{request.Scheme}://{request.Host}/Payment/VnPayReturn";
            }
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);
            pay.AddRequestData("vnp_TxnRef", order.OrderCode);

            string baseUrl = settings["BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string hashSecret = settings["HashSecret"] ?? "";
            string paymentUrl = pay.CreateRequestUrl(baseUrl, hashSecret);
            return paymentUrl;
        }

        public VnPayResponse PaymentExecute(IQueryCollection collections)
        {
            var pay = new VnPayLibrary();
            var settings = GetVnPaySection();

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    pay.AddResponseData(key, value.ToString());
            }

            string orderCode = pay.GetResponseData("vnp_TxnRef");
            string vnpTransactionId = pay.GetResponseData("vnp_TransactionNo");
            string vnpResponseCode = pay.GetResponseData("vnp_ResponseCode");
            string vnpSecureHash = collections.ContainsKey("vnp_SecureHash") ? collections["vnp_SecureHash"].ToString() : string.Empty;

            bool validSignature = !string.IsNullOrEmpty(vnpSecureHash) && pay.ValidateSignature(vnpSecureHash, settings["HashSecret"] ?? "");

            return new VnPayResponse
            {
                Success = validSignature && vnpResponseCode == "00",
                OrderCode = orderCode,
                TransactionId = vnpTransactionId,
                VnPayResponseCode = vnpResponseCode,
                Message = validSignature
                    ? (vnpResponseCode == "00" ? "Giao dịch thành công" : "Giao dịch không thành công / bị hủy")
                    : "Chữ ký không hợp lệ (dữ liệu có thể đã bị thay đổi)"
            };
        }
    }
}
