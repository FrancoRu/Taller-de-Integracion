using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services;

public class VenueService(IVenueRepository venueRepository) : IVenueService
{
    public async Task<Venue> CreateVenueAsync(Venue venueEntity)
    {
        await venueRepository.AddAsync(venueEntity);
        return venueEntity;
    }

    public async Task<Venue?> GetVenueByIdAsync(Guid venueId)
        => await venueRepository.GetByIdAsync(venueId);

    public async Task DeleteVenueAsync(Guid id)
    {
        await venueRepository.RemoveAsync(venue => venue.Id == id);
    }

    public async Task UpdateVenueAsync(Venue venueEntity)
        => await venueRepository.UpdateAsync(venueEntity);

    public async Task<IEnumerable<Venue>> GetAllVenuesAsync() => await venueRepository.FindAsync(venue => true);
}
