using System.Diagnostics.Contracts;

namespace Genius.Starlog.Core.LogFlow;

public abstract class LogSourceBase
{
    [Pure]
    public abstract LogSourceBase WithNewName(string newName);

    public abstract string Name { get; }
    public virtual string DisplayName => Name;

    /// <summary>
    ///   The source artifacts.
    /// </summary>
    public SourceArtifacts? Artifacts { get; set; }
}
