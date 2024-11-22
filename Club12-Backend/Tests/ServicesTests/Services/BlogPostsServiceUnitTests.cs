namespace Services.Tests.Services;

[TestFixture]
public class BlogPostServiceTests
{
    private IGenericService<BlogPost> _genericBlogPostService = Substitute.For<IGenericService<BlogPost>>();
    private BlogPostService _blogPostService = null!;

    [SetUp]
    public void SetUp()
    {
        _genericBlogPostService = Substitute.For<IGenericService<BlogPost>>();
        _blogPostService = new BlogPostService(_genericBlogPostService);
    }

    [Test]
    public async Task CreateBlogPostAsync_ShouldCallInsertAsync()
    {
        BlogPost blogPost = TestEntityFactory.CreateBlogPost();

        await _blogPostService.CreateBlogPostAsync(blogPost);

        await _genericBlogPostService.Received(1).InsertAsync(blogPost);
    }

    [Test]
    public async Task DeleteBlogPostAsync_ShouldCallDeleteAsync()
    {
        BlogPost blogPost = TestEntityFactory.CreateBlogPost();

        bool result = await _blogPostService.DeleteBlogPostAsync(blogPost);

        await _genericBlogPostService.Received(1).DeleteAsync(blogPost);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteBlogPostAsync_ShouldReturnFalseOnException()
    {
        BlogPost blogPost = TestEntityFactory.CreateBlogPost();
        _genericBlogPostService.When(x => x.DeleteAsync(blogPost)).Do(x => throw new Exception());

        bool result = await _blogPostService.DeleteBlogPostAsync(blogPost);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateBlogPostAsync_ShouldCallUpdateAsync()
    {
        BlogPost blogPost = TestEntityFactory.CreateBlogPost();

        bool result = await _blogPostService.UpdateBlogPostAsync(blogPost);

        await _genericBlogPostService.Received(1).UpdateAsync(blogPost);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateBlogPostAsync_ShouldReturnFalseOnException()
    {
        BlogPost blogPost = TestEntityFactory.CreateBlogPost();
        _genericBlogPostService.When(x => x.UpdateAsync(blogPost)).Do(x => throw new Exception());

        bool result = await _blogPostService.UpdateBlogPostAsync(blogPost);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetBlogPostByIdAsync_ShouldReturnBlogPost()
    {
        Guid blogPostId = Guid.NewGuid();
        BlogPost blogPost = TestEntityFactory.CreateBlogPost(blogPostId);

        IQueryable<BlogPost> blogPosts = new List<BlogPost> { blogPost }.AsAsyncQueryable();

        _genericBlogPostService.FilterByExpression(Arg.Any<Expression<Func<BlogPost, bool>>>())
            .Returns(blogPosts);

        BlogPost? result = await _blogPostService.GetBlogPostByIdAsync(blogPostId);

        Assert.That(result, Is.EqualTo(blogPost));
    }

    [Test]
    public async Task GetAllBlogPostsAsync_ShouldReturnPaginatedResponse()
    {
        // Arrange
        IQueryable<BlogPost> blogPosts = TestEntityFactory.CreateBlogPosts().AsAsyncQueryable();
        GetBlogPostsFilteredRequest filter = TestEntityFactory.CreateBlogPostsFilter();

        // Mock async-compatible IQueryable
        TestEntityFactory.SetupFilterWithPagination(_genericBlogPostService, blogPosts, filter);

        // Act
        PaginatedResponse<BlogPost> result = await _blogPostService.GetAllBlogPostsAsync(filter);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Count(), Is.EqualTo(blogPosts.Count()));
            Assert.That(result.Page, Is.EqualTo(filter.PageNumber));
            Assert.That(result.PageSize, Is.EqualTo(filter.PageSize));
            Assert.That(result.TotalCount, Is.EqualTo(blogPosts.Count()));
        });
    }
}
