using System.IO.Compression;
 
class Program
{
    const long LIMIT = 1L * 1024 * 1024 * 1024; // 1 GB
    //                long   KB     MB     GB. 
    private static void Main()
    {
        Console.WriteLine("Enter the source directory:");
        string srcDir = Console.ReadLine() ?? "";
        string outDir = Path.Combine(srcDir, "zips");
        if (!Directory.Exists(srcDir))
        {
            Console.WriteLine("Source directory doesn't exist.");
            Environment.Exit(1);
        }
        Directory.CreateDirectory(outDir);

        // 1. Plan batches by uncompressed size
        string[] files = Directory.GetFiles(srcDir);
        List<List<string>> batches = [];
        List<string> currentBatch = [];
        long current = 0;
         
        foreach (string path in files)
        {
            long size = new FileInfo(path).Length;
            if (currentBatch.Count > 0 && current + size > LIMIT)
            {
                batches.Add(currentBatch);
                currentBatch = [];
                current = 0;
            }
            currentBatch.Add(path);
            current += size;
            Console.WriteLine($"Adding {path} ({size / 1024 / 1024} MB) to batch {batches.Count + 1}, current batch size: {current / 1024 / 1024} MB");
        }
        // Add the left over.
        if (currentBatch.Count > 0) batches.Add(currentBatch);
        Console.WriteLine($"Planned {batches.Count} batches.");
         
        // 2. Build the zips in parallel
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };// lower this for a hard drive
         
        Console.WriteLine($"Creating zips in {outDir}...");
        Parallel.For(0, batches.Count, options, i =>
        {
            string zipPath = Path.Combine(outDir, $"May2026_{i + 1}.zip"); // Name of file.
            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
         
            foreach (string path in batches[i])
            {
                try
                {
                    string entryName = Path.GetRelativePath(srcDir, path);
                    zip.CreateEntryFromFile(path, entryName, CompressionLevel.Fastest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Skipping {path}: {ex.Message}");
                }
            }
            Console.WriteLine($"Created {zipPath} with {batches[i].Count} files.");
        });
         
        Console.WriteLine($"Created {batches.Count} zips.");
    }
}
