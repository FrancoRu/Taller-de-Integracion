using Club12.Entities.UserEntity;
using Club12.Services.DataAccessLayer;
using Club12.Services.Utils;

namespace Club12.Services.Users.Implementation;

public class UserService : IUserService
{
    private readonly IGenericService<User> _userGenericService;

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
