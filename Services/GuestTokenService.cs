using System.Security.Cryptography;
using System.Text;

namespace MyTasks.Services
{
    public class GuestTokenService : IGuestTokenService
    {
        public string Generate()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public string Hash(string rawToken)
        {
            return Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        }
    }
}