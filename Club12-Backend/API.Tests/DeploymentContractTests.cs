using System.Text.RegularExpressions;

namespace API.Tests;

/// <summary>
/// Pins the docker-compose topology and .env.example contract for the
/// self-hosted Postgres change: a network-private `db` service the backend
/// waits on, and the matching database keys in the example env file.
///
/// Plain-text assertions on the repo-root files — API.Tests ships no YAML
/// parser and adding one for these checks is not warranted.
/// </summary>
public class DeploymentContractTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static string ComposeYaml() => File.ReadAllText(Path.Combine(RepoRoot(), "docker-compose.yml"));

    private static string EnvExample() => File.ReadAllText(Path.Combine(RepoRoot(), ".env.example"));

    /// <summary>
    /// Returns the text of a single top-level compose service block (the lines
    /// from `  &lt;name&gt;:` up to the next 2-space-indented key or a column-0 key).
    /// </summary>
    private static string ServiceBlock(string yaml, string serviceName)
    {
        string[] lines = yaml.Replace("\r\n", "\n").Split('\n');
        int start = Array.FindIndex(lines, l => l.StartsWith($"  {serviceName}:", StringComparison.Ordinal));
        Assert.True(start >= 0, $"compose service '{serviceName}' not found");

        List<string> block = [lines[start]];
        for (int i = start + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            bool isNextTopLevelKey = line.Length > 0 && !char.IsWhiteSpace(line[0]);
            bool isNextServiceKey = Regex.IsMatch(line, @"^  \S");
            if (isNextTopLevelKey || isNextServiceKey)
            {
                break;
            }

            block.Add(line);
        }

        return string.Join('\n', block);
    }

    [Fact]
    public void Compose_DefinesPinnedPostgresDbService()
    {
        string db = ServiceBlock(ComposeYaml(), "db");

        Assert.Contains("image: postgres:17-alpine", db, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_DbServicePublishesNoHostPort()
    {
        string db = ServiceBlock(ComposeYaml(), "db");

        Assert.DoesNotContain("ports:", db, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_DbServiceHasHealthcheckAndMemoryLimit()
    {
        string db = ServiceBlock(ComposeYaml(), "db");

        Assert.Contains("healthcheck:", db, StringComparison.Ordinal);
        Assert.Contains("pg_isready", db, StringComparison.Ordinal);
        Assert.Contains("memory:", db, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_BackendWaitsForDbHealth()
    {
        string backend = ServiceBlock(ComposeYaml(), "backend");

        Assert.Contains("depends_on:", backend, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"db:\s*\n\s*condition:\s*service_healthy"), backend);
    }

    [Fact]
    public void EnvExample_DeclaresPostgresImageKeys()
    {
        string env = EnvExample();

        Assert.Contains("POSTGRES_USER=", env, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_PASSWORD=", env, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_DB=", env, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvExample_ConnectionStringTargetsTheInternalServiceWithoutTls()
    {
        string line = EnvExample()
            .Replace("\r\n", "\n")
            .Split('\n')
            .Single(l => l.StartsWith("ConnectionStrings__DbConnection=", StringComparison.Ordinal));

        Assert.Contains("Host=db", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SSL Mode=Disable", line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gitignore_ExcludesDotEnv()
    {
        string[] gitignore = File.ReadAllLines(Path.Combine(RepoRoot(), ".gitignore"))
            .Select(l => l.Trim())
            .ToArray();

        Assert.Contains(gitignore, l => l is ".env" or "/.env" or "*.env");
    }
}
