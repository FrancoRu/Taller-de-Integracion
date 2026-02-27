using Application.Interfaces.Services;
using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Application.Services;

public class UserService(User _genericUserService)
{
    //public async Task<bool> ValidateCredentialsAsync(User userEntity, string plainTextPassword) => await Task.Run(() => Encrypt.CheckHash(plainTextPassword, userEntity.Password));

    //public async Task<User?> GetUserByUserNameAsync(string userName) => await _genericUserService.FilterByExpression(user => user.Username.Equals(userName)).FirstOrDefaultAsync();

    //public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken) => await _genericUserService
    //        .FilterByExpression(user => user.RefreshToken != null && user.RefreshToken.Equals(refreshToken))
    //        .FirstOrDefaultAsync();

    //public async Task<bool> UpdateUserAsync(User user)
    //{
    //    try
    //    {
    //        await _genericUserService.UpdateAsync(user);
    //        return true;
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}
}
