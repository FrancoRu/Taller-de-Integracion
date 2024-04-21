using Club12.Entities.DivisionEntity;
using Club12.Services.DataAccessLayer.GenericEntity;

namespace Club12.Services.Divisions.Implementation;

public class DivisionService : IDivisionService
{
    private readonly IGenericService<Division> _genericDivisionService;

    public DivisionService(IGenericService<Division> genericDivisionService)
    {
        _genericDivisionService = genericDivisionService;
    }

    public Division CreateDivision(Division divisionEntity, Guid userId)
    {
        _genericDivisionService.Insert(divisionEntity, userId);
        return divisionEntity;
    }

    public void DeleteDivision(Division divisionEntity)
    {
        _genericDivisionService.Delete(divisionEntity);
    }

    public async Task<bool> UpdateDivision(Division divisionEntity, Guid userId)
    {
        try
        {
            await _genericDivisionService.UpdateAsync(divisionEntity, userId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Division? GetDivisionById(Guid divisionId)
    {
        return _genericDivisionService.TryGet(divisionId);
    }
}
