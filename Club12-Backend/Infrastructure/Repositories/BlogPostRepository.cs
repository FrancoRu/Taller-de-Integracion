using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BlogPost entities, inheriting generic CRUD and implementing IBlogPostRepository.
/// </summary>
/// <param name="context">The application's database context used for data access operations.</param>
public class BlogPostRepository(ApplicationDBContext context)
    : GenericRepository<BlogPost>(context), IBlogPostRepository
{
}