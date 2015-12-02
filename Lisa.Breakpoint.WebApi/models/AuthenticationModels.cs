using System;
using System.IdentityModel.Tokens;

namespace Lisa.Breakpoint.TokenAuthentication
{
    public class TokenAuthOptions
    {
        public string Issuer { get; set; }
        public SigningCredentials SigningCredentials { get; set; }
    }

    public class AuthRequest
    {
        public string username { get; set; }
    }

    public class TokenResponse
    {
        public string user { get; set; }
        public string token { get; set; }
        public DateTime? tokenExpires { get; set; }
    }
}
