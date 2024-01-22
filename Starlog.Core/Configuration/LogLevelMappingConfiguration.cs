namespace Genius.Starlog.Core.Configuration;

public sealed class LogLevelMappingConfiguration
{
    public required string[] TreatAsMinor { get; set; }
    public required string[] TreatAsWarning { get; set; }
    public required string[] TreatAsError { get; set; }
    public required string[] TreatAsCritical { get; set; }
}
