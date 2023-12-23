namespace Generellem.Services;

public class HttpClientFactory : IHttpClientFactory
{
    public HttpClient Create() => new HttpClient();
}
