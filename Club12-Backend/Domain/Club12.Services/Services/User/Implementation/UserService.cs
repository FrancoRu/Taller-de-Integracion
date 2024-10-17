using Club12.Entities.UserEntity;
using Club12.Services.DataAccessLayer.GenericEntity;
using Club12.Services.Utils;

namespace Club12.Services.Services.UserService.Implementation;

public class UserService(IGenericService<User> genericUserService) : IUserService
{
    public bool ValidateCredentials(User userEntity, string plainTextPassword)
    {
        return Encrypt.CheckHash(plainTextPassword, userEntity.Password);
    }

    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        return await genericUserService.FilterByExpression(user => user.Username.Equals(userName));
    }
}
