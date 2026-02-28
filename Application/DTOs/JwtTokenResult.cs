using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class JwtTokenResult
    {
        public string Token { get; set; } = string.Empty;
        public string Jti { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
