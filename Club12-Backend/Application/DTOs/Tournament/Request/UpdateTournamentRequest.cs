using Domain.Enums;

namespace Application.DTOs.Tournament.Request;

public class UpdateTournamentRequest: CreateTournamentRequest
{
    public TournamentStatus? Status { get; set; }
}
