using Club12.Entities.DivisionEntity;
using Club12.Services.DataAccessLayer;

namespace Club12.Services.Divisions.Implementation;

public class DivisionService : IDivisionService
{
    private readonly IGenericService<Division> _genericDivisionService;

    public DivisionService(
        IGenericService<Division> genericDivisionService
    )
    {
        _genericDivisionService = genericDivisionService;
    }

    public Division CreateDivision(Division divisionEntity)
    {
        _genericDivisionService.Insert(divisionEntity);
        return divisionEntity;
    }

    public Division? GetDivisionById(Guid divisionId)
    {
        return _genericDivisionService.TryGet(divisionId);
    }
}
