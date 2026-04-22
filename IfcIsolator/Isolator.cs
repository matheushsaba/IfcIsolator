using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace IfcIsolator;

public static class Isolator
{
    const string IFC_FILE_EXTENSION = ".ifc";
    const string OUTPUT_FILE_SUFFIX = "_Isolated";

    public static void SplitByEntityLabels(string sourceFilePath, string outputFolderPath, IEnumerable<int> entityLabels)
    {
        if (!Path.Exists(sourceFilePath) || !Path.Exists(outputFolderPath))
        {
            return;
        }

        using (var sourceModel = IfcStore.Open(sourceFilePath))
        {
            var needsIfc4x3Fallback = sourceModel.SchemaVersion == XbimSchemaVersion.Ifc4x3;
            var products = GetProductsByEntityLabel(sourceModel, entityLabels);
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var outputFileName = fileName + OUTPUT_FILE_SUFFIX + IFC_FILE_EXTENSION;
            var outputFilePath = Path.Combine(outputFolderPath, outputFileName);

            var xbimEditorCredentials = new XbimEditorCredentials()
            {
                EditorsFamilyName = Environment.UserName,
                EditorsGivenName = Environment.UserName,
                EditorsOrganisationName = Environment.UserName,
                ApplicationFullName = "Ifc Isolator",
                ApplicationVersion = "1.0.0",
                ApplicationIdentifier = "Ifc Isolator",
                ApplicationDevelopersName = "Matheus Sabadin",
            };

            using (var targetModel = IfcStore.Create(xbimEditorCredentials, ((IModel)sourceModel).SchemaVersion, XbimStoreType.EsentDatabase))
            {
                CopyHeader(sourceModel.Header, targetModel.Header);

                using (sourceModel.BeginEntityCaching())
                using (sourceModel.BeginInverseCaching())
                using (var txn = targetModel.BeginTransaction("InsertCopy with IfcProducts"))
                {
                    var map = new XbimInstanceHandleMap(sourceModel, targetModel);
                    targetModel.CustomInsertCopy(products, true, false, map, true);
                    if (needsIfc4x3Fallback)
                    {
                        Ifc4x3SpatialHierarchyFallback.Restore(targetModel, sourceModel, products, map, sourceFilePath);
                    }

                    txn.Commit();
                }

                targetModel.SaveAs(outputFilePath);
            }
        }
    }

    private static void CopyHeader(IStepFileHeader sourceHeader, IStepFileHeader targetHeader)
    {
        targetHeader.FileDescription = CopyFileDescription(sourceHeader.FileDescription);
        targetHeader.FileName = CopyFileName(sourceHeader.FileName);
        targetHeader.FileSchema = CopyFileSchema(sourceHeader.FileSchema);
    }

    private static IStepFileDescription CopyFileDescription(IStepFileDescription source)
    {
        return new StepFileDescription
        {
            Description = source.Description.ToList(),
            ImplementationLevel = source.ImplementationLevel,
            EntityCount = source.EntityCount,
        };
    }

    private static IStepFileName CopyFileName(IStepFileName source)
    {
        return new StepFileName
        {
            Name = source.Name,
            TimeStamp = source.TimeStamp,
            AuthorName = source.AuthorName.ToList(),
            Organization = source.Organization.ToList(),
            PreprocessorVersion = source.PreprocessorVersion,
            OriginatingSystem = source.OriginatingSystem,
            AuthorizationName = source.AuthorizationName,
            AuthorizationMailingAddress = source.AuthorizationMailingAddress.ToList(),
        };
    }

    private static IStepFileSchema CopyFileSchema(IStepFileSchema source)
    {
        IStepFileSchema schema = new StepFileSchema();
        schema.Schemas = source.Schemas.ToList();
        return schema;
    }

    private static HashSet<IIfcProduct> GetProductsByEntityLabel(IModel model, IEnumerable<int> entityLabels)
    {
        var entityLabelsSet = entityLabels.ToHashSet();
        var collectedProducts = new HashSet<IIfcProduct>();

        var entityProducts = model
            .Instances
            .OfType<IIfcProduct>()
            .Where(x => entityLabelsSet.Contains(x.EntityLabel));

        foreach (var product in entityProducts)
        {
            collectedProducts.Add(product);
            if (product is IIfcSpatialElement)
            {
                collectedProducts.UnionWith(GetProductHierarchyRecursively(product, collectedProducts));
            }
        }

        return collectedProducts;
    }

    private static HashSet<IIfcProduct> GetProductHierarchyRecursively(IIfcObjectDefinition ifcObjectDefinition, HashSet<IIfcProduct> collectedProducts)
    {
        return GetProductHierarchyRecursively(ifcObjectDefinition, collectedProducts, new HashSet<IIfcObjectDefinition>());
    }

    private static HashSet<IIfcProduct> GetProductHierarchyRecursively(
        IIfcObjectDefinition? ifcObjectDefinition,
        HashSet<IIfcProduct> collectedProducts,
        ISet<IIfcObjectDefinition> visitedDefinitions)
    {
        if (ifcObjectDefinition == null)
        {
            return collectedProducts;
        }

        if (!visitedDefinitions.Add(ifcObjectDefinition))
        {
            return collectedProducts;
        }

        // Only spatial elements can contain building elements
        if (ifcObjectDefinition is IIfcSpatialElement spatialElement)
        {
            // Use IfcRelContainedInSpatialElement to retrieve contained elements
            var containedProducts = OrEmpty(spatialElement.ContainsElements).SelectMany(rel => OrEmpty(rel.RelatedElements));
            foreach (var product in containedProducts)
            {
                collectedProducts.Add(product);
            }
        }

        // Use IfcRelAggregares to get the spatial decomposition of spatial structure elements
        var childObjectDefinitions = OrEmpty(ifcObjectDefinition.IsDecomposedBy).SelectMany(rel => OrEmpty(rel.RelatedObjects));
        foreach (var childDefinition in childObjectDefinitions)
        {
            GetProductHierarchyRecursively(childDefinition, collectedProducts, visitedDefinitions);
        }

        return collectedProducts;
    }

    private static IEnumerable<T> OrEmpty<T>(IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }
}
