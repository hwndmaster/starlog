using System.Text;

namespace Genius.Starlog.UI.Helpers;

public static class TimeSpanExtensions
{
    public static string ToDisplayString(this TimeSpan timeSpan)
    {
        StringBuilder sb = new();
        if (timeSpan.TotalMinutes >= 1)
        {
            sb.Append(Math.Floor(timeSpan.TotalMinutes));
            sb.Append(" min");
        }

        if (timeSpan.Seconds != 0 || sb.Length == 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append(timeSpan.Seconds);
            sb.Append(" sec");
        }

        if (timeSpan.Milliseconds != 0)
        {
            sb.Append(' ');
            sb.Append(timeSpan.Milliseconds);
            sb.Append(" ms");
        }

        return sb.ToString();
    }
}
