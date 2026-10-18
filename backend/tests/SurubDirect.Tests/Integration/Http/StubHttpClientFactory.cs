namespace SurubDirect.Tests;
public sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> respond) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new StubHttpHandler(respond))
    {
        BaseAddress = new Uri("http://service.test")
    };
}
