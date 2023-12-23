namespace Generellem.Services.Tests;

public class HttpClientFactoryTests
{
    readonly HttpClientFactory httpClientFact;

    public HttpClientFactoryTests()
    {
        httpClientFact = new HttpClientFactory();
    }

    [Fact]
    public void Create_ReturnsHttpClient()
    {
        HttpClient result = httpClientFact.Create();

        Assert.IsType<HttpClient>(result);
    }

    [Fact]
    public void Create_ReturnsNewInstance()
    {
        HttpClient client1 = httpClientFact.Create();
        HttpClient client2 = httpClientFact.Create();

        Assert.NotSame(client1, client2);
    }
}
