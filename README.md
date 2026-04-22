# IfcIsolator

IfcIsolator extracts selected IFC products from an IFC model and writes a smaller isolated IFC file. You choose the products by their IFC entity labels, such as `#328`, `#446`, or `#1417` in the STEP text.

The solution contains:

- `IfcIsolator`: class library DLL with the reusable isolation API.
- `IfcIsolatorTerminal`: command-line wrapper around the library.
- `IfcIsolatorTests`: xUnit tests and sample IFC files for IFC2x3, IFC4, and IFC4x3.

## What It Does

Given:

- an input `.ifc` file,
- an output folder,
- one or more IFC entity labels,

IfcIsolator creates a new IFC file containing the selected products and the supporting context needed for them to remain usable. The output file is written next to the output folder with this naming pattern:

```text
<input-file-name>_Isolated.ifc
```

For example:

```text
C:\Models\Building.ifc
```

becomes:

```text
C:\Models\Output\Building_Isolated.ifc
```

## Supported IFC Files

The test suite covers examples from:

- IFC2x3
- IFC4
- IFC4x3

The implementation uses xBIM through `Xbim.Essentials`. IFC4x3 files include additional fallback logic for spatial hierarchy cases where xBIM does not fully expose some infrastructure hierarchy relationships.

## Entity Labels

Entity labels are the numeric IDs used in IFC STEP files. In the raw IFC text they appear after `#`.

Example IFC fragment:

```ifc
#328=IFCWALL('0NWseyvsH7_gBW225aGtuD',#42,'Basic Wall',...);
#446=IFCWALL('0NWseyvsH7_gBW225aGtyM',#42,'Basic Wall',...);
#6967=IFCSPACE('3DbdJyICv8GP1HEYzu7Wjc',#91,'Office',...);
```

For these entities, the labels are:

```text
328
446
6967
```

When using the terminal, pass them as a single quoted string separated by spaces:

```text
"328 446 6967"
```

When using the library, pass them as an `IEnumerable<int>`.

## Build

From the repository root:

```powershell
dotnet build IfcIsolator.sln
```

Run tests:

```powershell
dotnet test IfcIsolatorTests\IfcIsolatorTests.csproj
```

Build only the library DLL:

```powershell
dotnet build IfcIsolator\IfcIsolator.csproj
```

Build the AnyCPU dependency bundle:

```powershell
dotnet build IfcIsolator\IfcIsolator.csproj -c Release
```

This creates a folder containing `IfcIsolator.dll`, `IfcIsolator.deps.json`, and all resolved dependency DLLs:

```text
artifacts\IfcIsolatorDependencies\AnyCPU\Release\net10.0
```

Use the DLLs in that folder as references/dependencies from another .NET project. A single reusable class-library DLL with every dependency embedded is not the standard .NET build output; this project instead produces the library plus its dependency DLLs together in one folder.

The Debug DLL is generated at:

```text
IfcIsolator\bin\Debug\net10.0\IfcIsolator.dll
```

The Release DLL is generated at:

```text
IfcIsolator\bin\Release\net10.0\IfcIsolator.dll
```

## Command-Line Usage

The terminal project accepts the same format as before:

```text
IfcIsolatorTerminal <inputPath> <outputFolderPath> <entityLabels>
```

Arguments:

- `inputPath`: full path to the source `.ifc` file.
- `outputFolderPath`: folder where the isolated IFC file should be written.
- `entityLabels`: one quoted string containing space-separated entity labels.

Run with `dotnet run`:

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\Ifc4_Revit_STR.ifc" "C:\Models\Output" "328"
```

This writes:

```text
C:\Models\Output\Ifc4_Revit_STR_Isolated.ifc
```

Isolate multiple products:

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\Ifc4_Revit_STR.ifc" "C:\Models\Output" "328 446"
```

Isolate a space:

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\DigitalHub_FM-ARC_v2.ifc" "C:\Models\Output" "6967"
```

Isolate an IFC4x3 infrastructure element:

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\KIT-Simple-Road-Test-Web-IFC4x3_RC2.ifc" "C:\Models\Output" "1417"
```

