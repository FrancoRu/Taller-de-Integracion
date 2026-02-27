using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class StaffRepository(ApplicationDBContext context) 
    : GenericRepository<Staff>(context), IStaffRepository
{
}
