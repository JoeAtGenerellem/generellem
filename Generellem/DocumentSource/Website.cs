using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using Generellem.Document.DocumentTypes;
using Generellem.Services;

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using IHttpClientFactory = Generellem.Services.IHttpClientFactory;

namespace Generellem.DocumentSource;

/// <summary>
/// Manages ingestion of a website
/// </summary>
public class Website : IDocumentSource, IWebsite
{
    readonly string appPath = string.Empty;

    /// <summary>
    /// Describes the document source.
    /// </summary>
    public string Description { get; set; } = "Web Site";

    public string Prefix { get; init; } = $"{Environment.MachineName}:{nameof(Website)}";

    readonly HttpClient httpClient;
    readonly ILogger<Website> logger;

    public Website(IHttpClientFactory httpFact, ILogger<Website> logger)
    {
        this.appPath = Path.GetDirectoryName(Environment.ProcessPath)!;
        this.httpClient = httpFact.Create();
        this.logger = logger;
    }

    /// <summary>
    /// Iteratively visits the page of each website for caller processing
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>foreachable sequence of <see cref="DocumentInfo"/></returns>
    public async IAsyncEnumerable<DocumentInfo> GetDocumentsAsync([EnumeratorCancellation] CancellationToken cancelToken)
    {
        IEnumerable<WebSpec> websites = await GetWebsitesAsync();

        foreach (WebSpec spec in websites)
        {
            if (spec?.Url is null) continue;

            string specDescription = spec.Description ?? string.Empty;

            await foreach (DocumentInfo page in GetPagesAsync(spec.Url, specDescription, cancelToken))
                yield return page;
        }
    }

    public virtual async Task<IEnumerable<WebSpec>> GetWebsitesAsync(string configPath = nameof(Website) + ".json")
    {
        configPath = Path.Combine(appPath, configPath);

        if (!File.Exists(configPath))
            using (FileStream specWriter = File.OpenWrite(configPath))
                await specWriter.WriteAsync(new ReadOnlyMemory<byte>(Encoding.Default.GetBytes("[]")));

        using var specReader = File.OpenRead(configPath);

        IEnumerable<WebSpec>? webSpecs = await JsonSerializer.DeserializeAsync<IEnumerable<WebSpec>>(specReader);

        return webSpecs ?? new List<WebSpec>();
    }

    /// <summary>
    /// Writes web URLs to the config file.
    /// </summary>
    /// <param name="webSpecs">Enumerable of <see cref="WebSpec"/>.</param>
    /// <param name="configPath">Location of the config file.</param>
    public virtual async ValueTask WriteSitesAsync(IEnumerable<WebSpec> webSpecs, string configPath = nameof(Website) + ".json")
    {
        configPath = Path.Combine(appPath, configPath);

        File.Delete(configPath);

        using FileStream specWriter = File.OpenWrite(configPath);

        string specJson = JsonSerializer.Serialize(webSpecs, new JsonSerializerOptions() { WriteIndented = true });
        byte[] specBytes = Encoding.Default.GetBytes(specJson);
        ReadOnlyMemory<byte> specMem = new(specBytes);

        await specWriter.WriteAsync(specMem);
    }

    async IAsyncEnumerable<DocumentInfo> GetPagesAsync(string url, string specDescription, [EnumeratorCancellation] CancellationToken cancelToken)
    {
        HashSet<string> alreadyVisited = new();

        Html html = new();

        Queue<string> queue = new();
        queue.Enqueue(url);

        while (queue.Count is not 0 && !cancelToken.IsCancellationRequested)
        {
            string currentUrl = queue.Dequeue();

            if (alreadyVisited.Contains(currentUrl))
                continue;

            alreadyVisited.Add(currentUrl);

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

            yield return new DocumentInfo(Prefix, memStream, html, currentUrl, specDescription);

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

        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//a[@href]");

        if (nodes == null)
            return new List<string>();

        List<string> links =
            (from node in nodes
             let href = node.GetAttributeValue("href", null)
             where href != null && href.StartsWith(baseUrl)
             select href)
            .ToList();

        return links;
    }
}