using System.IO.Compression;
 
class Program
{
    const long LIMIT = 1L * 1024 * 1024 * 1024; // 1 GB
    //                long   KB     MB     GB. 
    private static void Main()
    {
        string srcDir;
        string outDir;
        int count = 0;
        do {
            srcDir = Console.ReadLine() ?? "";
            outDir = Console.ReadLine() ?? "";
            try {
                Directory.CreateDirectory(outDir);
            } catch {
                count++;
                continue;
            };
            if (count == 3) {
                Console.WriteLine("One of your paths wasn't valid.");
                Environment.Exit(1);
            }
        } while (!Directory.Exists(srcDir));
         
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
        }
        // Add the left over.
        if (currentBatch.Count > 0) batches.Add(currentBatch);
         
        // 2. Build the zips in parallel
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };// lower this for a hard drive
         
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
        });
         
        Console.WriteLine($"Created {batches.Count} zips.");
    }
}
