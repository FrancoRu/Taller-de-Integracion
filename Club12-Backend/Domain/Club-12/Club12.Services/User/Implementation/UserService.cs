using Club12.Entities.UserEntity;
using Club12.Services.DataAccessLayer.GenericUser;
using Club12.Services.Utils;

namespace Club12.Services.Users.Implementation;

public class UserService : IUserService
{
    private readonly IGenericUserService _genericUserService;

    public UserService(IGenericUserService genericUserService)
    {
        _genericUserService = genericUserService;
    }

    public User CreateUser(User userEntity)
    {
        _genericUserService.Insert(userEntity);
        return userEntity;
    }

    public void DeleteUser(User userEntity)
    {
        _genericUserService.Delete(userEntity);
    }

    public User? GetUserById(Guid userId)
    {
        return _genericUserService.TryGet(userId);
    }

    public async Task<bool> UpdateUser(User userEntity)
    {
        try
        {
            await _genericUserService.UpdateAsync(userEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateCredentials(User userEntity, string plainTextPassword)
    {
        bool isValid = Encrypt.CheckHash(plainTextPassword, userEntity.Password);
        return isValid;
    }

    public User? GetUserByUserName(string userName)
    {
        return _genericUserService.FilterByExpression(user => user.UserName.Equals(userName)).FirstOrDefault();
    }
}
