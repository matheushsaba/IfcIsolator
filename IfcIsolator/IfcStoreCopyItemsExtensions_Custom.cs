using System.Collections;
using System.Reflection;

using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;

using Ifc4x3RelReferencedInSpatialStructure = Xbim.Ifc4x3.ProductExtension.IfcRelReferencedInSpatialStructure;

namespace IfcIsolator;

public static class IfcStoreCopyItemsExtensions_Custom
{

    /// <summary>
    /// This is a higher level function which uses InsertCopy function alongside with the knowledge of IFC schema to copy over
    /// products with their types and other related information (classification, aggregation, documents, properties) and optionally
    /// geometry. It will also bring in spatial hierarchy relevant to selected products. However, resulting model is not guaranteed 
    /// to be compliant with any Model View Definition unless you explicitly check the compliance. Context of a single product tend to 
    /// consist from hundreds of objects which need to be identified and copied over so this operation might be potentially expensive.
    /// You should never call this function more than once between two models. It not only selects objects to be copied over but also
    /// excludes other objects from being copied over so that it doesn't bring the entire model in a chain dependencies. This means
    /// that some objects are modified (like spatial relations) and won't get updated which would lead to an inconsistent copy.
    /// </summary>
    /// <param name="model">The target model</param>
    /// <param name="products">Products from other model to be inserted into this model</param>
    /// <param name="includeGeometry">If TRUE, geometry of the products will be copied over.</param>
    /// <param name="keepLabels">If TRUE, entity labels from original model will be used. Always set this to FALSE
    /// if you are going to insert products from multiple source models or if you are going to insert products to a non-empty model</param>
    /// <param name="mappings">Mappings to avoid multiple insertion of objects. Keep a single instance for insertion between two models.
    /// If you also use InsertCopy() function for some other insertions, use the same instance of mappings.</param>
    /// <param name="acceptIsolatedSpatialElements">Accept isolated spatial elements to make a copy of their hierarchy.</param>
    public static void CustomInsertCopy(this IModel model, IEnumerable<IIfcProduct> products, bool includeGeometry, bool keepLabels,
        XbimInstanceHandleMap mappings, bool acceptIsolatedSpatialElements = false)
    {
        var context = new CopyContext
        {
            IncludeGeometry = includeGeometry
        };

        var roots = products.Cast<IPersistEntity>().ToList();
        //return if there is nothing to insert
        if (!roots.Any())
            return;

        var source = roots.First().Model;
        if (source == model)
            //don't do anything if the source and target are the same
            return;

        var toInsert = GetEntitiesToInsert(context, source, roots, acceptIsolatedSpatialElements);
        //create new cache is none is defined
        var cache = mappings ?? new XbimInstanceHandleMap(source, model);

        foreach (var entity in toInsert)
            model.InsertCopy(entity, cache,
                (property, obj) => Filter(context, property, obj),
                true, keepLabels);
    }

