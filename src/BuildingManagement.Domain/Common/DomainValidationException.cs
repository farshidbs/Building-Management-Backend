using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public sealed class DomainValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
