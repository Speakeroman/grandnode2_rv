using Grand.Cms.Client.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;

namespace Grand.Cms.Client.Tests.Services;

[TestClass]
public class CmsApiKnowledgebaseServiceTests
{
    private static CmsApiKnowledgebaseService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(CmsClientConstants.HttpClientName)).Returns(client);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new CmsApiKnowledgebaseService(factory.Object, cache);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticles_WhenApiReturnsItems_ReturnsMappedList()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/knowledgebase/articles*")
               .Respond("application/json",
                   """[{"id":"k1","name":"How to setup","content":"Step by step...","seName":"how-to-setup","parentCategoryId":"","published":true,"displayOrder":1,"showOnHomepage":false,"metaKeywords":"","metaDescription":"","metaTitle":""}]""");

        var svc = CreateService(handler);
        var result = await svc.GetKnowledgebaseArticles("");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("How to setup", result[0].Name);
    }

    [TestMethod]
    public async Task GetKnowledgebaseArticle_WhenNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Get, "http://localhost/api/knowledgebase/articles/missing")
               .Respond(HttpStatusCode.NotFound);

        var svc = CreateService(handler);
        var result = await svc.GetKnowledgebaseArticle("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task InsertKnowledgebaseArticle_CallsPostAndPopulatesId()
    {
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "http://localhost/api/knowledgebase/articles")
               .Respond("application/json",
                   """{"id":"ka99","name":"FAQ","content":"","seName":"faq","parentCategoryId":"","published":true,"displayOrder":0,"showOnHomepage":false,"metaKeywords":"","metaDescription":"","metaTitle":""}""");

        var svc = CreateService(handler);
        var article = new Grand.Domain.Knowledgebase.KnowledgebaseArticle { Name = "FAQ" };
        await svc.InsertKnowledgebaseArticle(article);

        Assert.AreEqual("ka99", article.Id);
    }
}
