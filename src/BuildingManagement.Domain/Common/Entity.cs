using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public abstract class Entity
{
    public long Id { get; protected set; }
    public string Code { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];

    protected void Initialize(string code, DateTimeOffset now)
    {
        Code = PublicCode.Normalize(code);
        CreatedAtUtc = now;
    }

    public void SetActivation(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    protected void Touch(DateTimeOffset now) => UpdatedAtUtc = now;
    protected static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException(name, "Must not be blank.") : value.Trim();
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    protected static void Coordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude is < -90 or > 90) throw new DomainValidationException("latitude", "Must be between -90 and 90.");
        if (longitude is < -180 or > 180) throw new DomainValidationException("longitude", "Must be between -180 and 180.");
    }
}
