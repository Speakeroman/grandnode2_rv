using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiPageServiceTests
{
    private static CmsApiPageService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiPageService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetAllPages_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/pages*")
               .Respond("application/json",
                   """[{"id":"p1","systemName":"about-us","title":"About Us","body":"<p>About</p>","seName":"about-us","published":true,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","pageLayoutId":""}]""");

        var svc = CreateService(handler);
        var result = await svc.GetAllPages("");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("about-us", result[0].SystemName);
    }

    [TestMethod]
    public async Task GetPageById_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/pages/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetPageById("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertPage_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/pages")
               .Respond("application/json",
                   """{"id":"pg99","systemName":"new-page","title":"New Page","body":"","seName":"new-page","published":false,"startDateUtc":null,"endDateUtc":null,"metaKeywords":"","metaDescription":"","metaTitle":"","pageLayoutId":""}""");

        var svc = CreateService(handler);
        var page = new Grand.Domain.Pages.Page { SystemName = "new-page", Title = "New Page" };
        await svc.InsertPage(page);

        Assert.AreEqual("pg99", page.Id);
    }
}
