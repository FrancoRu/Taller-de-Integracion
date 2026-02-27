using System;
using System.Collections.Generic;

namespace Application.DTOs.Stage.Request;

public class UnassignamentTeamRequest
{
    public List<Guid> TeamIds { get; set; } = [];
}
