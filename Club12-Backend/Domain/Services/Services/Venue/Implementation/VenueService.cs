using Entities.Models.VenueEntity;

using Services.DataAccessLayer.GenericEntity;

namespace Services.Services.VenueService.Implementation;

public class VenueService(IGenericService<Venue> genericVenueService) : IVenueService
{
    public Venue CreateVenue(Venue venueEntity)
    {
        genericVenueService.Insert(venueEntity);
        return venueEntity;
    }

    public Venue? GetVenueById(Guid venueId)
    {
        return genericVenueService.FilterByExpression(venue => venue.Id == venueId).FirstOrDefault();
    }

    public void DeleteVenue(Venue venueEntity)
    {
        genericVenueService.Delete(venueEntity);
    }

    public async Task<bool> UpdateVenueAsync(Venue venueEntity)
    {
        try
        {
            await genericVenueService.UpdateAsync(venueEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Venue>> GetAllVenuesAsync()
    {
        return await genericVenueService.FindAllAsync();
    }
}
