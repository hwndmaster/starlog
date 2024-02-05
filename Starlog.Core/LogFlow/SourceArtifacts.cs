namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A record contains source artifacts, in case of files it is usually the first X lines of each log file.
/// </summary>
/// <param name="Artifacts">The source artifacts.</param>
public sealed record SourceArtifacts(string[] Artifacts);
