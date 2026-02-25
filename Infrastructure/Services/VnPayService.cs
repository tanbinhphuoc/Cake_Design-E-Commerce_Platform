using Application.Services;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _paymentUrl;
        private readonly string _returnUrl;

        public VnPayService(IConfiguration configuration)
        {
            _tmnCode = configuration["Vnpay:TmnCode"] ?? throw new InvalidOperationException("Vnpay:TmnCode not configured");
            _hashSecret = configuration["Vnpay:HashSecret"] ?? throw new InvalidOperationException("Vnpay:HashSecret not configured");
            _paymentUrl = configuration["Vnpay:PaymentUrl"] ?? throw new InvalidOperationException("Vnpay:PaymentUrl not configured");
            _returnUrl = configuration["Vnpay:ReturnUrl"] ?? throw new InvalidOperationException("Vnpay:ReturnUrl not configured");
        }

        public string CreatePaymentUrl(Guid orderId, decimal amount, string orderInfo, string ipAddress)
        {
            var vnpayData = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(amount * 100)).ToString() }, // VNPay requires amount * 100
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", orderId.ToString() },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", _returnUrl },
                { "vnp_IpAddr", ipAddress },
                { "vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss") },
                { "vnp_ExpireDate", DateTime.UtcNow.AddMinutes(15).ToString("yyyyMMddHHmmss") }
            };

            var queryString = BuildQueryString(vnpayData);
            var signData = queryString;
            var secureHash = HmacSha512(_hashSecret, signData);

            return $"{_paymentUrl}?{queryString}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateCallback(Dictionary<string, string> vnpayData)
        {
            if (!vnpayData.ContainsKey("vnp_SecureHash"))
                return false;

            var secureHash = vnpayData["vnp_SecureHash"];

            // Remove hash params before validation
            var dataToValidate = new SortedDictionary<string, string>();
            foreach (var kv in vnpayData)
            {
                if (kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType" && !string.IsNullOrEmpty(kv.Value))
                {
                    dataToValidate[kv.Key] = kv.Value;
                }
            }

            var queryString = BuildQueryString(dataToValidate);
            var checkHash = HmacSha512(_hashSecret, queryString);

            return string.Equals(secureHash, checkHash, StringComparison.InvariantCultureIgnoreCase);
        }

        public string GetResponseCode(Dictionary<string, string> vnpayData)
        {
            return vnpayData.ContainsKey("vnp_ResponseCode") ? vnpayData["vnp_ResponseCode"] : "";
        }

        private static string BuildQueryString(SortedDictionary<string, string> data)
        {
            var queryParts = new List<string>();
            foreach (var kv in data)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    queryParts.Add($"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}");
                }
            }
            return string.Join("&", queryParts);
        }

        private static string HmacSha512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