## Library Usage

Reference the library project from another .NET project:

```powershell
dotnet add MyConsumerProject.csproj reference .\IfcIsolator\IfcIsolator.csproj
```

Then call `Isolator.SplitByEntityLabels` with an `IEnumerable<int>`:

```csharp
using IfcIsolator;

var sourceFilePath = @"C:\Models\Ifc4_Revit_STR.ifc";
var outputFolderPath = @"C:\Models\Output";
IEnumerable<int> entityLabels = new[] { 328, 446 };

Isolator.SplitByEntityLabels(sourceFilePath, outputFolderPath, entityLabels);
```

The library writes:

```text
C:\Models\Output\Ifc4_Revit_STR_Isolated.ifc
```

You can also use any collection that implements `IEnumerable<int>`:

```csharp
using IfcIsolator;

var labels = new List<int> { 6967 };
Isolator.SplitByEntityLabels(sourcePath, outputFolderPath, labels);
```

Or parse labels in your own application:

```csharp
using IfcIsolator;

var labelsRaw = "328 446";
var labels = labelsRaw
    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
    .Select(int.Parse);

Isolator.SplitByEntityLabels(sourcePath, outputFolderPath, labels);
```

## Finding Entity Labels In IFC Files

### Option 1: Open The IFC As Text

IFC files are usually STEP text files. Open the file in a text editor and search for the object by name, type, or GlobalId.

Example:

```ifc
#328=IFCWALL('0NWseyvsH7_gBW225aGtuD',#42,'Basic Wall:Exterior - 200mm',...);
```

Use `328` as the entity label.

### Option 2: Use xBIM To List Products

You can write a small helper to print product labels, types, names, and GlobalIds:

```csharp
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using var model = IfcStore.Open(@"C:\Models\Building.ifc");

foreach (var product in model.Instances.OfType<IIfcProduct>())
{
    Console.WriteLine(
        $"#{product.EntityLabel} {product.GetType().Name} {product.Name} {product.GlobalId}");
}
```

Example output:

```text
#328 IfcWall Basic Wall:Exterior - 200mm 0NWseyvsH7_gBW225aGtuD
#446 IfcWall Basic Wall:Interior - 100mm 0NWseyvsH7_gBW225aGtyM
#6967 IfcSpace Office 3DbdJyICv8GP1HEYzu7Wjc
```

Then pass the numeric labels to IfcIsolator.

## Behavior Notes

- The output path argument is a folder, not a full output file path.
- The output file name is always generated from the source file name plus `_Isolated.ifc`.
- If the source file path or output folder does not exist, the library returns without writing a file.
- If a label does not match an `IIfcProduct`, it will not add a product to the isolated model.
- If a selected product is an `IIfcSpatialElement`, the isolator also includes products contained in that spatial hierarchy.
- For regular elements, the isolator copies related context such as spatial containment, aggregations, types, property relationships, materials, documents, and geometry where available.

## Example Scenarios

### Extract One Wall

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\Ifc4_Revit_STR.ifc" "C:\Models\Output" "328"
```

Result:

```text
C:\Models\Output\Ifc4_Revit_STR_Isolated.ifc
```

### Extract Two Walls

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\Ifc4_Revit_STR.ifc" "C:\Models\Output" "328 446"
```

### Extract A Space

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\DigitalHub_FM-ARC_v2.ifc" "C:\Models\Output" "6967"
```

### Extract An IFC4x3 Road Element

```powershell
dotnet run --project IfcIsolatorTerminal -- "C:\Models\KIT-Simple-Road-Test-Web-IFC4x3_RC2.ifc" "C:\Models\Output" "1417"
```

## Development

Run the full test suite:

```powershell
dotnet test IfcIsolatorTests\IfcIsolatorTests.csproj
```

The tests use sample IFC files in:

```text
IfcIsolatorTests\TestFiles
```

Generated isolated test files are written to `TestFilesOutput` folders under the test data folders. These outputs are ignored by git.