    private static IEnumerable<IPersistEntity> GetEntitiesToInsert(CopyContext context, IModel model, List<IPersistEntity> roots,
        bool acceptIsolatedSpatialElements)
    {
        context.PrimaryElements = roots.OfType<IIfcProduct>().ToList();

        //add any aggregated elements. For example IfcRoof is typically aggregation of one or more slabs so we need to bring
        //them along to have all the information both for geometry and for properties and materials.
        //This has to happen before we add spatial hierarchy or it would bring in full hierarchy which is not an intention
        var decompositionRels = GetAggregations(context, context.PrimaryElements.ToList(), model).ToList();
        context.PrimaryElements.AddRange(context.Decomposition);
        roots.AddRange(decompositionRels);

        //we should add spatial hierarchy right here so it brings its attributes as well
        var spatialRels = model.Instances.Where<IIfcRelContainedInSpatialStructure>(
            r => r.RelatingStructure != null &&
                 context.PrimaryElements.Any(e => OrEmpty(r.RelatedElements).Contains(e))).ToList();
        var spatialRefs =
            model.Instances.Where<IIfcRelReferencedInSpatialStructure>(
                r => GetReferencedRelatingStructure(r) != null &&
                     context.PrimaryElements.Any(e => GetReferencedRelatedElements(r).Contains(e))).ToList();
        var bottomSpatialHierarchy =
            spatialRels.Select(r => r.RelatingStructure)
                .Union(spatialRefs.Select(GetReferencedRelatingStructure))
                .OfType<IIfcSpatialElement>()
                .ToList();

        if (acceptIsolatedSpatialElements)
        {
            // If the user explicitly gave us a spatial element (storey, building, etc.) as a root,
            // treat it as though it was part of bottomSpatialHierarchy
            var rootSpatial = roots.OfType<IIfcSpatialElement>().ToList();
            bottomSpatialHierarchy.AddRange(rootSpatial);
        }

        var spatialAggregations = GetUpstreamHierarchy(bottomSpatialHierarchy, model).ToList();
        var upstreamSpatialHierarchy = spatialAggregations.Select(r => r.RelatingObject).OfType<IIfcSpatialElement>().ToList();
        var spatialHierarchy = bottomSpatialHierarchy.Union(upstreamSpatialHierarchy).ToList();
        var useIfc4x3SpatialContext = model.SchemaVersion == XbimSchemaVersion.Ifc4x3;
        var spatialContextRefs = useIfc4x3SpatialContext
            ? GetSpatialReferences(spatialHierarchy, model).ToList()
            : new List<IIfcRelReferencedInSpatialStructure>();
        var spatialReferenceProducts = spatialContextRefs.SelectMany(GetReferencedRelatedElements).ToList();
        var directSiteAggregations = useIfc4x3SpatialContext &&
                                     bottomSpatialHierarchy.Any() &&
                                     bottomSpatialHierarchy.All(s => s is IIfcSite)
            ? GetDirectSpatialAggregations(bottomSpatialHierarchy, model).ToList()
            : new List<IIfcRelAggregates>();
        var directSiteChildren = directSiteAggregations
            .SelectMany(r => OrEmpty(r.RelatedObjects))
            .OfType<IIfcSpatialElement>()
            .ToList();

        //add all spatial elements from bottom and from upstream hierarchy
        context.PrimaryElements.AddRange(bottomSpatialHierarchy);
        context.PrimaryElements.AddRange(upstreamSpatialHierarchy);
        context.PrimaryElements.AddRange(spatialReferenceProducts);
        context.PrimaryElements.AddRange(directSiteChildren);
        roots.AddRange(spatialAggregations);
        roots.AddRange(directSiteAggregations);
        roots.AddRange(spatialRels);
        roots.AddRange(spatialRefs);
        roots.AddRange(spatialContextRefs);

        //we should add any feature elements used to subtract mass from a product
        var featureRels = GetFeatureRelations(context.PrimaryElements).ToList();
        var openings = featureRels.Select(r => r.RelatedOpeningElement).Where(p => p != null);
        context.PrimaryElements.AddRange(openings);
        roots.AddRange(featureRels);

        //object types and properties for all primary products (elements and spatial elements)
        roots.AddRange(context.PrimaryElements.SelectMany(p => OrEmpty(p.IsDefinedBy)));
        roots.AddRange(context.PrimaryElements.SelectMany(p => OrEmpty(p.IsTypedBy)));



        //assignmnet to groups will bring in all system aggregarions if defined in the file
        roots.AddRange(context.PrimaryElements.SelectMany(p => OrEmpty(p.HasAssignments)));

        //associations with classification, material and documents
        roots.AddRange(context.PrimaryElements.SelectMany(p => OrEmpty(p.HasAssociations)));

        return roots;
    }

    private static object? Filter(CopyContext context, ExpressMetaProperty property, object parentObject)
    {
        //ignore inverses except for style
        if (property.IsInverse)
            return property.Name == "StyledByItem" ? property.PropertyInfo.GetValue(parentObject, null) : null;

