using Entities.Models.Venues;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;

namespace Services.Services.Venues.Implementation;

public class VenueService(IGenericService<Venue> _genericVenueService) : IVenueService
{
    public async Task<Venue> CreateVenueAsync(Venue venueEntity)
    {
        await _genericVenueService.InsertAsync(venueEntity);
        return venueEntity;
    }

    public async Task<Venue?> GetVenueByIdAsync(Guid venueId) => await _genericVenueService.FilterByExpression(venue => venue.Id == venueId).FirstOrDefaultAsync();

    public async Task<bool> DeleteVenueAsync(Venue venueEntity)
    {
        try
        {
            await _genericVenueService.DeleteAsync(venueEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateVenueAsync(Venue venueEntity)
    {
        try
        {
            await _genericVenueService.UpdateAsync(venueEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Venue>> GetAllVenuesAsync() => await _genericVenueService.FindAllAsync();
}
