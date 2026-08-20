using API.Controllers;
using API.Tests.Backup.Fakes;

using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Tests.Backup;

/// <summary>
/// Pure unit tests for BackupController's outcome-to-status-code
/// mapping (design.md's "Controllers return an explicit outcome, not
/// exception-mapped status codes" decision) — GET/POST/DELETE against fake
/// IBackupCatalog/IBackupOperationsService, no HTTP pipeline
/// involved. Non-Admin/anonymous 401/403 gating (which only takes effect via
/// [Authorize] in the real MVC pipeline) is covered separately by
/// BackupAuthorizationTests.
/// </summary>
public class BackupControllerTests
{
    private static BackupController CreateSut(IBackupCatalog? catalog = null, IBackupOperationsService? operations = null)
    {
        return new BackupController(catalog ?? new FakeBackupCatalog(), operations ?? new FakeBackupOperationsService());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCatalogRecords()
    {
        FakeBackupCatalog catalog = new();
        await catalog.AddAsync(new BackupRecord
        {
            CreatedBy = AuditConstants.SystemUser,
            StoragePath = "a.sql",
            SizeBytes = 10,
            Origin = BackupOrigin.Manual,
        });
        BackupController sut = CreateSut(catalog: catalog);

        ActionResult<IReadOnlyList<BackupRecordResponse>> result = await sut.GetAll(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<BackupRecordResponse> body = Assert.IsAssignableFrom<IReadOnlyList<BackupRecordResponse>>(ok.Value);
        Assert.Single(body);
        Assert.Equal("a.sql", body[0].StoragePath);
    }

    [Fact]
    public async Task GetAll_EmptyCatalog_ReturnsOkWithEmptyList()
    {
        BackupController sut = CreateSut();

        ActionResult<IReadOnlyList<BackupRecordResponse>> result = await sut.GetAll(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        IReadOnlyList<BackupRecordResponse> body = Assert.IsAssignableFrom<IReadOnlyList<BackupRecordResponse>>(ok.Value);
        Assert.Empty(body);
    }

    [Fact]
    public async Task Create_Completed_ReturnsOkWithRecord()
    {
        BackupRecordResponse expected = new(Guid.NewGuid(), DateTime.UtcNow, 42, "Manual", "a.sql");
        FakeBackupOperationsService operations = new()
        {
            NextCreateResult = new BackupOperationResult(BackupOperationOutcome.Completed, expected, null),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Create(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        BackupRecordResponse body = Assert.IsType<BackupRecordResponse>(ok.Value);
        Assert.Equal(expected, body);
    }

    [Fact]
    public async Task Create_Busy_ReturnsConflict()
    {
        FakeBackupOperationsService operations = new()
        {
            NextCreateResult = new BackupOperationResult(BackupOperationOutcome.Busy, null, "busy"),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Create(CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Create_Failed_ReturnsInternalServerError()
    {
        FakeBackupOperationsService operations = new()
        {
            NextCreateResult = new BackupOperationResult(BackupOperationOutcome.Failed, null, "boom"),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Create(CancellationToken.None);

        ObjectResult obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
    }

    [Fact]
    public async Task Delete_Completed_ReturnsNoContent()
    {
        FakeBackupOperationsService operations = new()
        {
            NextDeleteResult = new BackupOperationResult(BackupOperationOutcome.Completed, null, null),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        FakeBackupOperationsService operations = new()
        {
            NextDeleteResult = new BackupOperationResult(BackupOperationOutcome.NotFound, null, null),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Busy_ReturnsConflict()
    {
        FakeBackupOperationsService operations = new()
        {
            NextDeleteResult = new BackupOperationResult(BackupOperationOutcome.Busy, null, "busy"),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Delete_Failed_ReturnsInternalServerError()
    {
        FakeBackupOperationsService operations = new()
        {
            NextDeleteResult = new BackupOperationResult(BackupOperationOutcome.Failed, null, "boom"),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Delete(Guid.NewGuid(), CancellationToken.None);

        ObjectResult obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
    }
}
