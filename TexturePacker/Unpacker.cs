using System.Diagnostics;
using TexturePacker.Models;

namespace TexturePacker;

public static class Unpacker
{
    public static void UnpackAllPages(DirectoryInfo input, DirectoryInfo output)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var count = 0;
        
        foreach (var file in input.GetFiles())
        {
            if (!file.FullName.EndsWith(".json"))
                continue;

            var page = TexturePage.Load(file.DirectoryName, file.Name.Replace(".json", ""));

            Console.WriteLine($"Unpacking page {page}");
            
            if (page.Options.PackIndexed)
            {
                page.LoadPalettes($"{input.FullName}/palettes");
            }

            page.WriteToFolder(output.FullName);
            count++;
        }
        
        stopwatch.Stop();
        Console.WriteLine($"Unpacked {count} pages in {stopwatch.Elapsed}");
    }
}