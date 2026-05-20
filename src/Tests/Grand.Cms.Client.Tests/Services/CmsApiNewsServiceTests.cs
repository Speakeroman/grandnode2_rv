using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiNewsServiceTests
{
    private static CmsApiNewsService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiNewsService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetAllNews_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/news*")
               .Respond("application/json",
                   """[{"id":"1","title":"Breaking","short":"Summary","full":"Full text","published":true,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"breaking","pictureId":""}]""");

        var svc = CreateService(handler);
        var result = await svc.GetAllNews("", 0, 10);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("Breaking", result[0].Title);
        Assert.IsTrue(result[0].Published);
    }

    [TestMethod]
    public async Task GetNewsById_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/news/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetNewsById("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertNews_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/news")
               .Respond("application/json",
                   """{"id":"news99","title":"New Article","short":"","full":"","published":false,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","seName":"","pictureId":""}""");

        var svc = CreateService(handler);
        var item = new Grand.Domain.News.NewsItem { Title = "New Article" };
        await svc.InsertNews(item);

        Assert.AreEqual("news99", item.Id);
    }
}
