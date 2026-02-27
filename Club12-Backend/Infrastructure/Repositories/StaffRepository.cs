using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Staff"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Staff}"/> and implements <see cref="IStaffRepository"/>.
/// </summary>
/// <remarks>
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// </remarks>
public class StaffRepository(ApplicationDBContext context) 
    : GenericRepository<Staff>(context), IStaffRepository
{
}
