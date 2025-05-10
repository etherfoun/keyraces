using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace keyraces.Infrastructure.Security
{
    public class AspNetPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<object> hasher = new();
        public string Hash(string plainText) => hasher.HashPassword(null!, plainText);

        public bool Verify(string plainText, string hashedText)
        {
            var result = hasher.VerifyHashedPassword(null!, hashedText, plainText);
            return result == PasswordVerificationResult.Success;
        }
    }
}
