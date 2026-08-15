using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace API.Tests;

/// <summary>
/// Proves the test harness itself works: boots the real API host via
/// <see cref="CustomWebApplicationFactory"/> and issues an anonymous GET.
/// This is a harness smoke test, not business-logic coverage.
/// </summary>
public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDivisions_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/divisions/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
