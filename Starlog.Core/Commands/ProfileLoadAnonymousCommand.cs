using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Commands;

public sealed class ProfileLoadAnonymousCommand : ICommandMessageExchange<Profile>
{
    public ProfileLoadAnonymousCommand(string path, ProfileLogCodecBase logCodec, int? fileArtifactLinesCount)
    {
        Path = path;
        LogCodec = logCodec;
        FileArtifactLinesCount = fileArtifactLinesCount;
    }

    public string Path { get; }
    public ProfileLogCodecBase LogCodec { get; }
    public int? FileArtifactLinesCount { get; }
}
