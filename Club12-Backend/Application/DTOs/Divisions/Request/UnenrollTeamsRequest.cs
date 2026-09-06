using System;
using System.Collections.Generic;

namespace Application.DTOs.Divisions.Request;

public class UnenrollTeamsRequest
{
    public List<Guid> TeamIds { get; set; } = [];
}
