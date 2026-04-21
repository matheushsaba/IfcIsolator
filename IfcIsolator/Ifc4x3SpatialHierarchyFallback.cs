using System.Text.RegularExpressions;

using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Step21;

using Ifc4x3FacilityPartCommon = Xbim.Ifc4x3.ProductExtension.IfcFacilityPartCommon;
using Ifc4x3FacilityUsageEnum = Xbim.Ifc4x3.ProductExtension.IfcFacilityUsageEnum;
using Ifc4x3ObjectDefinition = Xbim.Ifc4x3.Kernel.IfcObjectDefinition;
using Ifc4x3Product = Xbim.Ifc4x3.Kernel.IfcProduct;
using Ifc4x3RelAggregates = Xbim.Ifc4x3.Kernel.IfcRelAggregates;
using Ifc4x3RelContainedInSpatialStructure = Xbim.Ifc4x3.ProductExtension.IfcRelContainedInSpatialStructure;
using Ifc4x3SpatialElement = Xbim.Ifc4x3.ProductExtension.IfcSpatialElement;

namespace IfcIsolator;

internal static class Ifc4x3SpatialHierarchyFallback
{
    private static readonly Regex EntityRegex = new(@"^\s*#(?<label>\d+)\s*=\s*(?<type>[A-Z0-9_]+)\s*\((?<args>.*)\)\s*;\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Restore(
        IModel targetModel,
        IModel sourceModel,
        IEnumerable<IIfcProduct> selectedProducts,
        XbimInstanceHandleMap map,
        string sourceFilePath)
    {
        if (sourceModel.SchemaVersion != XbimSchemaVersion.Ifc4x3 || !File.Exists(sourceFilePath))
            return;

        var entities = ReadStepEntities(sourceFilePath);
        var hierarchyIndex = BuildHierarchyIndex(entities.Values);
        var hierarchy = BuildHierarchy(sourceModel, selectedProducts, entities, hierarchyIndex);

        if (!hierarchy.NeedsFallback)
            return;

        CopyParsedAncestors(targetModel, hierarchy.ParsedAncestorLabels, sourceModel, map);
        var createdEntities = CreateMissingFacilityParts(targetModel, hierarchy.MissingFacilityParts);
        CreateHierarchyRelations(targetModel, hierarchy.Links, sourceModel, map, createdEntities);
    }

    private static HierarchyBuildResult BuildHierarchy(
        IModel sourceModel,
        IEnumerable<IIfcProduct> selectedProducts,
        IReadOnlyDictionary<int, StepEntity> entities,
        StepHierarchyIndex hierarchyIndex)
    {
        var links = new List<HierarchyLink>();
        var parsedAncestorLabels = new HashSet<int>();
        var missingFacilityPartLabels = new HashSet<int>();

        foreach (var product in selectedProducts)
        {
            var currentLabel = product.EntityLabel;
            if (!hierarchyIndex.ContainmentByChild.TryGetValue(currentLabel, out var containment))
                continue;

            links.Add(new HierarchyLink(HierarchyLinkKind.Contains, containment.Relation.Label,
                containment.ParentLabel, currentLabel));
            TrackAncestor(containment.ParentLabel);

            currentLabel = containment.ParentLabel;
            var visitedLabels = new HashSet<int>();
            while (visitedLabels.Add(currentLabel) &&
                   hierarchyIndex.AggregationByChild.TryGetValue(currentLabel, out var aggregate))
            {
                links.Add(new HierarchyLink(HierarchyLinkKind.Aggregates, aggregate.Relation.Label,
                    aggregate.ParentLabel, currentLabel));
                TrackAncestor(aggregate.ParentLabel);
                currentLabel = aggregate.ParentLabel;
            }
        }

        return new HierarchyBuildResult(
            links.Distinct().ToList(),
            parsedAncestorLabels,
            missingFacilityPartLabels.Select(label => CreateFacilityPartData(label, entities[label])).ToList());

        void TrackAncestor(int label)
        {
            if (!entities.TryGetValue(label, out var entity))
                return;

            if (sourceModel.Instances[label] != null)
            {
                parsedAncestorLabels.Add(label);
                return;
            }

            if (entity.Type.Equals("IFCFACILITYPART", StringComparison.OrdinalIgnoreCase))
                missingFacilityPartLabels.Add(label);
        }
    }

