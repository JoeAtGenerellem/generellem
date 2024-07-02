namespace Generellem.Services;

public interface IGenerellemFiles
{
    string GetAppDataPath(string fileName = "");
}