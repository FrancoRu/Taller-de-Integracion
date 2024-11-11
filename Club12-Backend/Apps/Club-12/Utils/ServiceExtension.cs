using Club12.Entities.DivisionEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Entities.UserEntity;
using Club12.Services.Auth;
using Club12.Services.Auth.Implementation;
using Club12.Services.DataAccessLayer.GenericEntity;
using Club12.Services.DataAccessLayer.GenericEntity.Implementation;
using Club12.Services.DataAccessLayer.GenericUser;
using Club12.Services.DataAccessLayer.GenericUser.Implementation;
using Club12.Services.Divisions;
using Club12.Services.Divisions.Implementation;
using Club12.Services.Players;
using Club12.Services.Players.Implementation;
using Club12.Services.Teams;
using Club12.Services.Teams.Implementation;
using Club12.Services.Users;
using Club12.Services.Users.Implementation;
using Club12.Services.Utils;
using Club12.Utils.Controller;
using Club12.Utils.Controller.Implementation;

using Persistence;

namespace Club12.Utils;

/// <summary>
/// Extension methods for registering application services in the dependency injection container.
/// </summary>
public static class ServiceExtension
{
    /// <summary>
    /// Registers application services in the dependency injection container.
    /// </summary>
    /// <param name="collection">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void RegisterApplicationServices(this IServiceCollection collection)
    {
        collection.AddScoped<IDivisionService, DivisionService>();
        collection.AddScoped<DivisionService>();
        collection.AddScoped<IGenericService<Division>, GenericService<Division>>();
        collection.AddScoped<ITeamService, TeamService>();
        collection.AddScoped<TeamService>();
        collection.AddScoped<IGenericService<Team>, GenericService<Team>>();
        collection.AddScoped<IPlayerService, PlayerService>();
        collection.AddScoped<PlayerService>();
        collection.AddScoped<IGenericService<Player>, GenericService<Player>>();
        collection.AddScoped<IUserService, UserService>();
        collection.AddScoped<UserService>();
        collection.AddScoped<IGenericUserService, GenericUserService>();
        collection.AddScoped<IAuthService, AuthService>();
        collection.AddScoped<AuthService>();
        collection.AddScoped<IHttpContextAccessor, HttpContextAccessor>();
        collection.AddScoped<IControllerUtils, ControllerUtils>();
        collection.AddScoped<ControllerUtils>();
    }

    /// <summary>
    /// Ensures that an admin user exists in the database. If no admin user is found, it creates one with default credentials.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance.</param>
    public static void EnsureAdminUserExists(this IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDBContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        User? adminUser = userService.GetUserByUserName("admin");

        if (adminUser is null)
        {
            User adminEntity = new()
            {
                UserName = "admin",
                Password = Encrypt.Hash("root"),
                Role = "SuperAdmin",
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
            };

            dbContext.Users.Add(adminEntity);
            dbContext.SaveChanges();
        }
    }
}
