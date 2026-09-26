using LithographERP.Domain.Modules.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LithographERP.Infrastructure.Modules.Authentication;

public sealed class PasswordHashing
{
    private readonly PasswordHasher<User> _hasher = new();
    private readonly string _dummyHash;

    public PasswordHashing()
    {
        _dummyHash = _hasher.HashPassword(new User(), "not-a-user-password");
    }

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string password) =>
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;

    public void VerifyDummy(string password) =>
        _hasher.VerifyHashedPassword(new User(), _dummyHash, password);
}
