namespace BuildingManagement.Application;

public static class IranianMobileNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw Validation("mobile", "Mobile is required.");
        var digits = new string(value.Trim().Select(ToAsciiDigit).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0098", StringComparison.Ordinal)) digits = digits[4..];
        else if (digits.StartsWith("98", StringComparison.Ordinal)) digits = digits[2..];
        if (digits.StartsWith('0')) digits = digits[1..];
        if (digits.Length != 10 || digits[0] != '9') throw Validation("mobile", "Iranian mobile format is invalid.");
        return "+98" + digits;
    }

    private static char ToAsciiDigit(char value) => value switch
    {
        >= '\u06F0' and <= '\u06F9' => (char)('0' + value - '\u06F0'),
        >= '\u0660' and <= '\u0669' => (char)('0' + value - '\u0660'),
        _ => value
    };

    private static AppException Validation(string field, string message) =>
        new(400, "validation.failed", "Request validation failed.",
            new Dictionary<string, string[]> { [field] = [message] });
}
