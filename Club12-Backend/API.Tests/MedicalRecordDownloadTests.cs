using API.Controllers;

using Application.DTOs.MedicalRecord.Response;
using Application.Interfaces.Services;
using Application.Interfaces.Storage;
using Application.Utils.Helper.SupabaseHelper;

using Domain.Enums;

using Infrastructure.Storage;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace API.Tests;

/// <summary>
/// Covers medical-record DOWNLOAD (QA wave 1, Bug 2): the uploaded PDF is kept
/// in a PRIVATE storage area (no public URL), so it can only be served back
/// through the API. Before the fix there was no download path at all. These
/// tests verify (1) the storage boundary round-trips an uploaded object back to
/// its bytes and (2) the controller streams those bytes as a PDF with the
/// original file name, and returns 404 when no file exists. The controller is
/// exercised directly (its DI graph pulls in the live SupabaseHelper, the same
/// documented host testability gap as SupabaseDependentControllerNotFoundTests).
/// </summary>
public class MedicalRecordDownloadTests
{
    [Fact]
    public async Task Storage_StoreThenDownload_RoundTripsBytes()
    {
        InMemoryRawStorage raw = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        SupabaseMedicalRecordStorage storage = new(raw, configuration);
        byte[] pdf = Encoding.UTF8.GetBytes("%PDF-1.4 fake medical record");
        Guid teamId = Guid.NewGuid();

        string objectPath = await storage.StoreAsync(
            teamId, Guid.NewGuid(), "ficha.pdf", new MemoryStream(pdf));

        byte[] downloaded = await storage.DownloadAsync(objectPath);

        Assert.Equal(pdf, downloaded);
        Assert.StartsWith($"{teamId}/", objectPath);
    }

    [Fact]
    public async Task Download_WithStoredFile_StreamsPdfWithOriginalName()
    {
        byte[] pdf = Encoding.UTF8.GetBytes("%PDF-1.4 stored");
        Guid playerId = Guid.NewGuid();
        Guid teamId = Guid.NewGuid();
        Guid tournamentId = Guid.NewGuid();
        const string fileUrl = "medical-records/t/p/object.pdf";

        FakeMedicalRecordService service = new(new MedicalRecordResponse
        {
            PlayerId = playerId,
            TeamId = teamId,
            TournamentId = tournamentId,
            Status = MedicalRecordStatus.Pending,
            IsHabilitado = false,
            FileUrl = fileUrl,
            FileName = "ficha-original.pdf",
        });
        FakeStorage storage = new(new Dictionary<string, byte[]> { [fileUrl] = pdf });

        MedicalRecordController controller = new(service, storage, null!);

        IActionResult result = await controller.DownloadMedicalRecord(playerId, teamId, tournamentId);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("ficha-original.pdf", file.FileDownloadName);
        Assert.Equal(pdf, file.FileContents);
    }

    [Fact]
    public async Task Download_WhenNoFileStored_Returns404()
    {
        Guid playerId = Guid.NewGuid();

        // Registration exists but no file was ever uploaded (FileUrl null).
        FakeMedicalRecordService service = new(new MedicalRecordResponse
        {
            PlayerId = playerId,
            TeamId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid(),
            Status = MedicalRecordStatus.Pending,
            IsHabilitado = false,
            FileUrl = null,
        });
        FakeStorage storage = new([]);

        MedicalRecordController controller = new(service, storage, null!);
        ConfigureProblemDetailsFactory(controller);

        IActionResult result = await controller.DownloadMedicalRecord(
            playerId, Guid.NewGuid(), Guid.NewGuid());

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    private static void ConfigureProblemDetailsFactory(ControllerBase controller)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddMvc();
        ServiceProvider provider = services.BuildServiceProvider();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = provider },
        };
    }

    private sealed class FakeMedicalRecordService(MedicalRecordResponse response) : IMedicalRecordService
    {
        public Task<MedicalRecordResponse> RecordUploadAsync(
            Guid playerId, Guid teamId, Guid tournamentId, string fileReference, string fileName, string actor)
            => Task.FromResult(response);

        public Task<MedicalRecordResponse> ReviewAsync(
            Guid playerId, Guid teamId, Guid tournamentId, bool approve, string? reason, string actor)
            => Task.FromResult(response);

        public Task<MedicalRecordResponse?> GetAsync(Guid playerId, Guid teamId, Guid tournamentId)
            => Task.FromResult<MedicalRecordResponse?>(response);
    }

    private sealed class FakeStorage(Dictionary<string, byte[]> objects) : IMedicalRecordStorage
    {
        public Task<string> StoreAsync(
            Guid teamId, Guid playerId, string fileName, Stream content, CancellationToken ct = default)
            => Task.FromResult("unused");

        public Task<byte[]> DownloadAsync(string objectPath, CancellationToken ct = default)
            => Task.FromResult(objects[objectPath]);
    }

    private sealed class InMemoryRawStorage : ISupabaseRawStorage
    {
        private readonly Dictionary<string, byte[]> _objects = [];

        public async Task UploadRawAsync(string objectPath, Stream content, string? bucket = null)
        {
            using MemoryStream buffer = new();
            await content.CopyToAsync(buffer);
            _objects[objectPath] = buffer.ToArray();
        }

        public Task<byte[]> DownloadRawAsync(string objectPath, string? bucket = null) => Task.FromResult(_objects[objectPath]);

        public Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix, string? bucket = null)
            => throw new System.NotImplementedException();

        public Task RemoveRawAsync(string objectPath, string? bucket = null) => throw new System.NotImplementedException();
    }
}
