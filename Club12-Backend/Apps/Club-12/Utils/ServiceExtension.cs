using Club12.Entities.DivisionEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Entities.UserEntity;
using Club12.Services.DataAccessLayer;
using Club12.Services.DataAccessLayer.Implementation;
using Club12.Services.Divisions;
using Club12.Services.Divisions.Implementation;
using Club12.Services.Players;
using Club12.Services.Players.Implementation;
using Club12.Services.Teams;
using Club12.Services.Teams.Implementation;
using Club12.Services.Users;
using Club12.Services.Users.Implementation;
using Club12.Services.Utils;
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
        collection.AddScoped<IGenericService<User>, GenericService<User>>();
        EnsureAdminUserExists(collection.BuildServiceProvider());
    }

    private static void EnsureAdminUserExists(IServiceProvider serviceProvider)
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
                PasswordHashed = Encrypt.Hash("root"),
                Role = "SuperAdmin",
                DateCreated = DateTime.UtcNow,
            };

            dbContext.Users.Add(adminEntity);
            dbContext.SaveChanges();
        }
    }
}
