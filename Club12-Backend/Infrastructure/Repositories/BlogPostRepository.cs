using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Represents the repository implementation for <see cref="BlogPost"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{BlogPost}"/> and implements <see cref="IBlogPostRepository"/>.
/// </summary>
/// <param name="context">The application's database context used for data access operations.</param>
public class BlogPostRepository(ApplicationDBContext context)
    : GenericRepository<BlogPost>(context), IBlogPostRepository
{
}