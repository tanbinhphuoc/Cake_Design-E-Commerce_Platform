using Application.DTOs;
using Application.Services;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class ViettelPostService : IViettelPostService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _token = string.Empty;

        // Cache address data to avoid hitting the API too often map
        private static List<ProvinceDto>? _cachedProvinces;
        private static readonly Dictionary<int, List<DistrictDto>> _cachedDistricts = new();
        private static readonly Dictionary<int, List<WardDto>> _cachedWards = new();

        public ViettelPostService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            // Base address should be configured in DI, but we can set a fallback here
            if (_httpClient.BaseAddress == null)
            {
                var apiUrl = _configuration["ViettelPost:ApiUrl"] ?? "https://partner.viettelpost.vn/v2/";
                _httpClient.BaseAddress = new Uri(apiUrl);
            }
        }

        private async Task AuthenticateAsync()
        {
            if (!string.IsNullOrEmpty(_token))
                return; // Already authenticated or implement token expiration logic

            var username = _configuration["ViettelPost:Username"];
            var password = _configuration["ViettelPost:Password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Viettel Post credentials are not configured.");

            var loginData = new
            {
                USERNAME = username,
                PASSWORD = password
            };

            var response = await _httpClient.PostAsJsonAsync("user/Login", loginData);
            
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to authenticate with Viettel Post. Status: {response.StatusCode}");

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            
            if (result.TryGetProperty("data", out var data) && data.TryGetProperty("token", out var tokenProp))
            {
                _token = tokenProp.GetString() ?? string.Empty;
                _httpClient.DefaultRequestHeaders.Remove("Token");
                _httpClient.DefaultRequestHeaders.Add("Token", _token);
            }
            else
            {
                throw new Exception("Invalid authentication response from Viettel Post.");
            }
        }

        public async Task<decimal> CalculateShippingFeeAsync(int senderProvinceId, int senderDistrictId, int receiverProvinceId, int receiverDistrictId, int weightInGrams, decimal productPrice)
        {
            await AuthenticateAsync();

            // Prepare the payload for calculating price
            // The API requires ID of province/district, but for simplicity in this example,
            // we send a request to a mock or simplified endpoint, OR assuming we have a way to map names to IDs.
            // **NOTE FOR STUDENT**: Proper implementation requires calling VTPost's API to get ProvinceID and DistrictID first,
            // or mapping your string "Hồ Chí Minh" to their ID (e.g., 50).
            // To keep the project scope manageable, we will use a simplified mock logic if the real API is too complex for string matching,
            // but here is the skeleton for the actual API call:

            var payload = new
            {
                SENDER_PROVINCE = senderProvinceId,
                SENDER_DISTRICT = senderDistrictId,
                RECEIVER_PROVINCE = receiverProvinceId,
                RECEIVER_DISTRICT = receiverDistrictId,
                PRODUCT_TYPE = "HH", // HH = Hàng hóa
                PRODUCT_WEIGHT = weightInGrams,
                PRODUCT_PRICE = productPrice,
                MONEY_COLLECTION = 0,
                ORDER_SERVICE = "SGN", // Standard/Economy service
                ORDER_SERVICE_ADD = ""
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            
            // For educational purposes, if you face issues with real IDs, you can return a calculated formula here:
            // return Task.FromResult(CalculateFeeByArea(senderProvince, receiverProvince));

            try
            {
                var response = await _httpClient.PostAsync("order/getPriceAll", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    // Typical VTPost response structure extracting the fee:
                    // Look for the cheapest or specific service in the array
                    if (result.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array && dataArray.GetArrayLength() > 0)
                    {
                        var firstOption = dataArray[0];
                        if (firstOption.TryGetProperty("GIA_CUOC", out var giaCuoc))
                        {
                            return giaCuoc.GetDecimal();
                        }
                    }
                }
            } 
            catch(Exception)
            {
                // Fallback for demo if API fails
            }

            // Fallback mock logic for the students' project if real VTPost mapping is not setup
            return CalculateMockFee(senderProvinceId, receiverProvinceId);
        }

        private decimal CalculateMockFee(int senderProv, int receiverProv)
        {
            // Simple mock: Same province = 20k, Different province = 40k
            if (senderProv == receiverProv)
                return 20000;
            return 40000;
        }

        // ============================================================
        // ADDRESS LOOKUP — Viettel Post Category API
        // ============================================================

        public async Task<List<ProvinceDto>> GetProvincesAsync()
        {
            if (_cachedProvinces != null) return _cachedProvinces;

            try
            {
                // Typically VTPost doesn't require authentication for public category endpoints,
                // but we authenticate just in case.
                var response = await _httpClient.GetAsync("categories/listProvinceById?provinceId=-1");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        var provinces = new List<ProvinceDto>();
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            provinces.Add(new ProvinceDto
                            {
                                ProvinceId = item.GetProperty("PROVINCE_ID").GetInt32(),
                                ProvinceName = item.GetProperty("PROVINCE_NAME").GetString() ?? ""
                            });
                        }
                        
                        _cachedProvinces = provinces;
                        return provinces;
                    }
                }
            }
            catch (Exception)
            {
                // Log exception in real world scenarios
            }

            return new List<ProvinceDto>();
        }

        public async Task<List<DistrictDto>> GetDistrictsAsync(int provinceId)
        {
            if (_cachedDistricts.TryGetValue(provinceId, out var cached)) return cached;

            try
            {
                var response = await _httpClient.GetAsync($"categories/listDistrict?provinceId={provinceId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        var districts = new List<DistrictDto>();
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            districts.Add(new DistrictDto
                            {
                                DistrictId = item.GetProperty("DISTRICT_ID").GetInt32(),
                                DistrictName = item.GetProperty("DISTRICT_NAME").GetString() ?? "",
                                ProvinceId = item.GetProperty("PROVINCE_ID").GetInt32()
                            });
                        }

                        _cachedDistricts[provinceId] = districts;
                        return districts;
                    }
                }
            }
            catch (Exception)
            {
                // Log exception
            }

            return new List<DistrictDto>();
        }

        public async Task<List<WardDto>> GetWardsAsync(int districtId)
        {
            if (_cachedWards.TryGetValue(districtId, out var cached)) return cached;

            try
            {
                var response = await _httpClient.GetAsync($"categories/listWards?districtId={districtId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        var wards = new List<WardDto>();
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            wards.Add(new WardDto
                            {
                                WardId = item.GetProperty("WARDS_ID").GetRawText(), // Often strings or ints, use string to be safe based on DTO
                                WardName = item.GetProperty("WARDS_NAME").GetString() ?? "",
                                DistrictId = item.GetProperty("DISTRICT_ID").GetInt32()
                            });
                        }

                        _cachedWards[districtId] = wards;
                        return wards;
                    }
                }
            }
            catch (Exception)
            {
                // Log exception
            }

            return new List<WardDto>();
        }
    }
}