    private static StepHierarchyIndex BuildHierarchyIndex(IEnumerable<StepEntity> entities)
    {
        var containmentByChild = new Dictionary<int, (StepEntity Relation, int ParentLabel)>();
        var aggregationByChild = new Dictionary<int, (StepEntity Relation, int ParentLabel)>();

        foreach (var entity in entities)
        {
            if (entity.Type.Equals("IFCRELCONTAINEDINSPATIALSTRUCTURE", StringComparison.OrdinalIgnoreCase))
            {
                if (entity.Arguments.Count < 6)
                    continue;

                var parentLabel = ExtractSingleReference(entity.Arguments[5]);
                if (!parentLabel.HasValue)
                    continue;

                foreach (var childLabel in ExtractReferences(entity.Arguments[4]))
                    containmentByChild.TryAdd(childLabel, (entity, parentLabel.Value));

                continue;
            }

            if (entity.Type.Equals("IFCRELAGGREGATES", StringComparison.OrdinalIgnoreCase))
            {
                if (entity.Arguments.Count < 6)
                    continue;

                var parentLabel = ExtractSingleReference(entity.Arguments[4]);
                if (!parentLabel.HasValue)
                    continue;

                foreach (var childLabel in ExtractReferences(entity.Arguments[5]))
                    aggregationByChild.TryAdd(childLabel, (entity, parentLabel.Value));
            }
        }

        return new StepHierarchyIndex(containmentByChild, aggregationByChild);
    }

    private static void CopyParsedAncestors(
        IModel targetModel,
        IEnumerable<int> ancestorLabels,
        IModel sourceModel,
        XbimInstanceHandleMap map)
    {
        foreach (var label in ancestorLabels)
        {
            var sourceEntity = sourceModel.Instances[label];
            if (sourceEntity == null || map.ContainsKey(new XbimInstanceHandle(sourceEntity)))
                continue;

            targetModel.InsertCopy(sourceEntity, map, CopyDirectProperties, true, false);
        }
    }

    private static Dictionary<int, IPersistEntity> CreateMissingFacilityParts(
        IModel targetModel,
        IEnumerable<FacilityPartData> facilityParts)
    {
        var createdEntities = new Dictionary<int, IPersistEntity>();

        foreach (var facilityPart in facilityParts)
        {
            var targetFacilityPart = targetModel.Instances.New<Ifc4x3FacilityPartCommon>();
            targetFacilityPart.GlobalId = facilityPart.GlobalId ?? StepGuidHelper.ToPart21(Guid.NewGuid());
            targetFacilityPart.Name = facilityPart.Name;
            targetFacilityPart.Description = facilityPart.Description;
            targetFacilityPart.CompositionType = Xbim.Ifc4x3.ProductExtension.IfcElementCompositionEnum.ELEMENT;
            targetFacilityPart.UsageType = facilityPart.UsageType;

            createdEntities.Add(facilityPart.Label, targetFacilityPart);
        }

        return createdEntities;
    }

    private static void CreateHierarchyRelations(
        IModel targetModel,
        IEnumerable<HierarchyLink> links,
        IModel sourceModel,
        XbimInstanceHandleMap map,
        IReadOnlyDictionary<int, IPersistEntity> createdEntities)
    {
        foreach (var link in links.Where(l => l.Kind == HierarchyLinkKind.Aggregates))
        {
            if (GetTargetEntity(link.ParentLabel, sourceModel, map, createdEntities) is not Ifc4x3ObjectDefinition parent ||
                GetTargetEntity(link.ChildLabel, sourceModel, map, createdEntities) is not Ifc4x3ObjectDefinition child)
            {
                continue;
            }

            var relation = targetModel.Instances.New<Ifc4x3RelAggregates>();
            relation.GlobalId = StepGuidHelper.ToPart21(Guid.NewGuid());
            relation.RelatingObject = parent;
            relation.RelatedObjects.Add(child);
        }

        foreach (var link in links.Where(l => l.Kind == HierarchyLinkKind.Contains))
        {
            if (GetTargetEntity(link.ParentLabel, sourceModel, map, createdEntities) is not Ifc4x3SpatialElement parent ||
                GetTargetEntity(link.ChildLabel, sourceModel, map, createdEntities) is not Ifc4x3Product child)
            {
                continue;
            }

            var relation = targetModel.Instances.New<Ifc4x3RelContainedInSpatialStructure>();
            relation.GlobalId = StepGuidHelper.ToPart21(Guid.NewGuid());
            relation.RelatingStructure = parent;
            relation.RelatedElements.Add(child);
        }
    }

    private static object? CopyDirectProperties(ExpressMetaProperty property, object parentObject)
    {
        return property.IsInverse ? null : property.PropertyInfo.GetValue(parentObject, null);
    }

