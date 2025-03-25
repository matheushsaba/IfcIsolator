using FluentAssertions;

using IfcIsolator;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace IfcIsolatorTests
{
    public class IsolatorTests
    {
        [Fact]
        public void Isolate_Wall()
        {
            // ConsoleRunner.exe "C:\\Users\\Matheus\\Desktop\\TestFiles\\Ifc4_Revit_STR.ifc" "C:\\Users\\Matheus\\Desktop\\TestFiles" 328
            var fileName = "Ifc4_Revit_STR";
            var sourceFilePath = FileManager.GetIfcTestFilePath(fileName);
            var outputFolderPath = FileManager.GetTestFilesOutputFolderPath();
            var entityLabels = new int[] { 328 };

            Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

            var outputFilePath = FileManager.GetIfcTestFileOutputPath(fileName);
            using (var resultingModel = IfcStore.Open(outputFilePath))
            {
                var products = resultingModel
                    .Instances
                    .OfType<IIfcProduct>()
                    .Where(x => x is not IIfcSite)
                    .ToList();

                products.Count.Should().Be(1);
                products.First().GlobalId.ToString().Should().Be("0NWseyvsH7_gBW225aGtuD");
            }
        }

        [Fact]
        public void Isolate_Space()
        {
            var fileName = "Ifc4_Revit_STR";
            var sourceFilePath = FileManager.GetIfcTestFilePath(fileName);
            var outputFolderPath = FileManager.GetTestFilesOutputFolderPath();
            var entityLabels = new int[] { 257 };

            Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

            var outputFilePath = FileManager.GetIfcTestFileOutputPath(fileName);
            using (var resultingModel = IfcStore.Open(outputFilePath))
            {
                var products = resultingModel
                    .Instances
                    .OfType<IIfcProduct>()
                    .Where(x => x is not IIfcSite)
                    .ToList();

                products.Count.Should().Be(1);
                products.First().GlobalId.ToString().Should().Be("0wWrXKmlH9o9quybISZT88");
            }
        }

        [Fact]
        public void Isolate_MultipleWalls()
        {
            var fileName = "Ifc4_Revit_STR";
            var sourceFilePath = FileManager.GetIfcTestFilePath(fileName);
            var outputFolderPath = FileManager.GetTestFilesOutputFolderPath();
            var entityLabels = new int[] { 328, 446 };

            Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

            var outputFilePath = FileManager.GetIfcTestFileOutputPath(fileName);
            using (var resultingModel = IfcStore.Open(outputFilePath))
            {
                var products = resultingModel
                    .Instances
                    .OfType<IIfcProduct>()
                    .Where(x => x is not IIfcSite)
                    .ToList();

                products.Count.Should().Be(2);

                var productGuids = new HashSet<string>
                {
                    "0NWseyvsH7_gBW225aGtuD",
                    "0NWseyvsH7_gBW225aGtyM"
                };

                products.Select(x => x.GlobalId.ToString()).ToHashSet().SetEquals(productGuids).Should().BeTrue();
            }
        }
    }
}