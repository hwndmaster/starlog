using Genius.Starlog.Core.LogFlow;
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
    private readonly IProfileLoadingController _controller;

    public ConsoleController(
        ILogCodecContainer logCodecContainer,
        IProfileSettingsTemplateQueryService templatesQuery,
        IProfileLoadingController controller,
        ILogger<ConsoleController> logger)
    {
        _logCodecContainer = logCodecContainer.NotNull();
        _controller = controller.NotNull();
        _templatesQuery = templatesQuery.NotNull();
        _logger = logger.NotNull();
    }

    public async Task LoadPathAsync(LoadPathCommandLineOptions options)
    {
        ProfileSettingsBase? settings = null;
        if (!string.IsNullOrEmpty(options.Template))
        {
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
                settings = template.Settings.Clone();
            }
        }

        if (settings is null)
        {
            var codecName = options.Codec ?? PlainTextProfileSettings.CodecName;
            var logCodec = _logCodecContainer.GetLogCodecs().FirstOrDefault(x => x.Name.Equals(codecName, StringComparison.OrdinalIgnoreCase));
            if (logCodec is null)
            {
                _logger.LogWarning("Couldn't load a profile with unknown codec '{Codec}'.", codecName);
                return;
            }
            settings = _logCodecContainer.CreateProfileSettings(logCodec);
            if (settings is null)
            {
                _logger.LogWarning("Couldn't create profile codec settings for codec '{Codec}'.", codecName);
                return;
            }
            if (options.CodecSettings is not null)
            {
                var logCodecSettingsReader = _logCodecContainer.FindLogCodecSettingsReader(settings);
                if (!logCodecSettingsReader.ReadFromCommandLineArguments(settings, options.CodecSettings.ToArray()))
                {
                    // Couldn't read arguments, terminating...
                    _logger.LogWarning("Couldn't load a profile from '{Path}' with codec '{Codec}' and the following settings: {Settings}", options.Path, codecName, string.Join(',', options.CodecSettings));
                    return;
                }
            }
        }

        // TODO: To cover with unit tests
        if (settings is PlainTextProfileSettings plainTextProfileSettings)
        {
            plainTextProfileSettings.Path = options.Path;

            // TODO: Might be worth taking it to `processor.ReadFromCommandLineArguments()`
            if (options.FileArtifactLinesCount is not null)
            {
                plainTextProfileSettings.FileArtifactLinesCount = options.FileArtifactLinesCount.Value;
            }
        }

        await _controller.LoadProfileSettingsAsync(settings).ConfigureAwait(false);
    }
}
