using Domain.Entities;

namespace Application.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Account user);
    }
}
