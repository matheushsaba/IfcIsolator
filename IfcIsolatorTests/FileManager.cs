namespace IfcIsolatorTests;

internal static class FileManager
{
    public const string Ifc4FolderName = "Ifc4";
    public const string Ifc4x3FolderName = "Ifc4x3";
    public const string Ifc2x3FolderName = "Ifc2x3";

    private const string BasePath = "..\\..\\..\\";
    private const string IfcExtension = ".ifc";
    private const string OutputFileSuffix = "_Isolated";
    private const string TestFilesFolderName = "TestFiles";
    private const string TestFilesOutputFolderName = "TestFilesOutput";

    public static string GetTestFilesFolderPath()
    {
        return Path.Combine(BasePath, TestFilesFolderName);
    }

    public static string GetTestFilesFolderPath(string ifcVersionFolderName)
    {
        return Path.Combine(GetTestFilesFolderPath(), ifcVersionFolderName);
    }

    public static string GetTestFilesOutputFolderPath(string ifcVersionFolderName)
    {
        var outputFolderPath = Path.Combine(GetTestFilesFolderPath(ifcVersionFolderName), TestFilesOutputFolderName);

        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        return outputFolderPath;
    }

    public static string GetIfcTestFilePath(string ifcVersionFolderName, string fileNameWithoutExtension)
    {
        var fileNameWithExtension = $"{fileNameWithoutExtension}{IfcExtension}";
        var filePath = Path.Combine(GetTestFilesFolderPath(ifcVersionFolderName), fileNameWithExtension);

        return filePath;
    }

    public static string GetIfcTestFileOutputPath(string ifcVersionFolderName, string fileNameWithoutExtension)
    {
        var outputPath = GetTestFilesOutputFolderPath(ifcVersionFolderName);
        var outputFileName = $"{fileNameWithoutExtension}{OutputFileSuffix}{IfcExtension}";
        var outputFilePath = Path.Combine(outputPath, outputFileName);

        return outputFilePath;
    }
}