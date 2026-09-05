using Application.DTOs.Match.Request;
using Application.DTOs.Match.Response;
using Application.DTOs.Player.Response;
using Application.DTOs.Scorer.Response;

using AutoMapper;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace API.AutoMapperProfiles;

public class MatchProfile : Profile
{
    public MatchProfile()
    {
        _ = CreateMap<CreateMatchRequest, Match>();

        _ = CreateMap<Match, DetailedMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.HomeTeam, opt => opt.MapFrom(src => src.HomeTeam))
            .ForMember(dest => dest.VisitorTeam, opt => opt.MapFrom(src => src.VisitorTeam))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null))
            .ForMember(dest => dest.WinningTeamId, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Id : (Guid?) null))
            // Score and Scorers are set here instead of via ForPath, since ForPath would force AutoMapper to instantiate HomeTeam even when src.HomeTeam is null, producing a fake team instead of an empty bracket slot.
            .AfterMap((src, dest) =>
            {
                // Each match's scorers are attributed to a team via the player's TeamId since Scorer itself has no TeamId, which requires the match to be loaded with Scorers.Player.
                Guid? tournamentId = src.Stage?.Division?.TournamentId;

                if (dest.HomeTeam is not null)
                {
                    dest.HomeTeam.Score = src.HomeScore ?? 0;
                    dest.HomeTeam.Scorers = ScorersForTeam(src, src.HomeTeamId);
                    PopulateRosterEligibility(dest.HomeTeam.Players, src.HomeTeam?.Players, tournamentId);
                }

                if (dest.VisitorTeam is not null)
                {
                    dest.VisitorTeam.Score = src.VisitorScore ?? 0;
                    dest.VisitorTeam.Scorers = ScorersForTeam(src, src.VisitorTeamId);
                    PopulateRosterEligibility(dest.VisitorTeam.Players, src.VisitorTeam?.Players, tournamentId);
                }

                dest.TournamentId = tournamentId;
            });

        _ = CreateMap<Match, MinimalMatchResponse>()
            .ForMember(dest => dest.MatchType, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.HomeTeamName, opt => opt.MapFrom(src => src.HomeTeam != null ? src.HomeTeam.Name : null))
            .ForMember(dest => dest.VisitorTeamName, opt => opt.MapFrom(src => src.VisitorTeam != null ? src.VisitorTeam.Name : null))
            .ForMember(dest => dest.WinningTeamName, opt => opt.MapFrom(src => src.WinningTeam != null ? src.WinningTeam.Name : null));

        _ = CreateMap<UpdateMatchRequest, Match>();
    }

    /// <summary>
    /// Builds the per-player scorer ranking for one team, attributing each scorer via Player.TeamId and summing points per player, highest first.
    /// </summary>
    private static List<ScorerByPlayerResponse> ScorersForTeam(Match match, Guid? teamId)
    {
        if (teamId is null)
        {
            return [];
        }

        Guid? tournamentId = match.Stage?.Division?.TournamentId;

        return [.. match.Scorers
            .Where(scorer => scorer.Player is not null && scorer.Player.TeamId == teamId.Value)
            .GroupBy(scorer => scorer.PlayerId)
            .Select(group => new ScorerByPlayerResponse
            {
                PlayerId = group.Key,
                FullName = group.First().Player!.FullName,
                JerseyNumber = JerseyNumberFor(group.First().Player!, tournamentId),
                Points = group.Sum(scorer => scorer.Points),
            })
            .OrderByDescending(scorer => scorer.Points)];
    }

    /// <summary>
    /// Resolves the player's jersey number from the matching roster registration for the tournament, since Player.JerseyNumber itself is transient.
    /// </summary>
    private static int? JerseyNumberFor(Player player, Guid? tournamentId)
    {
        if (player.PlayerTeamRegistrations is null || player.PlayerTeamRegistrations.Count == 0)
        {
            return null;
        }

        PlayerTeamRegistration? registration = tournamentId is null
            ? null
            : player.PlayerTeamRegistrations.FirstOrDefault(reg => reg.TournamentId == tournamentId.Value);

        return (registration ?? player.PlayerTeamRegistrations.First()).JerseyNumber;
    }

    /// <summary>
    /// Fills each roster player's season-scoped medical record status, habilitado flag, and jersey number from the matching PlayerTeamRegistration, since the plain Player to PublicPlayerResponse map cannot resolve them on its own.
    /// </summary>
    private static void PopulateRosterEligibility(
        List<PublicPlayerResponse> destPlayers, ICollection<Player>? srcPlayers, Guid? tournamentId)
    {
        if (tournamentId is null || srcPlayers is null || srcPlayers.Count == 0)
        {
            return;
        }

        Dictionary<Guid, Player> srcById = srcPlayers.ToDictionary(player => player.Id);

        foreach (PublicPlayerResponse destPlayer in destPlayers)
        {
            if (!srcById.TryGetValue(destPlayer.Id, out Player? srcPlayer)
                || srcPlayer.PlayerTeamRegistrations is null)
            {
                continue;
            }

            PlayerTeamRegistration? registration = srcPlayer.PlayerTeamRegistrations
                .FirstOrDefault(reg => reg.TournamentId == tournamentId.Value);

            if (registration is null)
            {
                continue;
            }

            destPlayer.MedicalRecordStatus = registration.MedicalRecordStatus;
            destPlayer.IsHabilitado = registration.IsHabilitado;
            destPlayer.JerseyNumber = registration.JerseyNumber;
        }
    }
}