        if (context.PrimaryElements != null && context.PrimaryElements.Any())
        {
            if (typeof(IIfcProduct).IsAssignableFrom(property.PropertyInfo.PropertyType))
            {
                var element = property.PropertyInfo.GetValue(parentObject, null) as IIfcProduct;
                if (element != null && context.PrimaryElements.Contains(element))
                    return element;
                return null;
            }
            if (property.EnumerableType != null && !property.EnumerableType.IsValueType && property.EnumerableType != typeof(string))
            {
                //this can either be a list of IPersistEntity or select type. The very base type is IPersist
                var entities = property.PropertyInfo.GetValue(parentObject, null) as IEnumerable<IPersist>;
                if (entities != null)
                {
                    var persistEntities = entities as IList<IPersist> ?? entities.ToList();
                    var elementsToRemove = persistEntities.OfType<IIfcProduct>().Where(e => !context.PrimaryElements.Contains(e)).ToList();
                    //if there are no IfcElements return what is in there with no care
                    if (elementsToRemove.Any())
                        //return original values excluding elements not included in the primary set
                        return CreateFilteredExpressEnumerable(property, parentObject, entities, persistEntities, elementsToRemove);
                }
            }
        }

        //if geometry is to be included don't filter it out
        if (context.IncludeGeometry)
            return property.PropertyInfo.GetValue(parentObject, null);

        //leave out geometry and placement of products
        if (parentObject is IIfcProduct &&
            (property.PropertyInfo.Name == "Representation" || property.PropertyInfo.Name == "ObjectPlacement")
           )
            return null;

        //leave out representation maps
        if (parentObject is IIfcTypeProduct && property.PropertyInfo.Name == "RepresentationMaps")
            return null;

        //leave out eventual connection geometry
        if (parentObject is IIfcRelSpaceBoundary && property.PropertyInfo.Name == "ConnectionGeometry")
            return null;

        //return the value for anything else
        return property.PropertyInfo.GetValue(parentObject, null);
    }

