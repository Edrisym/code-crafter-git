using System.IO.Compression;
using System.Text;

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

string command = args[0];

if (command == "init")
{
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", "ref: refs/heads/main\n");
    Console.WriteLine("Initialized git directory");
}
else if (command == "cat-file")
{
    if (args[1].Equals("-p", StringComparison.InvariantCultureIgnoreCase))
    {
        ReadBlobObject(".git", args[2]);
    }
}
else
{
    throw new ArgumentException($"Unknown command {command}");
}

void ReadBlobObject(string gitDir, string sha)
{
    string dir = sha.Substring(0, 2);
    string file = sha.Substring(2);
    string objectPath = Path.Combine(gitDir, "objects", dir, file);

    if (!File.Exists(objectPath))
    {
        Console.WriteLine($"Object not found: {sha}");
        return;
    }

    using FileStream fileStream = File.OpenRead(objectPath);
    using ZLibStream zLibStream = new ZLibStream(fileStream, CompressionMode.Decompress);
    using MemoryStream memoryStream = new MemoryStream();

    zLibStream.CopyTo(memoryStream);
    byte[] decompressedData = memoryStream.ToArray();
    int nullIndex = Array.IndexOf(decompressedData, (byte)0);

    // Extract header
    string header = Encoding.UTF8.GetString(decompressedData, 0, nullIndex);

    // Detect encoding of the content
    int contentOffset = nullIndex + 1;
    Encoding encoding;

    // Check for UTF-16 LE BOM (FF FE) or UTF-16 BE BOM (FE FF)
    if (decompressedData.Length >= contentOffset + 2 &&
        decompressedData[contentOffset] == 0xFF &&
        decompressedData[contentOffset + 1] == 0xFE)
    {
        encoding = Encoding.Unicode; // UTF-16 LE
        contentOffset += 2; // Skip BOM
    }
    else if (decompressedData.Length >= contentOffset + 2 &&
             decompressedData[contentOffset] == 0xFE &&
             decompressedData[contentOffset + 1] == 0xFF)
    {
        encoding = Encoding.BigEndianUnicode; // UTF-16 BE
        contentOffset += 2;
    }
    else
    {
        encoding = Encoding.UTF8;
    }

    // Decode only the content
    string content = encoding.GetString(decompressedData, contentOffset, decompressedData.Length - contentOffset);

    //Console.WriteLine($"[Header]: {header}");
    Console.Write($"{content}");
}