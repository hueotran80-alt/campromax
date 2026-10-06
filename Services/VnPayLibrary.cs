using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WebBanCameraGiamSat.Services
{
    /// <summary>
    /// Thư viện hỗ trợ tạo URL thanh toán và xác thực chữ ký (checksum) của VNPay Sandbox.
    /// Tham khảo cấu trúc chuẩn theo tài liệu tích hợp của VNPay (sandbox.vnpayment.vn/apis/docs).
    /// </summary>
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _requestData.Add(key, value);
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                _responseData.Add(key, value);
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
            string queryString = data.ToString();
            baseUrl += "?" + queryString;
            string signData = queryString;
            if (signData.Length > 0)
                signData = signData.Remove(signData.Length - 1, 1);
            string vnpSecureHash = HmacSha512(hashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnpSecureHash;
            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rawData = string.Join("&", _responseData
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
            string myChecksum = HmacSha512(secretKey, rawData);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string HmacSha512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var b in hashValue)
                    hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string GetIpAddress(HttpContext context)
        {
            try
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ip) || ip == "::1") return "127.0.0.1";
                return ip;
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vx = WebUtility.UrlEncode(x);
            var vy = WebUtility.UrlEncode(y);
            return string.CompareOrdinal(vx, vy);
        }
    }
}
