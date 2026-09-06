using System;
using System.Collections.Generic;

namespace Application.DTOs.Divisions.Request;

public class EnrollTeamsRequest
{
    public List<Guid> TeamIds { get; set; } = [];
}
