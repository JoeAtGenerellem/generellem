using Generellem.Document;
using Generellem.Document.DocumentTypes;
using Generellem.Services;

using Microsoft.Graph;

using System.Runtime.CompilerServices;
using Microsoft.Graph.Models;

namespace Generellem.DocumentSource;

/// <summary>
/// Supports ingesting documents from a computer file system
/// </summary>
public class OneDriveFileSystem : IDocumentSource
{
    /// <summary>
    /// Describes the document source.
    /// </summary>
    public string Description { get; set; } = "OneDrive File System";

    /// <summary>
    /// Used in the vector DB to uniquely identify the document and where it was ingested from.
    /// </summary>
    public string Prefix { get; set; }

    readonly IEnumerable<string> DocExtensions = DocumentTypeFactory.GetSupportedDocumentTypes();
    readonly string OneDriveErrorMessage = $"Please set {GKeys.OneDriveUserName} via IDynamicConfiguration with the user name for the OneDrive account that we're reading from.";

    readonly string? oneDriveUserName;
    readonly IMSGraphClientFactory msGraphFact;
    readonly IPathProvider pathProvider;

    public OneDriveFileSystem(
        IDynamicConfiguration config, 
        IMSGraphClientFactory msGraphFact,
        IPathProviderFactory pathProviderFact)
    {
        oneDriveUserName = config[GKeys.OneDriveUserName];

        this.msGraphFact = msGraphFact;
        this.pathProvider = pathProviderFact.Create(this);

        Prefix = $"{oneDriveUserName}:{nameof(OneDriveFileSystem)}";
    }

    /// <summary>
    /// Based on the config file, scan files.
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Enumerable of <see cref="DocumentInfo"/>.</returns>
    public async IAsyncEnumerable<DocumentInfo> GetDocumentsAsync([EnumeratorCancellation] CancellationToken cancelToken)
    {
        GraphServiceClient graphClient = await msGraphFact.CreateAsync(Scopes.OneDrive);
        User? user = await graphClient.Me.GetAsync();

        if (user is not null)
            Prefix = $"{user.DisplayName}:{nameof(OneDriveFileSystem)}";

        IEnumerable<PathSpec> fileSpecs = await pathProvider.GetPathsAsync($"{nameof(OneDriveFileSystem)}.json");

        if (fileSpecs is null || fileSpecs.Count() == 0)
            yield break;

        foreach (PathSpec spec in fileSpecs)
        {
            if (spec?.Path is not { } path)
                continue;

            string specDescription = spec.Description ?? string.Empty;

            if (user?.Id is null)
                continue;

            // Get the DriveItem for the given path
            DriveItem? driveItem = await graphClient.Drives[user.Id].Root
                .ItemWithPath(path)
                .GetAsync();

            if (driveItem is null)
                continue;

            // Recursively get all files under the DriveItem
            List<DriveItem> files = await GetFilesRecursively(graphClient, driveItem, user.Id);

            foreach (var file in files)
            {
                string fileName = file.Name ?? string.Empty;
                string folder = file.ParentReference?.Path?.Substring(file.ParentReference.Path.IndexOf(':') + 1) ?? string.Empty;
                string filePath = Path.Combine(folder, fileName);

                IDocumentType docType = DocumentTypeFactory.Create(fileName);

                // Get the stream for each file
                Stream? fileStream = await graphClient.Drives[user.Id].Items[file.Id].Content.GetAsync();
                yield return new DocumentInfo(Prefix, fileStream, docType, filePath, specDescription);

                if (cancelToken.IsCancellationRequested)
                    break;
            }

            if (cancelToken.IsCancellationRequested)
                break;
        }
    }
    
    async Task<List<DriveItem>> GetFilesRecursively(GraphServiceClient graphClient, DriveItem driveItem, string userID)
    {
        List<DriveItem> files = new List<DriveItem>();

        // Check if the current DriveItem is a file
        if (driveItem.Folder == null)
        {
            files.Add(driveItem);
        }
        else
        {
            // If it's a folder, recursively get files in its children
            DriveItemCollectionResponse? children = await graphClient.Drives[userID].Items[driveItem.Id].Children.GetAsync();

            if (children?.Value is null)
                return files;

            foreach (var child in children.Value)
            {
                files.AddRange(await GetFilesRecursively(graphClient, child, userID));
            }
        }

        return files;
    }
}
