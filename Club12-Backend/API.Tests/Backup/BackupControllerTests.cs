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
/// exception-mapped status codes" decision) — GET/POST/DELETE against a fake
/// IBackupOperationsService, no HTTP pipeline
/// involved. Non-Admin/anonymous 401/403 gating (which only takes effect via
/// [Authorize] in the real MVC pipeline) is covered separately by
/// BackupAuthorizationTests.
/// </summary>
public class BackupControllerTests
{
    private static BackupController CreateSut(IBackupOperationsService? operations = null)
    {
        return new BackupController(operations ?? new FakeBackupOperationsService());
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithCatalogRecords()
    {
        BackupRecord record = new()
        {
            CreatedBy = AuditConstants.SystemUser,
            StoragePath = "a.sql",
            SizeBytes = 10,
            Origin = BackupOrigin.Manual,
        };
        FakeBackupOperationsService operations = new()
        {
            NextListResult = [record],
        };
        BackupController sut = CreateSut(operations: operations);

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

    /// <summary>
    /// threat-matrix "Restore of foreign/uploaded dumps": Restore's only
    /// input parameter is the route Guid — no [FromBody] parameter
    /// exists on the action, so no request body is ever bound/deserialized
    /// into a path or dump payload. Confirming the exact id reaches
    /// IBackupOperationsService.RestoreBackupAsync is the closest
    /// unit-level proof of that (no separate body-binding surface to probe).
    /// </summary>
    [Fact]
    public async Task Restore_Completed_ReturnsOkWithRecord_PassesOnlyRouteId()
    {
        Guid id = Guid.NewGuid();
        BackupRecordResponse expected = new(Guid.NewGuid(), DateTime.UtcNow, 99, "Job", "safety.sql");
        FakeBackupOperationsService operations = new()
        {
            NextRestoreResult = new BackupOperationResult(BackupOperationOutcome.Completed, expected, null),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Restore(id, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        BackupRecordResponse body = Assert.IsType<BackupRecordResponse>(ok.Value);
        Assert.Equal(expected, body);
        Assert.Equal(id, operations.LastRestoreId);
    }

    [Fact]
    public async Task Restore_NotFound_ReturnsNotFound()
    {
        FakeBackupOperationsService operations = new()
        {
            NextRestoreResult = new BackupOperationResult(BackupOperationOutcome.NotFound, null, null),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Restore(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Restore_Busy_ReturnsConflict()
    {
        FakeBackupOperationsService operations = new()
        {
            NextRestoreResult = new BackupOperationResult(BackupOperationOutcome.Busy, null, "busy"),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Restore(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Restore_Failed_ReturnsInternalServerError()
    {
        FakeBackupOperationsService operations = new()
        {
            NextRestoreResult = new BackupOperationResult(BackupOperationOutcome.Failed, null, "boom"),
        };
        BackupController sut = CreateSut(operations: operations);

        IActionResult result = await sut.Restore(Guid.NewGuid(), CancellationToken.None);

        ObjectResult obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
    }
}
