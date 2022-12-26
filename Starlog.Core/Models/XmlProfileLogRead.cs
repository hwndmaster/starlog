namespace Genius.Starlog.Core.Models;

/// <summary>
///   The log reader settings for the profile with Xml log reader type.
/// </summary>
public sealed class XmlProfileLogRead : ProfileLogReadBase
{
    public XmlProfileLogRead(LogReader logReader)
        : base(logReader)
    {
    }
}
