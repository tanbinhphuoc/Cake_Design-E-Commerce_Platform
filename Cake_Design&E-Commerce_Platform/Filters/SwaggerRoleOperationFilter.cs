using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cake_Design_E_Commerce_Platform.Filters
{
    public class SwaggerRoleOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Lấy ra tất cả các [Authorize] attributes có Roles trên Method hoặc Controller
            var authorizeAttributes = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(true)
                .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(true) ?? Enumerable.Empty<AuthorizeAttribute>())
                .ToList();

            if (authorizeAttributes.Any())
            {
                var roles = authorizeAttributes
                    .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                    .Select(a => a.Roles)
                    .Distinct()
                    .ToList();

                if (roles.Any())
                {
                    // Thêm chú thích vào summary hoặc description của endpoint
                    var roleText = string.Join(", ", roles);
                    operation.Description += $"\n\n**🔒 Yêu cầu Role:** {roleText}";
                }
            }
        }
    }
}
