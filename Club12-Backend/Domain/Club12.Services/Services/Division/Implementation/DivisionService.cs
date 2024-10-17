using Club12.Entities.DivisionEntity;
using Club12.Services.DataAccessLayer.GenericEntity;

namespace Club12.Services.Services.DivisionService.Implementation;

public class DivisionService(IGenericService<Division> genericDivisionService) : IDivisionService
{
    public Division CreateDivision(Division divisionEntity)
    {
        genericDivisionService.Insert(divisionEntity);
        return divisionEntity;
    }

    public void DeleteDivision(Division divisionEntity)
    {
        genericDivisionService.Delete(divisionEntity);
    }

    public async Task<bool> UpdateDivision(Division divisionEntity)
    {
        try
        {
            await genericDivisionService.UpdateAsync(divisionEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Division? GetDivisionById(Guid divisionId)
    {
        return genericDivisionService.TryGet(divisionId);
    }
}
