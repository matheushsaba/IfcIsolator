using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcIsolatorTests
{
    internal static class FileManager
    {
        const string BASE_PATH = "..\\..\\..\\";
        const string IFC_EXTENSION = ".ifc";
        const string OUTPUT_FILE_SUFFIX = "_Isolated";
        const string TEST_FILES_FOLDER_NAME = "TestFiles";
        const string TEST_FILES_OUTPUT_FOLDER_NAME = "TestFilesOutput";

        public static string GetTestFilesFolderPath()
        {
            return Path.Combine(BASE_PATH, TEST_FILES_FOLDER_NAME);
        }

        public static string GetTestFilesOutputFolderPath()
        {
            var outputFolderPath = Path.Combine(GetTestFilesFolderPath(), TEST_FILES_OUTPUT_FOLDER_NAME);

            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            return outputFolderPath;
        }

        public static string GetIfcTestFilePath(string fileNameWithoutExtension)
        {
            var fileNameWithExtension = $"{fileNameWithoutExtension}{IFC_EXTENSION}";
            var filePath = Path.Combine(GetTestFilesFolderPath(), fileNameWithExtension);

            return filePath;
        }

        public static string GetIfcTestFileOutputPath(string fileNameWithoutExtension)
        {
            var outputPath = GetTestFilesOutputFolderPath();
            var outputFileName = $"{fileNameWithoutExtension}{OUTPUT_FILE_SUFFIX}{IFC_EXTENSION}";
            var outputFilePath = Path.Combine(outputPath, outputFileName);

            return outputFilePath;
        }
    }
}
