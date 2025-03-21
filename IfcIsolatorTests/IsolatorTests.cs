using IfcIsolator;

namespace IfcIsolatorTests
{
    public class IsolatorTests
    {
        [Fact]
        public void Test1()
        {
            // ConsoleRunner.exe "C:\\Users\\Matheus\\Desktop\\TestFiles\\Ifc4_Revit_STR.ifc" "C:\\Users\\Matheus\\Desktop\\TestFiles" 328
            var sourcePath = "C:\\Users\\Matheus\\Desktop\\TestFiles\\Ifc4_Revit_STR.ifc";
            var outputPath = "C:\\Users\\Matheus\\Desktop\\TestFiles";
            var entityLabels = new int[] { 328 };

            if (!Path.Exists(sourcePath))
            {
                return;
            }


            Isolator.SplitByEntityLabels(sourcePath, outputPath, entityLabels);
        }
    }
}