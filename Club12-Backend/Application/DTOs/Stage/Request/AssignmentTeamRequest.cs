namespace Application.DTOs.Stage.Request;

public class AssignmentTeamRequest : UnassignmentTeamRequest
{
    public bool Auto { get; set; } = false;
}
