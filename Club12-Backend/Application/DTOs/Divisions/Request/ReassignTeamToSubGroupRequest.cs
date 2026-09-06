using System;

namespace Application.DTOs.Divisions.Request;

public class ReassignTeamToSubGroupRequest
{
    public required Guid TeamId { get; set; }

    public required Guid FromStageId { get; set; }

    public required Guid ToStageId { get; set; }
}
