using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing Player entities.
/// </summary>
public interface IPlayerRepository : IGenericRepository<Player>
{
}
