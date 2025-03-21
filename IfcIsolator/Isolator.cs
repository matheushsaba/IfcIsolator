using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace IfcIsolator
{
    public static class Isolator
    {
        const string IFC_FILE_EXTENSION = ".ifc";
        const string OUTPUT_FILE_SUFFIX = "_Isolated";

        public static void SplitByEntityLabels(string sourceFilePath, string outputFolderPath, int[] entityLabels)
        {
            using (var sourceModel = IfcStore.Open(sourceFilePath))
            {
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
                    using (sourceModel.BeginEntityCaching())
                    using (sourceModel.BeginInverseCaching())
                    using (var txn = targetModel.BeginTransaction("InsertCopy with IfcProducts"))
                    {
                        var map = new XbimInstanceHandleMap(sourceModel, targetModel);
                        targetModel.InsertCopy(products, true, false, map, true);
                        txn.Commit();
                    }
                    targetModel.Header.FileDescription = sourceModel.Header.FileDescription;
                    targetModel.Header.FileName = sourceModel.Header.FileName;
                    targetModel.Header.FileSchema = sourceModel.Header.FileSchema;
                    targetModel.Header.FileName.OriginatingSystem = sourceModel.Header.FileName.OriginatingSystem;
                    targetModel.SaveAs(outputFilePath);
                }
            }
        }

        private static HashSet<IIfcProduct> GetProductsByEntityLabel(IModel model, int[] entityLabels)
        {
            var entityLabelsSet = new HashSet<int>(entityLabels);
            var collectedProducts = new HashSet<IIfcProduct>();

            var entityProducts = model
                .Instances
                .OfType<IIfcProduct>()
                .Where(x => entityLabelsSet.Contains(x.EntityLabel));

            foreach (var product in entityProducts)
            {
                collectedProducts.Add(product);
                if (product is IIfcSpatialStructureElement)
                {
                    collectedProducts.UnionWith(GetProductHierarchyRecursively(product, collectedProducts));
                }
            }

            return collectedProducts;
        }

        private static HashSet<IIfcProduct> GetProductHierarchyRecursively(IIfcObjectDefinition ifcObjectDefinition, HashSet<IIfcProduct> collectedProducts)
        {
            if (ifcObjectDefinition == null)
            {
                return collectedProducts;
            }

            // Only spatial elements can contain building elements
            if (ifcObjectDefinition is IIfcSpatialStructureElement spatialElement)
            {
                // Use IfcRelContainedInSpatialElement to retrieve contained elements
                var containedProducts = spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements);
                foreach (var product in containedProducts)
                {
                    collectedProducts.Add(product);
                }
            }

            // Use IfcRelAggregares to get the spatial decomposition of spatial structure elements
            var childObjectDefinitions = ifcObjectDefinition.IsDecomposedBy.SelectMany(rel => rel.RelatedObjects);
            foreach (var childDefinition in childObjectDefinitions)
            {
                GetProductHierarchyRecursively(childDefinition, collectedProducts);
            }

            return collectedProducts;
        }
    }
}
