using System;
using System.Collections.Generic;

namespace Application.DTOs.Stage.Request;

public class UnassignmentTeamRequest
{
    public List<Guid> TeamIds { get; set; } = [];
}
