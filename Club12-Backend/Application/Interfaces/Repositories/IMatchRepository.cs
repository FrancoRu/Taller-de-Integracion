using Domain.Entities.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing Match entities.
/// </summary>
public interface IMatchRepository : IGenericRepository<Match>
{
}
