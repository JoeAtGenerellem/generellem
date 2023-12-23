using System.Net;

using Generellem.Services;

using Moq.Protected;

namespace Generellem.DocumentSource.Tests;

public class WebsiteTests
{
    readonly Mock<IHttpClientFactory> httpClientFactMock = new();
    readonly Mock<HttpClient> httpClientMock = new();
    readonly Mock<HttpMessageHandler> httpMsgHandlerMock = new();

    public WebsiteTests()
    {
        httpClientMock = new Mock<HttpClient>(httpMsgHandlerMock.Object);
        httpClientFactMock.Setup(fact => fact.Create()).Returns(httpClientMock.Object);
    }

    [Fact]
    public async Task GetDocumentsAsync_WithValidUrl_ReturnsPages()
    {
        const string html = "<html><body><a href='page1'>Page 1</a><a href='page2'>Page 2</a></body></html>";

        var website = new Website(httpClientFactMock.Object);
        httpMsgHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                nameof(HttpClient.SendAsync), 
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(html) });
        List<DocumentInfo> docInfos = new();

        await foreach (DocumentInfo doc in website.GetDocumentsAsync(CancellationToken.None))
            docInfos.Add(doc);
            
        Assert.NotEmpty(docInfos);
    }

    [Fact]
    public async Task GetDocumentsAsync_WithInvalidUrl_ReturnsEmpty()
    {
        Website website = new(httpClientFactMock.Object);
        httpMsgHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                nameof(HttpClient.SendAsync),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });
        List<DocumentInfo> docInfos = new();

        await foreach (DocumentInfo doc in website.GetDocumentsAsync(CancellationToken.None))
            docInfos.Add(doc);

        Assert.Empty(docInfos);
    }
}
