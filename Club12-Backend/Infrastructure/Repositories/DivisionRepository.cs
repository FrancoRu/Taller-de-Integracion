using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class DivisionRepository(ApplicationDBContext context) 
    : GenericRepository<Division>(context), IDivisionRepository
{
}
