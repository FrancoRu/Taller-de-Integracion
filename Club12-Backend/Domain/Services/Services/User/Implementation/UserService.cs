using Entities.Models.UserEntity;
using Services.DataAccessLayer.GenericEntity;
using Services.Utils;

namespace Services.Services.UserService.Implementation;

public class UserService(IGenericService<User> genericUserService) : IUserService
{
    public bool ValidateCredentials(User userEntity, string plainTextPassword)
    {
        return Encrypt.CheckHash(plainTextPassword, userEntity.Password);
    }

    public User? GetUserByUserNameAsync(string userName)
    {
        return genericUserService.FilterByExpression(user => user.Username.Equals(userName)).FirstOrDefault();
    }
}
