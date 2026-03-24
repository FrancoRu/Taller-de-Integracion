using Application.Interfaces.Mappers;
using Domain.Entities.Models;
using Riok.Mapperly.Abstractions;

namespace Application.Utils.Mappers;

[Mapper]
public partial class TeamMapper : ITeamMapper
{
    public partial void ApplyUpdate(Team source, [MappingTarget] Team target);
}
