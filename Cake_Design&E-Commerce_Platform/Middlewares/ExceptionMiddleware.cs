using Application.Exceptions;
using System.Text.Json;

namespace Cake_Design_E_Commerce_Platform.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Cho phép Request đi tiếp vào Controller / Service
                await _next(context);
            }
            catch (Exception ex)
            {
                // Nếu có bất kỳ lỗi nào bị "throw" ra, nó sẽ rơi vào đây
                _logger.LogError(ex, ex.Message); // Ghi log ra console hoặc file
                await HandleExceptionAsync(context, ex); // Xử lý trả về cho Frontend
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Map từng loại Exception sang mã HTTP tương ứng
            context.Response.StatusCode = exception switch
            {
                BadRequestException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                TooManyRequestsException => StatusCodes.Status429TooManyRequests, // Dùng cho chống Spam OTP
                _ => StatusCodes.Status500InternalServerError
            };
            // Tạo format JSON chuẩn để trả về cho Frontend dễ đọc
            var response = new
            {
                statusCode = context.Response.StatusCode,
                // Nếu là lỗi 500 thì giấu chi tiết để bảo mật, các lỗi khác thì in thẳng câu thông báo ra
                message = context.Response.StatusCode == 500 ? "Lỗi hệ thống nội bộ." : exception.Message
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(json);
        }
    }
}
