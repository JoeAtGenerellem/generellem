namespace Generellem.Processors;

/// <summary>
/// Data for ingestion progress reports.
/// </summary>
/// <param name="Message">Description of what is happening.</param>
/// <param name="CurrentCount">Where are we in the process for a data source.</param>
public record IngestionProgress(string Message, int CurrentCount = 0);
