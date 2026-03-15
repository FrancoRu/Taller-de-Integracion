using System;

namespace Domain.Entities.Models;

public class StageTeamMatch : EntityBase
{
    public required Guid StageId { get; set; }
    public Stage? Stage { get; set; }
    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }
}