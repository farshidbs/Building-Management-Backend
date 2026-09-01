using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.AspNetCore.Identity;

namespace BuildingManagement.Api;

public sealed class AspNetCorePlatformPasswordHasher : IPlatformPasswordHasher
{
    private readonly PasswordHasher<PlatformUser> hasher = new();
    public string Hash(PlatformUser user, string password) => hasher.HashPassword(user, password);
    public bool Verify(PlatformUser user, string passwordHash, string password) =>
        hasher.VerifyHashedPassword(user, passwordHash, password) != PasswordVerificationResult.Failed;
}
