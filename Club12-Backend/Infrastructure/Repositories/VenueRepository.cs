using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class VenueRepository(ApplicationDBContext context) 
    : GenericRepository<Venue>(context), IVenueRepository {}
