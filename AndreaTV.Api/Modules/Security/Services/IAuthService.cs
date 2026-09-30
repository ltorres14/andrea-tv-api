using AndreaTV.Api.Modules.Security.Models;

namespace AndreaTV.Api.Modules.Security.Services;

public interface IAuthService
{
    AuthStatus GetStatus();
}
