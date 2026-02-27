namespace Application.DTOs.Stage.Request;

public class AssignamentTeamRequest: UnassignamentTeamRequest
{
    public bool Auto { get; set; } = false;
}
