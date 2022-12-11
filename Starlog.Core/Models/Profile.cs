using Genius.Atom.Infrastructure.Entities;

namespace Genius.Starlog.Core.Models;

public sealed class Profile : EntityBase
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required ProfileLogReaderBase LogReader { get; set; }
    public IList<ProfileFilterBase> Filters { get; set; } = new List<ProfileFilterBase>();
    public int FileArtifactLinesCount { get; set; } = 0;
}
