namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A record contains file artifacts, usually the first X lines of each log file.
/// </summary>
/// <param name="Artifacts">The file artifacts.</param>
public sealed record FileArtifacts(string[] Artifacts);
