namespace Generellem.Services;

/// <summary>
/// Manages the location where configuration files go.
/// </summary>
public class GenerellemFiles : IGenerellemFiles
{
    /// <summary>
    /// Allows the caller to set a sub folder for config files.
    /// </summary>
    /// <remarks>
    /// Prevents one instance of Generellem from consuming/overwriting files of another instance.
    /// </remarks>
    public static string SubFolder { get; set; } = string.Empty;

    public string GetAppDataPath(string fileName = "")
    {
        string folder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Generellem",
                SubFolder);

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return Path.Combine(folder, fileName);
    }
}