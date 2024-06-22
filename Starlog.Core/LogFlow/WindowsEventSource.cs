namespace Genius.Starlog.Core.LogFlow;

/// <summary>
///   A class contains information about a reading log file.
/// </summary>
public sealed class WindowsEventSource : LogSourceBase
{
    public WindowsEventSource(string name)
    {
        Name = name.NotNull();
    }

    public override string Name { get; }

    public override LogSourceBase WithNewName(string newName)
    {
        return new WindowsEventSource(newName);
    }
}
