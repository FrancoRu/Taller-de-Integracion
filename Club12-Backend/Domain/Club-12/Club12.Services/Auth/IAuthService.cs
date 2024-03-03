namespace Club12.Services.Auth;

public interface IAuthService
{
    public bool IsUserAuthorized();

    public bool IsSuperAdmin();
}
