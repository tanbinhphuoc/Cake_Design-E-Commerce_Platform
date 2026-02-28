using Application.DTOs;

namespace Application.Services
{
    public interface IViettelPostService
    {
        /// <summary>
        /// Calculates the shipping fee based on Viettel Post API.
        /// </summary>
        /// <param name="senderProvinceId">Sender's province ID</param>
        /// <param name="senderDistrictId">Sender's district ID</param>
        /// <param name="receiverProvinceId">Receiver's province ID</param>
        /// <param name="receiverDistrictId">Receiver's district ID</param>
        /// <param name="weightInGrams">Weight of the product in grams (e.g. 1000 for 1kg)</param>
        /// <param name="productPrice">The value of the product for insurance purposes</param>
        /// <returns>The calculated shipping fee in VND</returns>
        Task<decimal> CalculateShippingFeeAsync(int senderProvinceId, int senderDistrictId, int receiverProvinceId, int receiverDistrictId, int weightInGrams, decimal productPrice);

        Task<List<ProvinceDto>> GetProvincesAsync();
        Task<List<DistrictDto>> GetDistrictsAsync(int provinceId);
        Task<List<WardDto>> GetWardsAsync(int districtId);
    }
}
