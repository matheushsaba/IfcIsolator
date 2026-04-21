using FluentAssertions;
using IfcIsolator;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcIsolatorTests.Tests;

public class Ifc2x3IsolatorTests
{
    [Fact]
    public void Isolate_Space()
    {
        var fileName = "Duplex_A_20110907_optimized";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc2x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc2x3FolderName);
        var entityLabels = new int[] { 349 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc2x3FolderName, fileName);
        using (var resultingModel = IfcStore.Open(outputFilePath))
        {
            var products = resultingModel
                .Instances
                .OfType<IIfcSpace>()
                .ToList();

            products.Count.Should().Be(1);
            products.First().GlobalId.ToString().Should().Be("0pNy6pOyf7JPmXRLgxs3sW");
        }
    }
    
    [Fact]
    public void Isolate_SpaceWithElementsInside()
    {
        var fileName = "Duplex_A_20110907_optimized";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc2x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc2x3FolderName);
        var entityLabels = new int[] { 96 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc2x3FolderName, fileName);
        using (var resultingModel = IfcStore.Open(outputFilePath))
        {
            var products = resultingModel
                .Instances
                .OfType<IIfcProduct>()
                .ToList();

            products.Count.Should().Be(10);
        }
    }
}