using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public abstract class ProfileUpdatableData
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required ProfileLogCodecBase LogCodec { get; init; }
    public required int FileArtifactLinesCount { get; init; }
}
