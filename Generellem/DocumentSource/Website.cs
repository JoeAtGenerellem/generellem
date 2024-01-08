using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Generellem.Document.DocumentTypes;
using Generellem.Services;

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

namespace Generellem.DocumentSource;

/// <summary>
/// Manages ingestion of a website
/// </summary>
public class Website(
    IHttpClientFactory httpFact, ILogger<Website> logger) 
    : IDocumentSource
{
    public string Prefix { get; init; } = $"{Environment.MachineName}:{nameof(Website)}";

    readonly HttpClient httpClient = httpFact.Create();
    readonly ILogger<Website> logger = logger;

    /// <summary>
    /// Iteratively visits the page of each website for caller processing
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>foreachable sequence of <see cref="DocumentInfo"/></returns>
    public async IAsyncEnumerable<DocumentInfo> GetDocumentsAsync([EnumeratorCancellation] CancellationToken cancelToken)
    {
        IEnumerable<WebSpec> websites = await GetWebsitesAsync();

        foreach (WebSpec website in websites)
        {
            if (website?.Url is null) continue;

            await foreach (DocumentInfo page in GetPagesAsync(website.Url, cancelToken))
                yield return page;
        }
    }

    static async Task<IEnumerable<WebSpec>> GetWebsitesAsync()
    {
        using var fileStream = File.OpenRead("Websites.json");

        IEnumerable<WebSpec>? websites = await JsonSerializer.DeserializeAsync<IEnumerable<WebSpec>>(fileStream);

        return websites ?? new List<WebSpec>();
    }

    async IAsyncEnumerable<DocumentInfo> GetPagesAsync(string url, [EnumeratorCancellation] CancellationToken cancelToken)
    {
        var html = new Html();

        var queue = new Queue<string>();
        queue.Enqueue(url);

        while (queue.Count is not 0 && !cancelToken.IsCancellationRequested)
        {
            var currentUrl = queue.Dequeue();

            string? htmlDocument = null;

            try
            {
                htmlDocument = await GetHtmlDocumentAsync(currentUrl, cancelToken);
            }
            catch (HttpRequestException httpReqEx)
            {
                logger.LogWarning(GenerellemLogEvents.HttpError, httpReqEx, "Unable to process URL: {CurrentURL}", currentUrl);
            }

            if (string.IsNullOrEmpty(htmlDocument))
                continue;

            MemoryStream memStream = new(Encoding.UTF8.GetBytes(htmlDocument))
            {
                Position = 0
            };

            yield return new DocumentInfo(Prefix, memStream, html, currentUrl);

            var links = GetLinks(htmlDocument, url);

            foreach (var link in links)
                queue.Enqueue(link);
        }
    }

    async Task<string> GetHtmlDocumentAsync(string url, CancellationToken cancelToken)
    {
        var response = await httpClient.GetAsync(url, cancelToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancelToken);
    }

    static List<string> GetLinks(string htmlDocument, string baseUrl)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlDocument);

        List<string> links = 
            (from node in doc.DocumentNode.SelectNodes("//a[@href]")
             let href = node.GetAttributeValue("href", null)
             where href != null && href.StartsWith(baseUrl)
             select href)
            .ToList();

        return links;
    }
}