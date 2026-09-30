using AndreaTV.Api.Modules.Security.Models;

namespace AndreaTV.Api.Modules.Security.Services;

public sealed class AuthService : IAuthService
{
    public AuthStatus GetStatus() => new("Security module is ready.");
}
