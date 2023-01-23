using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Genius.Starlog.UI.Controllers;

namespace Genius.Starlog.UI.Console;

public interface IConsoleParser
{
    void Process(string[] args);
}

// TODO: Cover with unit tests
internal sealed class ConsoleParser : IConsoleParser, IDisposable
{
    private readonly IMainController _mainController;
    private readonly IUserInteraction _ui;
    private HelpWriter? _helpWriter;

    public ConsoleParser(IMainController mainController, IUserInteraction ui)
    {
        _mainController = mainController.NotNull();
        _ui = ui.NotNull();
    }

    public void Dispose()
    {
        _helpWriter?.Dispose();
    }

    public void Process(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            return;
        }

        EnsureHelpWriter();

        var parser = new Parser(cfg =>
        {
            cfg.HelpWriter = _helpWriter;
        });

        parser.ParseArguments<LoadPathCommandLineOptions>(args)
            .MapResult(
                async (LoadPathCommandLineOptions opts) =>
                    {
                        if (string.IsNullOrEmpty(opts.Path)) return;
                        await _mainController.LoadPathAsync(opts);
                    }, errors => Task.CompletedTask);
    }

    [MemberNotNull(nameof(_helpWriter))]
    private void EnsureHelpWriter()
    {
        if (_helpWriter is not null)
            return;

        _helpWriter = new HelpWriter();
        _helpWriter.TextWritten.Subscribe(async text =>
        {
            await _mainController.Loaded;
            _ui.ShowInformation(text);
        });
    }
}
