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
    public void Isolate_Beam_InsideIfcBridgePart()
    {
        var fileName = "Viadotto Acerno";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 165163 };

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
            products.Should().ContainSingle(x => x.GetType().Name == "IfcBeam");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "2XHebXUVj4ZxsWseaIlu7q");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Isolate_Course_InsideIfcRoad()
    {
        var fileName = "Viadotto Acerno";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 193612 };

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
            products.Should().ContainSingle(x => x.GetType().Name == "IfcCourse");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "3pK88A2Dj2tPAsyMR9fiaK");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Isolate_Railing_InsideIfcRoadPart()
    {
        var fileName = "Viadotto Acerno";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 193612 };

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
            products.Should().ContainSingle(x => x.GetType().Name == "IfcCourse");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "3pK88A2Dj2tPAsyMR9fiaK");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Isolate_EarthworksFill_InsideEarthworks()
    {
        var fileName = "Viadotto Acerno";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 159322 };

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
            products.Should().ContainSingle(x => x.GetType().Name == "IfcEarthworksFill");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "1xO3w10Cv45RbSJ7yyiuCQ");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Isolate_GeographicElement()
    {
        var fileName = "Viadotto Acerno";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 159309 };

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
            products.Should().ContainSingle(x => x.GetType().Name == "IfcGeographicElement");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "3ChX9QQBn9Bx2PopL0MnPz");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Isolate_BuiltElement_InsideIfcBridge()
    {
        var fileName = "L4-BR01-PrecastAndCastConcrete-IowaDOT-ALLPLAN-V01";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 100350 };

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
            products.Should().ContainSingle(x => x.GetType().Name == "IfcBuiltElement");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "2zDrLTCUjC_9Wbao$WoDym");

            resultingModel.Instances.OfType<IIfcProject>().Should().ContainSingle();
        }
    }
    
    [Fact]
    public void Isolate_BuiltElement_NoIfcSite()
    {
        var fileName = "L4-BR02-ConcreteCulvert-NCDOT-ALLPLAN-V01";
        var sourceFilePath = FileManager.GetIfcTestFilePath(FileManager.Ifc4x3FolderName, fileName);
        var outputFolderPath = FileManager.GetTestFilesOutputFolderPath(FileManager.Ifc4x3FolderName);
        var entityLabels = new int[] { 4985 };

        Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);

        var outputFilePath = FileManager.GetIfcTestFileOutputPath(FileManager.Ifc4x3FolderName, fileName);
        using (var resultingModel = IfcStore.Open(outputFilePath))
        {
            var products = resultingModel
                .Instances
                .OfType<IIfcProduct>()
                .ToList();

            products.Should().HaveCount(3);
            products.Should().NotContain(x => x is IIfcSite);
            products.Should().ContainSingle(x => x.GetType().Name == "IfcBuiltElement");
            products.Should().ContainSingle(x => x.GlobalId.ToString() == "3Xd16ktuP91Qj9khP11Um5");

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
