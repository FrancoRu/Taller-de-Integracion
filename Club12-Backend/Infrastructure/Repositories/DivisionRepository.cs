using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Division"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Division}"/> and implements <see cref="IDivisionRepository"/>.
/// </summary>
/// <remarks>
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// This repository is intended for use within the infrastructure layer to encapsulate
/// data access logic specific to Division entities.
/// </remarks>
public class DivisionRepository(ApplicationDBContext context)
    : GenericRepository<Division>(context), IDivisionRepository
{
}