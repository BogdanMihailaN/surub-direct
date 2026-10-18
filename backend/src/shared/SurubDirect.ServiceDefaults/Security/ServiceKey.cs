using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SurubDirect.ServiceDefaults.Security;
public static class ServiceKey
{
    public static string Required(IConfiguration configuration, string name)
    {
        var key = configuration[name];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException($"Configure a service credential of at least 32 characters: {name}");
        return key;
    }

    public static bool Matches(HttpRequest request, string expected)
    {
        var supplied = request.Headers["X-Service-Key"].ToString();
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
    }
}
