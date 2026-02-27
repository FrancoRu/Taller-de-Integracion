using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class BlogPostRepository(ApplicationDBContext context) 
    : GenericRepository<BlogPost>(context), IBlogPostRepository
{
}
