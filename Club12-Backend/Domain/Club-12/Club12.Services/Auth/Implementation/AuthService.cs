using Club12.Services.Users;
using Microsoft.AspNetCore.Http;

namespace Club12.Services.Auth.Implementation;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IUserService userService,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsUserAuthorized()
    {
        string jwtToken = GetJwtToken();
        return _userService.IsAuthenticated(jwtToken);
    }

    public bool IsSuperAdmin()
    {
        string jwtToken = GetJwtToken();
        return _userService.IsSuperAdmin(jwtToken);
    }

    private string GetJwtToken()
    {
        string jwtToken = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
        return jwtToken.Replace("Bearer ", "");
    }
}
