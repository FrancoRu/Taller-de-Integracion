using Application.Utils.Helper.Slug;

namespace API.Tests;

/// <summary>
/// Verifies slug generation produces lowercase, kebab-case, accent-free
/// identifiers and that unique-slug resolution appends numeric suffixes on
/// collision.
/// </summary>
public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Campeonato Principal - La Vuelta", "campeonato-principal-la-vuelta")]
    [InlineData("River Plate", "river-plate")]
    [InlineData("Águilas Doradas F.C.", "aguilas-doradas-f-c")]
    [InlineData("  Leading and trailing spaces  ", "leading-and-trailing-spaces")]
    [InlineData("Multiple---Hyphens", "multiple-hyphens")]
    [InlineData("Ñoño Ünïçode", "nono-unicode")]
    public void GenerateSlug_ProducesExpectedKebabCaseSlug(string input, string expectedSlug)
    {
        string slug = SlugGenerator.GenerateSlug(input);

        Assert.Equal(expectedSlug, slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_NoCollision_ReturnsBaseSlug()
    {
        string slug = await SlugGenerator.GenerateUniqueSlugAsync(
            "River Plate",
            _ => Task.FromResult(false));

        Assert.Equal("river-plate", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_SingleCollision_AppendsSuffixTwo()
    {
        string slug = await SlugGenerator.GenerateUniqueSlugAsync(
            "River Plate",
            candidate => Task.FromResult(candidate == "river-plate"));

        Assert.Equal("river-plate-2", slug);
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_MultipleCollisions_AppendsNextAvailableSuffix()
    {
        HashSet<string> taken = ["river-plate", "river-plate-2", "river-plate-3"];

        string slug = await SlugGenerator.GenerateUniqueSlugAsync(
            "River Plate",
            candidate => Task.FromResult(taken.Contains(candidate)));

        Assert.Equal("river-plate-4", slug);
    }
}
