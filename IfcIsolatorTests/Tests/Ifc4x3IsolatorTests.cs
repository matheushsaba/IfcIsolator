using FluentAssertions;
using IfcIsolator;
using Xbim.Common.ExpressValidation;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IfcIsolatorTests.Tests;

public class Ifc4x3IsolatorTests
{
    private const string Ifc4x3RoadFileName = "KIT-Simple-Road-Test-Web-IFC4x3_RC2";

    [Fact]
    public void Isolate_Pavement()
    {
        var fileName = Ifc4x3RoadFileName;
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 1417 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4x3FolderName, fileName);
        using (var resultingModel = IfcStore.Open(outputFilePath))
        {
            var products = resultingModel
                .Instances
                .OfType<IIfcProduct>()
                .ToList();

            products.Should().HaveCount(5);
            products.Should().ContainSingle(x => x is IIfcSite);
            products.Should().ContainSingle(x => x.GetType().Name == "IfcRoad");
            products.Where(x => x.GetType().Name == "IfcFacilityPartCommon").Should().HaveCount(2);
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "2dG5Zfp9f3Gx9LmmVqVMZE");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();

            var pavement = products.Single(x => x.GlobalId.ToString() == "2dG5Zfp9f3Gx9LmmVqVMZE");
            var containment = resultingModel
                .Instances
                .OfType<IIfcRelContainedInSpatialStructure>()
                .Single(x => x.RelatedElements.Contains(pavement));
            containment.RelatingStructure.Name.ToString().Should().Be("Road-Carriageway-01");
        }
    }
    
    [Fact]
    public void Isolate_Kerb()
    {
        var fileName = Ifc4x3RoadFileName;
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 2675 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4x3FolderName, fileName);
        using (var resultingModel = IfcStore.Open(outputFilePath))
        {
            var products = resultingModel
                .Instances
                .OfType<IIfcProduct>()
                .ToList();

            products.Should().HaveCount(5);
            products.Should().ContainSingle(x => x is IIfcSite);
            products.Should().ContainSingle(x => x.GetType().Name == "IfcKerb");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "2I54c_0nH5S8cFA5zYikNn");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Ifc4x3_ExportsValidModel()
    {
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, Ifc4x3RoadFileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 1417 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4x3FolderName, Ifc4x3RoadFileName);
        using var resultingModel = IfcStore.Open(outputFilePath);
        var validationResults = new Validator()
            .Validate(resultingModel)
            .ToList();

        validationResults.Should().BeEmpty();
    }
}
