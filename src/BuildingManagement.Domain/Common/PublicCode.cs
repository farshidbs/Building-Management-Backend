using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public static class PublicCode
{
    public const int Length = 5;
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Create()
    {
        Span<char> code = stackalloc char[Length];
        for (var index = 0; index < code.Length; index++)
            code[index] = Characters[RandomNumberGenerator.GetInt32(Characters.Length)];
        return new string(code);
    }

    public static string Normalize(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();
        if (normalized.Length != Length || normalized.Any(character => !Characters.Contains(character)))
            throw new DomainValidationException("code", "Code must contain exactly five uppercase English letters or digits.");
        return normalized;
    }
}
