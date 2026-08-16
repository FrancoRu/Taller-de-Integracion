using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Represents the repository implementation for BlogPost entities.
/// Inherits generic CRUD operations from GenericRepository{BlogPost} and implements IBlogPostRepository.
/// </summary>
/// <param name="context">The application's database context used for data access operations.</param>
public class BlogPostRepository(ApplicationDBContext context)
    : GenericRepository<BlogPost>(context), IBlogPostRepository
{
}