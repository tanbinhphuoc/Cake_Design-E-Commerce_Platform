using Application.DTOs;
using Domain.Entities;

namespace Application.Services
{
    public interface IJwtTokenGenerator
    {
        JwtTokenResult GenerateToken(Account user);
    }
}
