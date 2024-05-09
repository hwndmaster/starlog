global using Genius.Atom.Infrastructure;

using System.Diagnostics.CodeAnalysis;
using Genius.Atom.Data;
using Genius.Atom.Infrastructure.Commands;
using Genius.Starlog.Core.CommandHandlers;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.LogReading;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.Repositories;
using Genius.Atom.Data.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Genius.Atom.Infrastructure.Entities;
using Microsoft.Extensions.Configuration;
using Genius.Starlog.Core.Configuration;
using Genius.Starlog.Core.Models.VersionUpgraders;
using Genius.Starlog.Core.Serialization;
using Genius.Starlog.Core.Comparison;
using Genius.Starlog.Core.ProfileLoading;

namespace Genius.Starlog.Core;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services, IConfiguration config)
    {
        // Repositories and Query services
        services.RegisterRepository<Profile, ProfileRepository, IProfileQueryService, IProfileRepository>();
        services.RegisterRepository<ProfileSettingsTemplate, ProfileSettingsTemplateRepository, IProfileSettingsTemplateQueryService, IProfileSettingsTemplateRepository>();
        services.AddSingleton<SettingsRepository>();
        services.AddSingleton<ISettingsQueryService>(sp => sp.GetRequiredService<SettingsRepository>());
        services.AddSingleton<ISettingsRepository>(sp => sp.GetRequiredService<SettingsRepository>());

        // Log flow components
        services.AddSingleton<CurrentProfileLogContainer>();
        services.AddSingleton<ICurrentProfile>(x => x.GetRequiredService<CurrentProfileLogContainer>());
        services.AddSingleton<ILogContainer>(x => x.GetRequiredService<CurrentProfileLogContainer>());
        services.AddTransient<ILogCodecProcessor, PlainTextLogCodecProcessor>();
        services.AddTransient<ILogCodecProcessor, XmlLogCodecProcessor>();
        services.AddTransient<ILogCodecProcessor, WindowsEventLogCodecProcessor>();
        services.AddSingleton<LogCodecContainer>();
        services.AddSingleton<ILogCodecContainer>(sp => sp.GetRequiredService<LogCodecContainer>());
        services.AddSingleton<ILogCodecContainerInternal>(sp => sp.GetRequiredService<LogCodecContainer>());
        services.AddSingleton<IQueryService<LogCodec>>(sp => sp.GetRequiredService<LogCodecContainer>());

        // Log filtering components
        services.AddSingleton<ILogFilterContainer, LogFilterContainer>();
        services.AddTransient<IFilterProcessor, FilesFilterProcessor>();
        services.AddTransient<IFilterProcessor, MessageFilterProcessor>();
        services.AddTransient<IFilterProcessor, FieldFilterProcessor>();
        services.AddTransient<IFilterProcessor, LogLevelsFilterProcessor>();
        services.AddTransient<IFilterProcessor, TimeAgoFilterProcessor>();
        services.AddTransient<IFilterProcessor, TimeRangeFilterProcessor>();
        services.AddTransient<ILogRecordMatcher, LogRecordMatcher>();
        services.AddTransient<IQuickFilterProvider, QuickFilterProvider>();

        // Serialization and JSON Converters
        services.AddSingleton<IJsonConverter, LogCodecJsonConverter>();
        services.AddSingleton<IJsonConverter, LogFilterJsonConverter>();
        services.AddSingleton<FieldProfileFilterFromLoggersAndThreadsUpgrader>();
        services.AddSingleton<PlainTextProfileLogCodecVer1To2Upgrader>();
        services.AddSingleton<ProfileSettingsLegacyUpgrader>();

        // Command Handlers
        services.AddScoped<ICommandHandler<MessageParsingCreateOrUpdateCommand, MessageParsingCreateOrUpdateCommandResult>, MessageParsingCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<MessageParsingDeleteCommand>, MessageParsingDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileCreateCommand, Guid>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileUpdateCommand>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileDeleteCommand>, ProfileDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileLoadAnonymousCommand, Profile>, ProfileLoadAnonymousCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileFilterCreateOrUpdateCommand, ProfileFilterCreateOrUpdateCommandResult>, ProfileFilterCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileFilterDeleteCommand>, ProfileFilterDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<SettingsUpdateCommand>, SettingsUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<SettingsUpdateAutoLoadingProfileCommand>, SettingsUpdateAutoLoadingProfileCommandHandler>();

        // Other components
        services.AddTransient<IDirectoryMonitor, DirectoryMonitor>();
        services.AddTransient<IMaskPatternParser, MaskPatternParser>();
        services.AddTransient<IMessageParsingHandler, MessageParsingHandler>();
        services.AddSingleton<IProfileLoaderFactory, ProfileLoaderFactory>();
        services.AddSingleton<IComparisonService, ComparisonService>();

        // Configurations
        services.Configure<LogLevelMappingConfiguration>(config.GetSection("LogLevelMapping"));
    }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        RegisterLogFilters(serviceProvider);
        RegisterLogCodecs(serviceProvider);
        RegisterTypeDiscriminators(serviceProvider);
    }

    private static void RegisterLogFilters(IServiceProvider serviceProvider)
    {
        var logFilterContainer = serviceProvider.GetRequiredService<ILogFilterContainer>();
        logFilterContainer.RegisterLogFilter<FieldProfileFilter, FieldFilterProcessor>(
            new LogFilter(FieldProfileFilter.LogFilterId, "Field filter"));
        logFilterContainer.RegisterLogFilter<FilesProfileFilter, FilesFilterProcessor>(
            new LogFilter(new Guid("836b05dc-8f94-40b2-9606-67452f86ace0"), "File filter"));
        logFilterContainer.RegisterLogFilter<MessageProfileFilter, MessageFilterProcessor>(
            new LogFilter(new Guid("c78616c5-fe0d-4f9b-b46b-38a4b26727e6"), "Message filter"));
        logFilterContainer.RegisterLogFilter<LogLevelsProfileFilter, LogLevelsFilterProcessor>(
            new LogFilter(new Guid("bd1ffa05-8534-4555-ab17-92fd3f53fe13"), "Level filter"));
        logFilterContainer.RegisterLogFilter<TimeAgoProfileFilter, TimeAgoFilterProcessor>(
            new LogFilter(new Guid("8f5c5e27-5f4d-489f-8534-5fbaa8ee8571"), "Time ago filter"));
        logFilterContainer.RegisterLogFilter<TimeRangeProfileFilter, TimeRangeFilterProcessor>(
            new LogFilter(new Guid("4ba18116-122b-4580-afc9-97211c0a53af"), "Time range filter"));

#pragma warning disable CS0618 // Type or member is obsolete
        logFilterContainer.RegisterLogFilter<LoggersProfileFilter, ObsoleteFilterProcessor>(
            new LogFilter(new Guid("ad1398bc-a17e-4584-b7fa-d82fa547b5fe"), string.Empty), isObsolete: true);
        logFilterContainer.RegisterLogFilter<ThreadsProfileFilter, ObsoleteFilterProcessor>(
            new LogFilter(new Guid("11235ba9-cf84-413c-b094-d2c2c6672f4f"), string.Empty), isObsolete: true);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static void RegisterLogCodecs(IServiceProvider serviceProvider)
    {
        var logCodecContainer = serviceProvider.GetRequiredService<ILogCodecContainerInternal>();
        logCodecContainer.RegisterLogCodec<PlainTextProfileSettings, PlainTextLogCodecProcessor>(
            new LogCodec(new Guid("a38a40b6-c07f-49d5-a143-5c9f9f42149b"), PlainTextProfileSettings.CodecName));
        logCodecContainer.RegisterLogCodec<XmlProfileSettings, XmlLogCodecProcessor>(
            new LogCodec(new Guid("0cb976bc-6d87-4450-8202-530d9db09b40"), XmlProfileSettings.CodecName));
        logCodecContainer.RegisterLogCodec<WindowsEventProfileSettings, WindowsEventLogCodecProcessor>(
            new LogCodec(new Guid("23b498d4-576b-4967-b79f-3db43c9247e9"), WindowsEventProfileSettings.CodecName));
    }

    private static void RegisterTypeDiscriminators(IServiceProvider serviceProvider)
    {
        var typeDiscriminators = serviceProvider.GetRequiredService<ITypeDiscriminators>();

        // Filters
        typeDiscriminators.AddMapping<FilesProfileFilter>("files-profile-filter");
        typeDiscriminators.AddMapping<MessageProfileFilter>("msg-profile-filter");
        typeDiscriminators.AddMapping<FieldProfileFilter>("field-profile-filter");
        typeDiscriminators.AddMapping<LogLevelsProfileFilter>("loglevels-profile-filter");
        typeDiscriminators.AddMapping<TimeAgoProfileFilter>("timeago-profile-filter");
        typeDiscriminators.AddMapping<TimeRangeProfileFilter>("timerange-profile-filter");

        // Obsolete:
#pragma warning disable CS0618 // Type or member is obsolete
        typeDiscriminators.AddMapping<LoggersProfileFilter>("loggers-profile-filter");
        typeDiscriminators.AddMapping<ThreadsProfileFilter>("threads-profile-filter");
        typeDiscriminators.AddVersionUpgrader<FieldProfileFilter, LoggersProfileFilter, FieldProfileFilterFromLoggersAndThreadsUpgrader>();
        typeDiscriminators.AddVersionUpgrader<FieldProfileFilter, ThreadsProfileFilter, FieldProfileFilterFromLoggersAndThreadsUpgrader>();
#pragma warning restore CS0618 // Type or member is obsolete

        // Codecs
#pragma warning disable CS0618 // Type or member is obsolete
        typeDiscriminators.AddMapping<ProfileSettingsLegacy>("profile-settings"); // For backwards compatibility only
        typeDiscriminators.AddMapping<PlainTextProfileLogCodecV1>("plaintext-profile-log-codec"); // For backwards compatibility only
        typeDiscriminators.AddMapping<PlainTextProfileLogCodecV2, PlainTextProfileLogCodecV1, PlainTextProfileLogCodecVer1To2Upgrader>("plaintext-profile-log-codec", 2); // For backwards compatibility only
        typeDiscriminators.AddMapping<PlainTextProfileSettings, ProfileSettingsLegacy, ProfileSettingsLegacyUpgrader>("plaintext-profile-settings", 3);
#pragma warning restore CS0618 // Type or member is obsolete
        typeDiscriminators.AddMapping<XmlProfileSettings>("xml-profile-settings");
        typeDiscriminators.AddMapping<WindowsEventProfileSettings>("winevent-profile-settings");

        // Misc
        typeDiscriminators.AddMapping<MessageParsing>("message-parsing");
        typeDiscriminators.AddMapping<PatternValue>("pattern-value");
    }
}
