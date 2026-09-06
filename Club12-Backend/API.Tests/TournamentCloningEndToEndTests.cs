using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Application.DTOs.Divisions.Request;
using Application.DTOs.Tournament.Request;
using Application.DTOs.Tournament.Response;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>
/// End-to-end proof of the tournament-cloning pipeline (Phase 7): a real
/// <see cref="SampleTournamentBuilder"/>-produced tournament — multi-division,
/// cross-division cup, cup playoff mappings — is read through the real
/// GET {idOrSlug}/structure endpoint, reverse-mapped per the D1 rules (the
/// same rules `cloneWizard.ts` implements on the frontend: regular-cup
/// qualifiers from the PlayoffMapping span, cross-cup qualifiers from
/// groupCount x QualifiersPerGroup), edited (one zone dropped, mirroring an
/// organizer editing the wizard before submit), given an explicit DIFFERENT
/// category than the source, and submitted through the real, unchanged
/// POST /full transaction. Confirms the created tournament's structure
/// matches the edited/reconstructed request, the category is the chosen one
/// (never the source's), and every division starts with zero team
/// registrations and zero matches.
/// </summary>
public class TournamentCloningEndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public TournamentCloningEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static List<Venue> BuildVenues() =>
    [
        new() { Slug = "clon-cancha-1", CreatedBy = "test", Name = "Cancha Clon 1", Address = "Calle 1" },
        new() { Slug = "clon-cancha-2", CreatedBy = "test", Name = "Cancha Clon 2", Address = "Calle 2" },
    ];

    private static SampleTournamentBuilder.TournamentDefinition BuildSourceDefinition()
    {
        static (string[] Names, string[] Codes, string[] Colors) Roster(string prefix, int count) => (
            [.. Enumerable.Range(1, count).Select(i => $"{prefix} {i}")],
            [.. Enumerable.Range(1, count).Select(i => $"{prefix[..2].ToUpperInvariant()}{i:00}")],
            [.. Enumerable.Range(1, count).Select(_ => "#222222")]);

        (string[] namesA, string[] codesA, string[] colorsA) = Roster("Equipo A", 4);
        (string[] namesB, string[] codesB, string[] colorsB) = Roster("Equipo B", 4);

        return new(
            Name: "Apertura Clonable",
            Description: "Torneo fuente para el flujo de clonado.",
            TeamRegistrationDeadline: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageStartDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            StageEndDate: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Divisions:
            [
                new("Zona A", namesA, codesA, colorsA,
                    [new SampleTournamentBuilder.PlayoffCupDefinition("Copa Oro", FromPosition: 1, ToPosition: 4, BestOf: 3)]),
                new("Zona B", namesB, codesB, colorsB,
                    [new SampleTournamentBuilder.PlayoffCupDefinition("Copa Plata", FromPosition: 1, ToPosition: 4, BestOf: 1)]),
            ],
            CrossCup: new SampleTournamentBuilder.CrossCupDefinition(
                DivisionName: "Copa Cruzada", GroupCount: 2, QualifiersPerGroup: 1, RoundRobinLegs: 1, FinalsBestOf: 1),
            Category: TournamentCategory.Masculine);
    }

    /// <summary>
    /// Reconstructs one division's request from its structure tree per the D1
    /// rules: a regular cup's qualifier count comes from its PlayoffMapping
    /// span; the cross cup's pooled bracket carries none, since it is never
    /// seeded by a position-range mapping. Every stage gets a fresh sequential
    /// date window, mirroring `submitWizard.ts`'s own date-building — the
    /// source's own dates never carry over (HU-cloning: dates always blank
    /// until the organizer re-enters them).
    /// </summary>
    private static CreateFullDivisionRequest ReconstructDivision(
        DivisionStructureResponse source, TournamentCategory chosenCategory, DateTime anchor)
    {
        List<CreateFullStageRequest> stages = [];
        DateTime cursor = anchor;

        foreach (StageStructureResponse stage in source.Stages)
        {
            DateTime end = cursor.AddDays(7);
            stages.Add(new CreateFullStageRequest
            {
                Name = stage.Name,
                StageType = stage.StageType,
                IsElimination = stage.IsElimination,
                StartDate = cursor,
                EndDate = end,
                BracketName = stage.BracketName,
                BestOf = stage.BestOf,
                RoundRobinLegs = stage.RoundRobinLegs,
            });
            cursor = end;
        }

        return new CreateFullDivisionRequest
        {
            Name = source.Name,
            IsCrossDivisionCup = source.IsCrossDivisionCup,
            PointsForWin = source.PointsForWin,
            PointsForLoss = source.PointsForLoss,
            QualifiersPerGroup = source.QualifiersPerGroup,
            Category = chosenCategory,
            PlayoffMappings = [.. source.PlayoffMappings.Select(mapping => new PlayoffMappingRequest
            {
                FromPosition = mapping.FromPosition,
                ToPosition = mapping.ToPosition,
                Destination = mapping.Destination,
            })],
            Stages = stages,
        };
    }

    [Fact]
    public async Task CloningPipeline_SeedStructureEditSubmit_CreatesFaithfulTournamentWithZeroInstanceData()
    {
        // 1. Seed a real, non-trivial source tournament (two regular cup
        //    divisions plus a cross-division cup) via the same builder the
        //    sample-data seeder uses.
        int playerCounter = 0;
        SampleTournamentBuilder.BuildResult built = SampleTournamentBuilder.Build(
            BuildSourceDefinition(), BuildVenues(), ref playerCounter, includePlayoffs: true);

        Guid sourceTournamentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            db.Tournaments.Add(built.Tournament);
            await db.SaveChangesAsync();
            sourceTournamentId = built.Tournament.Id;
        }

        HttpClient client = _factory.CreateAuthenticatedClient(Roles.Admin);

        // 2. Read the source's structure through the real endpoint — the exact
        //    contract the frontend's "Clonar torneo" action hits.
        HttpResponseMessage structureResponse =
            await client.GetAsync($"api/tournaments/{sourceTournamentId}/structure");
        Assert.Equal(HttpStatusCode.OK, structureResponse.StatusCode);

        TournamentStructureResponse structure =
            (await structureResponse.Content.ReadFromJsonAsync<TournamentStructureResponse>(JsonOptions))!;

        Assert.Equal(3, structure.Divisions.Count);

        // 3. Reverse-map per D1 (mirrors cloneWizard.ts), choosing a category
        //    DIFFERENT from the source's (Masculine) to prove it is an
        //    explicit organizer choice, never silently inherited — then EDIT
        //    the wizard state by dropping "Zona B" entirely, mirroring an
        //    organizer deleting a zone before submitting.
        const TournamentCategory chosenCategory = TournamentCategory.Feminine;
        DateTime newStartDate = new(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime newDeadline = newStartDate.AddDays(-14);

        List<DivisionStructureResponse> editedDivisions =
            [.. structure.Divisions.Where(division => division.Name != "Zona B")];
        Assert.Equal(2, editedDivisions.Count);

        CreateFullTournamentRequest cloneRequest = new()
        {
            Name = $"{structure.Name} (copia)",
            Description = structure.Description ?? string.Empty,
            StartDate = newStartDate,
            TeamRegistrationDeadline = newDeadline,
            Category = chosenCategory,
            Divisions = [.. editedDivisions.Select(
                division => ReconstructDivision(division, chosenCategory, newStartDate))],
        };

        // 4. Submit through the SAME unchanged /full creation transaction.
        HttpResponseMessage createResponse =
            await client.PostAsJsonAsync("api/tournaments/full", cloneRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        TournamentResponse created =
            (await createResponse.Content.ReadFromJsonAsync<TournamentResponse>(JsonOptions))!;

        // 5. Verify the created tournament, in a FRESH scope, matches the
        //    edited wizard state — not the original source structure.
        using IServiceScope verifyScope = _factory.Services.CreateScope();
        ApplicationDBContext verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament createdTournament = await verifyDb.Tournaments
            .Include(t => t.Divisions).ThenInclude(d => d.Stages)
            .Include(t => t.Divisions).ThenInclude(d => d.PlayoffMappings)
            .Include(t => t.Divisions).ThenInclude(d => d.DivisionTeamRegistrations)
            .SingleAsync(t => t.Id == created.Id);

        // Dates were required and honored — never carried over blank/from the source.
        Assert.Equal(newStartDate, createdTournament.StartDate);
        Assert.Equal(newDeadline, createdTournament.TeamRegistrationDeadline);
        // The chosen category applies, NOT the source's Masculine.
        Assert.Equal(TournamentCategory.Feminine, createdTournament.Category);

        // The edit (deleting "Zona B") is reflected — only 2 divisions, not the original 3.
        Assert.Equal(2, createdTournament.Divisions.Count);
        Assert.DoesNotContain(createdTournament.Divisions, d => d.Name == "Zona B");

        Division zonaA = createdTournament.Divisions.Single(d => d.Name == "Zona A");
        Assert.False(zonaA.IsCrossDivisionCup);
        Assert.Equal(TournamentCategory.Feminine, zonaA.Category);
        Assert.Contains(zonaA.Stages, s => s.StageType == StageType.Group);
        Assert.Contains(zonaA.Stages, s => s.StageType == StageType.SemiFinal && s.BracketName == "Copa Oro");
        Assert.Contains(zonaA.Stages, s => s.StageType == StageType.Final && s.BracketName == "Copa Oro");
        DivisionPlayoffMapping zonaAMapping = Assert.Single(zonaA.PlayoffMappings);
        Assert.Equal(1, zonaAMapping.FromPosition);
        Assert.Equal(4, zonaAMapping.ToPosition);
        Assert.Equal("Copa Oro", zonaAMapping.Destination);

        Division crossCup = createdTournament.Divisions.Single(d => d.IsCrossDivisionCup);
        Assert.Equal("Copa Cruzada", crossCup.Name);
        Assert.Equal(1, crossCup.QualifiersPerGroup);
        Assert.Equal(2, crossCup.Stages.Count(s => s.StageType == StageType.Group));
        Assert.Contains(crossCup.Stages, s => s.StageType == StageType.Final);

        // Zero instance data: every created division starts with no team
        // registrations at all, and no matches were generated (structure-only
        // creation, exactly like a from-scratch wizard run).
        Assert.All(createdTournament.Divisions, d => Assert.Empty(d.DivisionTeamRegistrations));
        int matchCount = await verifyDb.Matches
            .CountAsync(m => createdTournament.Divisions.Select(d => d.Id).Contains(m.Stage.DivisionId));
        Assert.Equal(0, matchCount);
    }
}
