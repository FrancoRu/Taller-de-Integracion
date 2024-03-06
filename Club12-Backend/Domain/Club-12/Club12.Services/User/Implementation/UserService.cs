using Club12.Entities.UserEntity;
using Club12.Services.DataAccessLayer;
using Club12.Services.Utils;
using Microsoft.IdentityModel.Tokens;

namespace Club12.Services.Users.Implementation;

public class UserService : IUserService
{
    private readonly IGenericService<User> _userGenericService;
    private readonly string _jwtSecret = Environment.GetEnvironmentVariable("JWT_TOKEN_KEY") ?? throw new InvalidOperationException("La variable de entorno JWT_TOKEN_KEY no está configurada.");

    public UserService(
        IGenericService<User> userGenericService
    )
    {
        _userGenericService = userGenericService;
    }

    public User CreateUser(User userEntity)
    {
        _userGenericService.Insert(userEntity);
        return userEntity;
    }

    public void DeleteUser(User userEntity)
    {
        _userGenericService.Delete(userEntity);
    }

    public string GenerateJwtToken(User userEntity)
    {
        return Encrypt.GenerateJWTToken(_jwtSecret, userEntity.UserName, userEntity.Role, userEntity.Id);
    }

    public User? GetUserById(Guid userId)
    {
        return _userGenericService.TryGet(userId);
    }

    public async Task<bool> UpdateUser(User userEntity)
    {
        try
        {
            await _userGenericService.UpdateAsync(userEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsSuperAdmin(string jwtToken)
    {
        Encrypt.UserClaims? userClaims = Encrypt.DecodeJWTToken(jwtToken, _jwtSecret);
        return userClaims?.Role == "SuperAdmin";
    }

    public bool IsAuthenticated(string jwtToken)
    {
        Encrypt.UserClaims? userClaims = Encrypt.DecodeJWTToken(jwtToken, _jwtSecret);
        return userClaims?.Role == "SuperAdmin" || userClaims?.Role == "Admin";
    }

    public bool ValidateToken(string jwtToken)
    {
        Encrypt.UserClaims? userClaims = Encrypt.DecodeJWTToken(jwtToken, _jwtSecret);
        
        if (userClaims is null || string.IsNullOrEmpty(userClaims.Id.ToString()))
        {
            return false;
        }

        var user = GetUserById(userClaims.Id);

        return user != null;
    }

    public bool ValidateCredentials(User userEntity, string plainTextPassword)
    {
        bool isValid = Encrypt.CheckHash(plainTextPassword, userEntity.PasswordHashed);
        return isValid;
    }

    public User? GetUserByUserName(string userName)
    {
        return _userGenericService.FilterByExpression(user => user.UserName.Equals(userName)).FirstOrDefault();
    }
}
