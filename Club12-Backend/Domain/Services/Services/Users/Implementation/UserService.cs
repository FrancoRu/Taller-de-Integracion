using Entities.Models.Users;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;
using Services.Utils;

namespace Services.Services.Users.Implementation;

public class UserService(IGenericService<User> _genericUserService) : IUserService
{
    public async Task<bool> ValidateCredentialsAsync(User userEntity, string plainTextPassword) => await Task.Run(() => Encrypt.CheckHash(plainTextPassword, userEntity.Password));

    public async Task<User?> GetUserByUserNameAsync(string userName) => await _genericUserService.FilterByExpression(user => user.Username.Equals(userName)).FirstOrDefaultAsync();

    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken) => await _genericUserService
            .FilterByExpression(user => user.RefreshToken != null && user.RefreshToken.Equals(refreshToken))
            .FirstOrDefaultAsync();

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            await _genericUserService.UpdateAsync(user);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
