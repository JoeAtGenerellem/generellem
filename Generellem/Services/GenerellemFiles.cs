namespace Generellem.Services;

public class GenerellemFiles
{
    public static string GetAppDataPath(string fileName = "")
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Generellem",
            fileName);
    }
}