    private static object CreateFilteredExpressEnumerable(
        ExpressMetaProperty property,
        object parentObject,
        IEnumerable<IPersist> originalEntities,
        IEnumerable<IPersist> persistEntities,
        IEnumerable<IIfcProduct> elementsToRemove)
    {
        var filteredEntities = persistEntities.Except(elementsToRemove).ToList();
        var itemSetType = originalEntities.GetType();

        if (parentObject is not IPersistEntity owningEntity ||
            property.EntityAttribute == null ||
            itemSetType.IsAbstract ||
            !typeof(IExpressEnumerable).IsAssignableFrom(itemSetType))
        {
            return filteredEntities;
        }

        var itemSet = Activator.CreateInstance(
            itemSetType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { owningEntity, filteredEntities.Count, property.EntityAttribute.Order },
            culture: null);

        if (itemSet == null || itemSetType.GetGenericArguments().Length != 1)
            return filteredEntities;

        var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemSetType.GetGenericArguments()[0]))!;
        foreach (var entity in filteredEntities)
            typedList.Add(entity);

        var internalField = itemSetType
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Concat(itemSetType.BaseType?.GetFields(BindingFlags.Instance | BindingFlags.NonPublic) ?? Enumerable.Empty<FieldInfo>())
            .FirstOrDefault(f => f.Name == "<Internal>k__BackingField");

        if (internalField == null)
            return filteredEntities;

        internalField.SetValue(itemSet, typedList);

        return itemSet;
    }

    private static IEnumerable<IIfcRelVoidsElement> GetFeatureRelations(IEnumerable<IIfcProduct> products)
    {
        var elements = products.OfType<IIfcElement>().ToList();
        if (!elements.Any()) yield break;
        var model = elements.First().Model;
        var rels = model.Instances.Where<IIfcRelVoidsElement>(r => elements.Any(e => Equals(e, r.RelatingBuildingElement)));
        foreach (var rel in rels)
            yield return rel;
    }

    private static IEnumerable<IIfcRelDecomposes> GetAggregations(CopyContext context, List<IIfcProduct> products, IModel model)
    {
        context.Decomposition.Clear();
        var processedProducts = new HashSet<IIfcProduct>();
        while (true)
        {
            products = products.Where(processedProducts.Add).ToList();
            if (!products.Any())
                yield break;

            var products1 = products;
            var rels = model.Instances.Where<IIfcRelDecomposes>(r =>
            {
                if (r is IIfcRelAggregates aggr)
                    return products1.Any(p => Equals(aggr.RelatingObject, p));
                if (r is IIfcRelNests nest)
                    return products1.Any(p => Equals(nest.RelatingObject, p));
                if (r is IIfcRelProjectsElement prj)
                    return products1.Any(p => Equals(prj.RelatingElement, p));
                if (r is IIfcRelVoidsElement voids)
                    return products1.Any(p => Equals(voids.RelatingBuildingElement, p));
                return false;

            }).ToList();
            var relatedProducts = rels.SelectMany(r =>
            {
                if (r is IIfcRelAggregates aggr)
                    return OrEmpty(aggr.RelatedObjects).OfType<IIfcProduct>();
                if (r is IIfcRelNests nest)
                    return OrEmpty(nest.RelatedObjects).OfType<IIfcProduct>();
                if (r is IIfcRelProjectsElement prj)
                    return OneOrEmpty(prj.RelatedFeatureElement);
                if (r is IIfcRelVoidsElement voids)
                    return OneOrEmpty(voids.RelatedOpeningElement);
                return Enumerable.Empty<IIfcProduct>();
            }).Where(p => p != null).ToList();

            foreach (var rel in rels)
                yield return rel;

            products = relatedProducts;
            context.Decomposition.AddRange(products);
        }
    }

    private static IEnumerable<IIfcRelAggregates> GetUpstreamHierarchy(IEnumerable<IIfcSpatialElement> spatialStructureElements, IModel model)
    {
        var processedElements = new HashSet<IIfcSpatialElement>();
        while (true)
        {
            var elements = spatialStructureElements.Where(processedElements.Add).ToList();
            if (!elements.Any())
                yield break;

            var rels = model.Instances.Where<IIfcRelAggregates>(r =>
                r.RelatingObject != null &&
                elements.Any(s => OrEmpty(r.RelatedObjects).Contains(s))).ToList();
            var decomposing = rels.Select(r => r.RelatingObject).OfType<IIfcSpatialElement>();

            foreach (var rel in rels)
                yield return rel;

            spatialStructureElements = decomposing;
        }
    }

    private static IEnumerable<IIfcRelAggregates> GetDirectSpatialAggregations(IEnumerable<IIfcSpatialElement> spatialStructureElements, IModel model)
    {
        var elements = spatialStructureElements.ToList();
        if (!elements.Any())
            yield break;

        var rels = model.Instances.Where<IIfcRelAggregates>(r =>
            r.RelatingObject != null &&
            elements.Any(s => Equals(r.RelatingObject, s)) &&
            OrEmpty(r.RelatedObjects).OfType<IIfcSpatialElement>().Any()).ToList();

        foreach (var rel in rels)
            yield return rel;
    }

    private static IEnumerable<IIfcRelReferencedInSpatialStructure> GetSpatialReferences(IEnumerable<IIfcSpatialElement> spatialStructureElements, IModel model)
    {
        var elements = spatialStructureElements.ToList();
        if (!elements.Any())
            yield break;

        var rels = model.Instances.Where<IIfcRelReferencedInSpatialStructure>(r =>
            GetReferencedRelatingStructure(r) != null &&
            elements.Any(s => Equals(GetReferencedRelatingStructure(r), s))).ToList();

        foreach (var rel in rels)
            yield return rel;
    }

    private static IEnumerable<T> OrEmpty<T>(IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    private static IEnumerable<IIfcProduct> GetReferencedRelatedElements(IIfcRelReferencedInSpatialStructure relation)
    {
        try
        {
            return OrEmpty(relation.RelatedElements);
        }
        catch (NotImplementedException) when (relation is Ifc4x3RelReferencedInSpatialStructure ifc4x3Relation)
        {
            return OrEmpty(ifc4x3Relation.RelatedElements).OfType<IIfcProduct>();
        }
    }

    private static IIfcSpatialElement? GetReferencedRelatingStructure(IIfcRelReferencedInSpatialStructure relation)
    {
        try
        {
            return relation.RelatingStructure;
        }
        catch (NotImplementedException) when (relation is Ifc4x3RelReferencedInSpatialStructure ifc4x3Relation)
        {
            return ifc4x3Relation.RelatingStructure;
        }
    }

    private static IEnumerable<IIfcProduct> OneOrEmpty(IIfcProduct? product)
    {
        return product == null ? Enumerable.Empty<IIfcProduct>() : new[] { product };
    }

    private class CopyContext
    {
        public List<IIfcProduct>? PrimaryElements { get; set; } = new List<IIfcProduct>();
        public List<IIfcProduct> Decomposition { get; private set; } = new List<IIfcProduct>();
        public bool IncludeGeometry { get; set; }
    }
}