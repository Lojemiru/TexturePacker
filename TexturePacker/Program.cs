using TexturePacker.Models;

namespace TexturePacker;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.CommandLine;
using Rectangle = SixLabors.ImageSharp.Rectangle;

// Full disclosure - no code was directly used, but I poked through these as my learning material:
// https://blackpawn.com/texts/lightmaps/default.html
// https://gist.github.com/ttalexander2/88a40eec0fd0ea5b31cc2453d6bbddad

// TODO: Duplicate removal
// TODO: Either make sprite padding actually extrude color, or only pad on one side per axis to save space.

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Texture packer. Shenanigans abound!!!");

        var enumCommand = new Command("enum", "Compiles a folder of enum files into a single target enum .cs file with the given namespace.");

        var dirOption = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The directory to grab enum .cs files from.");

        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "The output file.");

        var namespaceOption = new Option<string>(
            name: "--namespace",
            description: "The namespace for the output enums.");

        var nameOption = new Option<string>(
            name: "--name",
            description: "The name of the output file."
            );

        enumCommand.AddOption(dirOption);
        enumCommand.AddOption(outputOption);
        enumCommand.AddOption(namespaceOption);
        enumCommand.AddOption(nameOption);

        enumCommand.SetHandler((dir, output, name, nameSpace) =>
        {
            Packer.Enum(dir!, output!, name!, nameSpace!);
        },
            dirOption, outputOption, nameOption, namespaceOption);

        rootCommand.AddCommand(enumCommand);

        var packCommand = new Command("pack", "Packs textures from a folder into a texturepage, JSON metadata, and an enum file.");

        var dirOption2 = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The directory to begin packing from.");

        var sizeOption = new Option<int>(
            name: "--size",
            description: "The size of the output texturepage, in pixels.",
            getDefaultValue: () => 2048);

        var outputOption2 = new Option<DirectoryInfo?>(
            name: "--output",
            description: "The output directory for the resulting texturepage.");

        var nameOption2 = new Option<string>(
            name: "--name",
            description: "The name of the output file."
            );

        packCommand.AddOption(dirOption2);
        packCommand.AddOption(sizeOption);
        packCommand.AddOption(outputOption2);
        packCommand.AddOption(nameOption2);

        packCommand.SetHandler((dir, size, output, name) =>
        {
            Packer.Pack(dir!, size!, output!, name!);
        },
            dirOption2, sizeOption, outputOption2, nameOption2);


        rootCommand.AddCommand(packCommand);

        return await rootCommand.InvokeAsync(args);
    }
}