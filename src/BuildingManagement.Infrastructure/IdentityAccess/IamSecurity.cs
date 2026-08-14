using System.Security.Cryptography;
using System.Text;
using BuildingManagement.Application;

namespace BuildingManagement.Infrastructure;

public sealed class HmacIamSecretProtector : IIamSecretProtector
{
    private readonly byte[] key;
    public HmacIamSecretProtector(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("Iam:Secret must contain at least 32 characters.");
        key = Encoding.UTF8.GetBytes(secret);
    }
    public string Hash(string value) => Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)));
    public bool Verify(string value, string hash) => CryptographicOperations.FixedTimeEquals(
        Convert.FromHexString(Hash(value)), Convert.FromHexString(hash));
    public string CreateToken(int bytes = 32) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    public string CreateOtp() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class UnconfiguredOtpDelivery : IOtpDelivery
{
    public Task Send(string normalizedMobile, string code, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No OTP provider is configured. Configure an IOtpDelivery implementation; plaintext OTP values are never logged.");
}
