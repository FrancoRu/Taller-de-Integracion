using System.Net;

namespace API.Tests;

/// <summary>
/// Proves the two container-probe endpoints defined by the
/// service-health-endpoint spec: `/health` (liveness — process is up, no
/// dependency checks) and `/health/ready` (readiness — DB connectivity via
/// AddDbContextCheck&lt;ApplicationDBContext&gt;). Both endpoints must be
/// anonymous. Real HTTP round trips through CustomWebApplicationFactory,
/// same pattern as SmokeTests/AuthorizationGatingTests.
/// </summary>
public class HealthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Uses a dedicated factory instance (not the shared class fixture) so
    /// breaking its DB connection cannot leak into other tests in this
    /// class. CreateClient() first forces the host to boot (schema created,
    /// migrations "applied") before the connection is broken.
    /// </summary>
    [Fact]
    public async Task Health_ReturnsOk_EvenWithDatabaseUnreachable()
    {
        using CustomWebApplicationFactory brokenDbFactory = new();
        HttpClient client = brokenDbFactory.CreateClient();
        brokenDbFactory.BreakDatabaseConnection();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsOk_WhenDatabaseReachable()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsServiceUnavailable_WhenDatabaseUnreachable()
    {
        using CustomWebApplicationFactory brokenDbFactory = new();
        HttpClient client = brokenDbFactory.CreateClient();
        brokenDbFactory.BreakDatabaseConnection();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_AllowAnonymousAccess(string path)
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Covers the service-health-endpoint spec scenario "Repeated readiness
    /// failures do not affect the process": with the DB unreachable for an
    /// extended period, every /health/ready call must keep returning 503
    /// (not throw, hang, or crash the host), and /health must keep returning
    /// 200 in between those failing calls on the SAME factory/host instance —
    /// proving the process itself stays up and responsive throughout, not
    /// just that a single call degrades gracefully.
    /// </summary>
    [Fact]
    public async Task HealthReady_RepeatedFailures_KeepReturning503_WithoutAffectingLiveness()
    {
        using CustomWebApplicationFactory brokenDbFactory = new();
        HttpClient client = brokenDbFactory.CreateClient();
        brokenDbFactory.BreakDatabaseConnection();

        HttpResponseMessage ready1 = await client.GetAsync("/health/ready");
        HttpResponseMessage live1 = await client.GetAsync("/health");
        HttpResponseMessage ready2 = await client.GetAsync("/health/ready");
        HttpResponseMessage live2 = await client.GetAsync("/health");
        HttpResponseMessage ready3 = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready1.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready2.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready3.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live2.StatusCode);
    }
}
