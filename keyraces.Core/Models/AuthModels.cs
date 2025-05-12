namespace keyraces.Core.Models
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime Expiration { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
    }

    public class TokenRequest
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
