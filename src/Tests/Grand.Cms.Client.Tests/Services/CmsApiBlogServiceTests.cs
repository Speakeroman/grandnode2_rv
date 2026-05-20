using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiBlogServiceTests
{
    private static CmsApiBlogService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiBlogService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetAllBlogPosts_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/blogs*")
               .Respond("application/json",
                   """[{"id":"1","title":"Hello","body":"World","bodyOverview":"","tags":"","startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"hello","pictureId":"","allowComments":false}]""");

        var svc = CreateService(handler);
        var result = await svc.GetAllBlogPosts("", null, null, 0, 10);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("Hello", result[0].Title);
    }

    [TestMethod]
    public async Task GetBlogPostById_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/blogs/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetBlogPostById("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertBlogPost_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/blogs")
               .Respond("application/json",
                   """{"id":"abc123","title":"New","body":"","bodyOverview":"","tags":"","startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"new","pictureId":"","allowComments":false}""");

        var svc = CreateService(handler);
        var post = new Grand.Domain.Blogs.BlogPost { Title = "New" };
        await svc.InsertBlogPost(post);

        Assert.AreEqual("abc123", post.Id);
    }
}
