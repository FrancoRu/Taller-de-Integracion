using Club12.Entities.UserEntity;
using Club12.Services.DataAccessLayer.GenericEntity;
using Club12.Services.Utils;

namespace Club12.Services.Users.Implementation;

public class UserService(IGenericService<User> genericUserService) : IUserService
{
    public bool ValidateCredentials(User userEntity, string plainTextPassword)
    {
        return Encrypt.CheckHash(plainTextPassword, userEntity.Password);
    }

    public User? GetUserByUserName(string userName)
    {
        return genericUserService.FilterByExpression(user => user.Username.Equals(userName)).FirstOrDefault();
    }
}
