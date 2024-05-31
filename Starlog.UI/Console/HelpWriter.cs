using System.IO;
using System.Reactive.Subjects;
using System.Text;

namespace Genius.Starlog.UI.Console;

public class HelpWriter : TextWriter
{
    private readonly Subject<string> _textWritten = new();

    public override void Write(string? value)
    {
        if (value is not null)
        {
            _textWritten.OnNext(value);
        }

        base.Write(value);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _textWritten.Dispose();
    }

    public override Encoding Encoding => Encoding.UTF8;
    public IObservable<string> TextWritten => _textWritten;
}