    private static IPersistEntity? GetTargetEntity(
        int sourceLabel,
        IModel sourceModel,
        XbimInstanceHandleMap map,
        IReadOnlyDictionary<int, IPersistEntity> createdEntities)
    {
        if (createdEntities.TryGetValue(sourceLabel, out var createdEntity))
            return createdEntity;

        var sourceEntity = sourceModel.Instances[sourceLabel];
        if (sourceEntity == null)
            return null;

        return map.TryGetValue(new XbimInstanceHandle(sourceEntity), out var targetHandle)
            ? targetHandle.GetEntity()
            : null;
    }

    private static Dictionary<int, StepEntity> ReadStepEntities(string filePath)
    {
        var entities = new Dictionary<int, StepEntity>();

        foreach (var statement in ReadStepStatements(filePath))
        {
            var match = EntityRegex.Match(statement);
            if (!match.Success)
                continue;

            var label = int.Parse(match.Groups["label"].Value);
            var type = match.Groups["type"].Value.ToUpperInvariant();
            var arguments = SplitArguments(match.Groups["args"].Value);
            entities[label] = new StepEntity(label, type, arguments);
        }

        return entities;
    }

    private static IEnumerable<string> ReadStepStatements(string filePath)
    {
        var statement = "";

        foreach (var line in File.ReadLines(filePath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("/*", StringComparison.Ordinal))
                continue;

            statement += trimmed;
            if (!trimmed.EndsWith(";", StringComparison.Ordinal))
                continue;

            yield return statement;
            statement = "";
        }
    }

    private static List<string> SplitArguments(string arguments)
    {
        var result = new List<string>();
        var start = 0;
        var depth = 0;
        var inString = false;

        for (var i = 0; i < arguments.Length; i++)
        {
            var current = arguments[i];
            if (current == '\'')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (current == '(')
                depth++;
            else if (current == ')')
                depth--;
            else if (current == ',' && depth == 0)
            {
                result.Add(arguments[start..i].Trim());
                start = i + 1;
            }
        }

        result.Add(arguments[start..].Trim());
        return result;
    }

    private static IEnumerable<int> ExtractReferences(string value)
    {
        foreach (Match match in Regex.Matches(value, @"#(\d+)"))
            yield return int.Parse(match.Groups[1].Value);
    }

    private static int? ExtractSingleReference(string value)
    {
        return ExtractReferences(value).Cast<int?>().FirstOrDefault();
    }

    private static FacilityPartData CreateFacilityPartData(int label, StepEntity entity)
    {
        return new FacilityPartData(
            label,
            entity.Arguments.Count > 0 ? ParseStepString(entity.Arguments[0]) : null,
            entity.Arguments.Count > 2 ? ParseStepString(entity.Arguments[2]) : null,
            entity.Arguments.Count > 3 ? ParseStepString(entity.Arguments[3]) : null,
            ParseUsageType(entity.Arguments));
    }

    private static string? ParseStepString(string value)
    {
        value = value.Trim();
        if (value == "$" || value.Length < 2 || value[0] != '\'' || value[^1] != '\'')
            return null;

        return value[1..^1].Replace("''", "'");
    }

    private static Ifc4x3FacilityUsageEnum ParseUsageType(IEnumerable<string> arguments)
    {
        foreach (var argument in arguments.Reverse())
        {
            var value = argument.Trim().Trim('.');
            if (Enum.TryParse<Ifc4x3FacilityUsageEnum>(value, ignoreCase: true, out var usageType))
                return usageType;
        }

        return Ifc4x3FacilityUsageEnum.NOTDEFINED;
    }

    private sealed record StepEntity(int Label, string Type, List<string> Arguments);

    private sealed record StepHierarchyIndex(
        IReadOnlyDictionary<int, (StepEntity Relation, int ParentLabel)> ContainmentByChild,
        IReadOnlyDictionary<int, (StepEntity Relation, int ParentLabel)> AggregationByChild);

    private sealed record FacilityPartData(
        int Label,
        string? GlobalId,
        string? Name,
        string? Description,
        Ifc4x3FacilityUsageEnum UsageType);

    private sealed record HierarchyLink(HierarchyLinkKind Kind, int RelationLabel, int ParentLabel, int ChildLabel);

    private sealed record HierarchyBuildResult(
        IReadOnlyCollection<HierarchyLink> Links,
        IReadOnlyCollection<int> ParsedAncestorLabels,
        IReadOnlyCollection<FacilityPartData> MissingFacilityParts)
    {
        public bool NeedsFallback => MissingFacilityParts.Any();
    }

    private enum HierarchyLinkKind
    {
        Aggregates,
        Contains
    }
}