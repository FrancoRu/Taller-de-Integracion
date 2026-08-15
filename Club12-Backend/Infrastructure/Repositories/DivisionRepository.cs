using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Division entities.
/// Inherits generic CRUD operations from GenericRepository{Division} and implements IDivisionRepository.
/// </summary>
/// <remarks>
/// Utilizes ApplicationDBContext for data access.
/// This repository is intended for use within the infrastructure layer to encapsulate
/// data access logic specific to Division entities.
/// </remarks>
public class DivisionRepository(ApplicationDBContext context)
    : GenericRepository<Division>(context), IDivisionRepository
{
}