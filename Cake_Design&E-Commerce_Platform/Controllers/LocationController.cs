using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [Route("api/locations")]
    [ApiController]
    [AllowAnonymous] // Location data should be accessible without login for registration/cart pages
    public class LocationController : ControllerBase
    {
        private readonly IViettelPostService _viettelPost;

        public LocationController(IViettelPostService viettelPost)
        {
            _viettelPost = viettelPost;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var provinces = await _viettelPost.GetProvincesAsync();
            return Ok(provinces);
        }

        [HttpGet("provinces/{provinceId}/districts")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _viettelPost.GetDistrictsAsync(provinceId);
            return Ok(districts);
        }

        [HttpGet("districts/{districtId}/wards")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _viettelPost.GetWardsAsync(districtId);
            return Ok(wards);
        }
    }
}
