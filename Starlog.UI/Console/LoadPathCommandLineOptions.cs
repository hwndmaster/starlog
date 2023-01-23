using CommandLine;

namespace Genius.Starlog.UI.Console;

[Verb("loadpath", HelpText = "Lets Starlog to load a specified folder or file without creating a profile.")]
public class LoadPathCommandLineOptions
{
    [Option('p', "path", Required = true, HelpText = "Defines a path which Starlog will open at a startup.")]
    public required string Path { get; init; }

    [Option('r', "codec", Required = false, HelpText = "Defines a log codec to be used for the selected path. Default is 'Plain Text'.")]
    public string? Codec { get; set; }

    [Option('s', "settings", Required = false, HelpText = "Defines the log codec settings.")]
    public IEnumerable<string> CodecSettings { get; set; } = Enumerable.Empty<string>();

    [Option('a', "artifacts", Required = false, HelpText = "Defines the number of lines of the file artifacts.")]
    public int? FileArtifactLinesCount { get; set; }
}
