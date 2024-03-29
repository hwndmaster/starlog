using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Starlog.UI.Console;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI.Controllers;

public interface IConsoleController
{
    /// <summary>
    ///   Loads a file or folder by the specified path with an anonymous not-persisted profile.
    /// </summary>
    /// <param name="options">The options of an anonymous profile.</param>
    /// <returns>A task for awaiting the operation completion.</returns>
    Task LoadPathAsync(LoadPathCommandLineOptions options);
}

internal sealed class ConsoleController : IConsoleController
{
    private readonly ILogCodecContainer _logCodecContainer;
    private readonly IProfileSettingsTemplateQueryService _templatesQuery;
    private readonly ILogger<ConsoleController> _logger;
    private readonly IMainController _mainController;

    public ConsoleController(
        ILogCodecContainer logCodecContainer,
        IProfileSettingsTemplateQueryService templatesQuery,
        IMainController mainController,
        ILogger<ConsoleController> logger)
    {
        _logCodecContainer = logCodecContainer.NotNull();
        _mainController = mainController.NotNull();
        _templatesQuery = templatesQuery.NotNull();
        _logger = logger.NotNull();
    }

    public async Task LoadPathAsync(LoadPathCommandLineOptions options)
    {
        ProfileSettings? settings = null;
        if (!string.IsNullOrEmpty(options.Template))
        {
            // TODO: Cover this condition with unit tests
            ProfileSettingsTemplate? template = null;
            if (Guid.TryParse(options.Template, out var templateId))
            {
                template = await _templatesQuery.FindByIdAsync(templateId);
            }
            if (template is null)
            {
                template = (await _templatesQuery.GetAllAsync()).FirstOrDefault(x => x.Name.Equals(options.Template, StringComparison.OrdinalIgnoreCase));
            }
            if (template is not null)
            {
                settings = template.Settings;
            }
        }

        if (settings is null)
        {
            var codecName = options.Codec ?? "Plain Text";
            var logCodec = _logCodecContainer.GetLogCodecs().FirstOrDefault(x => x.Name.Equals(codecName, StringComparison.OrdinalIgnoreCase));
            if (logCodec is null)
            {
                _logger.LogWarning("Couldn't load a profile with unknown codec '{codec}'.", codecName);
                return;
            }
            var profileLogCodec = _logCodecContainer.CreateProfileLogCodec(logCodec);
            if (profileLogCodec is null)
            {
                // TODO: Cover this condition with unit tests
                _logger.LogWarning("Couldn't create profile codec settings for codec '{codec}'.", codecName);
                return;
            }
            if (options.CodecSettings is not null)
            {
                var processor = _logCodecContainer.CreateLogCodecProcessor(profileLogCodec);
                if (!processor.ReadFromCommandLineArguments(profileLogCodec, options.CodecSettings.ToArray()))
                {
                    // Couldn't read arguments, terminating...
                    _logger.LogWarning("Couldn't load a profile from '{path}' with codec '{codec}' and the following settings: {settings}", options.Path, codecName, string.Join(',', options.CodecSettings));
                    return;
                }
            }

            settings = new ProfileSettings
            {
                LogCodec = profileLogCodec,
                FileArtifactLinesCount = options.FileArtifactLinesCount ?? 0
            };
        }

        await _mainController.LoadPathAsync(options.Path, settings).ConfigureAwait(false);
    }
}
