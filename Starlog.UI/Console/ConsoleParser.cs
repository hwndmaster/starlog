using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Genius.Starlog.UI.Controllers;

namespace Genius.Starlog.UI.Console;

public interface IConsoleParser
{
    void Process(string[] args);
}

internal sealed class ConsoleParser : IConsoleParser, IDisposable
{
    private readonly IConsoleController _consoleController;
    private readonly IMainController _mainController;
    private readonly IUserInteraction _ui;
    private HelpWriter? _helpWriter;

    public ConsoleParser(IConsoleController consoleController, IMainController mainController, IUserInteraction ui)
    {
        _consoleController = consoleController.NotNull();
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

        using var parser = new Parser(cfg => cfg.HelpWriter = _helpWriter);

        _ = parser.ParseArguments<LoadPathCommandLineOptions>(args)
            .MapResult(
                async (LoadPathCommandLineOptions opts) =>
                    {
                        if (string.IsNullOrEmpty(opts.Path)) return;
                        await _consoleController.LoadPathAsync(opts);
                    }, errors => Task.CompletedTask);
    }

    [MemberNotNull(nameof(_helpWriter))]
    private void EnsureHelpWriter()
    {
        if (_helpWriter is not null)
        {
            return;
        }
        else
        {
            _helpWriter = new HelpWriter();
            _helpWriter.TextWritten.Subscribe(async text =>
            {
                await _mainController.Loaded;
                _ui.ShowInformation(text);
            });
        }
    }
}
