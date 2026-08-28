using Application.Interfaces.Services;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace API.Tests;

/// <summary>
/// HU-101: sensitive actions are recorded in the audit trail. Services are
/// resolved from the real host container (over a shared SQLite database), so
/// the audit wiring is exercised end to end. With no HTTP request bound the
/// actor resolves to the system user, which is the expected value for these
/// service-level invocations.
/// </summary>
public class AuditTrailTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditTrailTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WipeSampleData_WritesDataWipeAuditEntry()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDataMaintenanceService service = scope.ServiceProvider.GetRequiredService<IDataMaintenanceService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        int before = await db.AuditLogs.CountAsync(a => a.Action == AuditAction.DataWipe);

        await service.WipeSampleDataAsync();

        int after = await db.AuditLogs.CountAsync(a => a.Action == AuditAction.DataWipe);
        Assert.Equal(before + 1, after);

        // The wipe never deletes the audit trail itself.
        Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == AuditAction.DataWipe));
    }

    [Fact]
    public async Task ChangeTournamentStatus_WritesStatusChangeAuditEntry()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITournamentService tournamentService = scope.ServiceProvider.GetRequiredService<ITournamentService>();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        Tournament tournament = new()
        {
            Id = Guid.NewGuid(),
            CreatedBy = "test",
            Name = $"Audit Tournament {Guid.NewGuid()}",
            Description = "Status-change audit fixture.",
            Slug = $"audit-tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = TournamentStatus.Scheduled,
            Divisions = [],
            Teams = [],
        };

        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        int before = await db.AuditLogs.CountAsync(a => a.Action == AuditAction.TournamentStatusChange);

        // Scheduled -> OpenForRegistration is a valid transition that does not
        // trigger fixture generation.
        await tournamentService.ChangeStatusAsync(tournament.Id, TournamentStatus.OpenForRegistration);

        int after = await db.AuditLogs.CountAsync(a => a.Action == AuditAction.TournamentStatusChange);
        Assert.Equal(before + 1, after);

        AuditLog entry = await db.AuditLogs
            .Where(a => a.Action == AuditAction.TournamentStatusChange && a.TargetId == tournament.Id.ToString())
            .OrderByDescending(a => a.DateCreated)
            .FirstAsync();

        Assert.Equal(nameof(Tournament), entry.TargetType);
        Assert.Contains("Scheduled", entry.Detail);
        Assert.Contains("OpenForRegistration", entry.Detail);
    }
}
