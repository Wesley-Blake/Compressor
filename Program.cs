using System.IO.Compression;

class Program
{
    private static void Main(string[] args)
    {
        // Help check.
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine("""
            Usage:
            Compressor inputfile <path> [options] <input>

            Options:
                --output <path>     Where zipped files will be stored.
                --size <number>     Target size of output, individual files larger than that will be zipped solo.
                -h, --help          Show this screen.
            """);
            return;
        }
        // Source Directory check and creation.
        if (args.Length < 1) 
        {
            Console.WriteLine("Not enought arguments provided.");
            return;
        }
        string srcDir = args[0];
        if (!Directory.Exists(srcDir))
        {
            Console.WriteLine("Sorce Directory doesn't exist!");
            return;
        }
        // Out Directory check and creation.
        int outIndex = args.IndexOf("--output")+1;
        string outDir;
        if (outIndex > 1) // 0 will always be source and 1 is flag.
        {
            if (outIndex < args.Length)
            {
                outDir = args[outIndex];
            } else
            {
                Console.WriteLine("Bad output argument.");
                return;
            }
            Directory.CreateDirectory(outDir);
        } else
        {
            outDir = Path.Combine(srcDir, @"zip\");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        }
        // Size check and creation.
        outIndex = args.IndexOf("--size")+1;
        long? limit = null;
        if (outIndex > 0 &&
            outIndex < args.Length &&
            long.TryParse(args[outIndex], out long parsedLimit))
        {
            limit = parsedLimit * 1024 * 1024 * 1024 ;
        //            long          KB     MB     GB. 
        }
        limit ??= 1L * 1024 * 1024 * 1024; // 1 GB

        // 1. Plan batches by uncompressed size
        string[] files = Directory.GetFiles(srcDir);
        List<List<string>> batches = [];
        List<string> currentBatch = [];
        long current = 0;
         
        foreach (string path in files)
        {
            long size = new FileInfo(path).Length;
            if (currentBatch.Count > 0 && current + size > limit)
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
