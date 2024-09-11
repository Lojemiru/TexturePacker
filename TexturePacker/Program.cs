using System.CommandLine;

namespace TexturePacker;

// Full disclosure - no code was directly used, but I poked through these as my learning material:
// https://blackpawn.com/texts/lightmaps/default.html
// https://gist.github.com/ttalexander2/88a40eec0fd0ea5b31cc2453d6bbddad

// TODO: Duplicate removal
// TODO: Either make sprite padding actually extrude color, or only pad on one side per axis to save space.

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Lojemiru's TexturePacker. For use with Another Mediocre 2D Engine.");

        var packCommand = new Command("pack", "Packs a texture group collection into individual atlases with accompanying metadata JSON files.");

        var dirOption2 = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The parent directory of the texture groups you desire to pack.");

        var outputOption2 = new Option<DirectoryInfo?>(
            name: "--output",
            description: "The output directory for the resulting atlases and metadata.");

        var outputOption3 = new Option<DirectoryInfo?>(
            name: "--enums",
            description: "Enables enum generation and specifies the output directory for the resulting enums."
            );

        var namespaceOption = new Option<string?>(
            name: "--namespace",
            description: "Specifies the namespace used for enum generation. Defaults to 'GameContent'."
            );

        packCommand.AddOption(dirOption2);
        packCommand.AddOption(outputOption2);
        packCommand.AddOption(outputOption3);
        packCommand.AddOption(namespaceOption);

        packCommand.SetHandler((dir, output, enumOutput, nameSpace) =>
        {
            Packer.PackAllPages(dir!, output!, enumOutput, nameSpace ?? "GameContent");
        },
            dirOption2, outputOption2, outputOption3, namespaceOption);
        
        rootCommand.AddCommand(packCommand);

        var unpackCommand = new Command("unpack", "Unpacks an atlas+metadata combo into individual files.");

        var inputOption = new Option<DirectoryInfo?>(
            name: "--input",
            description: "The parent directory of the texture atlases you desire to unpack."
        );

        var outputOption = new Option<DirectoryInfo?>(
            name: "--output",
            description: "The output directory for the resulting individual sprites."
        );
        
        unpackCommand.AddOption(inputOption);
        unpackCommand.AddOption(outputOption);
        
        unpackCommand.SetHandler((input, output) =>
        {
            Unpacker.UnpackAllPages(input!, output!);
        },
            inputOption, outputOption);
        
        rootCommand.AddCommand(unpackCommand);

        return await rootCommand.InvokeAsync(args);
    }
}