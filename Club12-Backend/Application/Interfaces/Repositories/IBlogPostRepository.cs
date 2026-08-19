using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing BlogPost entities.
/// </summary>
public interface IBlogPostRepository : IGenericRepository<BlogPost>
{
}
