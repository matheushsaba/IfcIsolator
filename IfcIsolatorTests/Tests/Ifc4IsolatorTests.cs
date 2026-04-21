using FluentAssertions;
using IfcIsolator;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcIsolatorTests.Tests;

public class Ifc4IsolatorTests
{
    [Fact]
    public void Isolate_Wall()
    {
        var fileName = "Ifc4_Revit_STR";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4FolderName);
        var entityLabels = new int[] { 328 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4FolderName, fileName);
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
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4FolderName);
        var entityLabels = new int[] { 257 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4FolderName, fileName);
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
    public void Isolate_Space_2()
    {
        var fileName = "DigitalHub_FM-ARC_v2";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4FolderName);
        var entityLabels = new int[] { 6967 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4FolderName, fileName);
        using (var resultingModel = IfcStore.Open(outputFilePath))
        {
            var products = resultingModel
                .Instances
                .OfType<IIfcSpace>()
                .ToList();

            products.Count.Should().Be(1);
            products.First().GlobalId.ToString().Should().Be("3DbdJyICv8GP1HEYzu7Wjc");
        }
    }

    [Fact]
    public void Isolate_MultipleWalls()
    {
        var fileName = "Ifc4_Revit_STR";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4FolderName);
        var entityLabels = new int[] { 328, 446 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4FolderName, fileName);
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