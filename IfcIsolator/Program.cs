namespace IfcIsolator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Validate that we have at least three arguments
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: Program <inputPath> <outputPath> <entityLabels>");
                Console.WriteLine("Example: Program \"C:\\input.ifc\" \"C:\\output.ifc\" \"10 20 30 100\"");
                return;
            }

            // Read the arguments
            string inputPath = args[0];
            string outputPath = args[1];
            string labelsRaw = args[2]; // e.g., "10 20 30"

            // Split the third argument on spaces and parse them as integers
            int[] entityLabels = labelsRaw
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToArray();

            // Now call into your IfcSplitter logic
            IfcIsolator.SplitByEntityLabels(inputPath, outputPath, entityLabels);
        }
    }
}
