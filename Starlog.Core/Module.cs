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

namespace Genius.Starlog.Core;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services)
    {
        // Repositories and Query services
        services.RegisterRepository<Profile, ProfileRepository, IProfileQueryService, IProfileRepository>();
        services.RegisterRepository<ProfileSettingsTemplate, ProfileSettingsTemplateRepository, IProfileSettingsTemplateQueryService, IProfileSettingsTemplateRepository>();
        services.AddSingleton<SettingsRepository>();
        services.AddSingleton<ISettingsQueryService>(sp => sp.GetRequiredService<SettingsRepository>());
        services.AddSingleton<ISettingsRepository>(sp => sp.GetRequiredService<SettingsRepository>());

        // LogFlow and LogFiltering services
        services.AddSingleton<LogContainer>();
        services.AddSingleton<ICurrentProfile>(x => x.GetRequiredService<LogContainer>());
        services.AddSingleton<ILogContainer>(x => x.GetRequiredService<LogContainer>());
        services.AddSingleton<ILogFilterContainer, LogFilterContainer>();

        services.AddSingleton<LogCodecContainer>();
        services.AddSingleton<ILogCodecContainer>(sp => sp.GetRequiredService<LogCodecContainer>());
        services.AddSingleton<IQueryService<LogCodec>>(sp => sp.GetRequiredService<LogCodecContainer>());

        services.AddTransient<IFilterProcessor, MessageFilterProcessor>();
        services.AddTransient<IFilterProcessor, LoggersFilterProcessor>();
        services.AddTransient<IFilterProcessor, LogLevelsFilterProcessor>();
        services.AddTransient<IFilterProcessor, ThreadsFilterProcessor>();
        services.AddTransient<IFilterProcessor, TimeAgoFilterProcessor>();
        services.AddTransient<IFilterProcessor, TimeRangeFilterProcessor>();
        services.AddTransient<ILogCodecProcessor, PlainTextLogCodecProcessor>();
        services.AddTransient<ILogCodecProcessor, XmlLogCodecProcessor>();
        services.AddTransient<ILogRecordMatcher, LogRecordMatcher>();
        services.AddTransient<IQuickFilterProvider, QuickFilterProvider>();

        // Services
        // TO BE DONE LATER

        // Converters
        services.AddSingleton<IJsonConverter, LogCodecJsonConverter>();
        services.AddSingleton<IJsonConverter, LogFilterJsonConverter>();

        // Command Handlers
        services.AddScoped<ICommandHandler<ProfileCreateCommand, Guid>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileUpdateCommand>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileDeleteCommand>, ProfileDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileLoadAnonymousCommand, Profile>, ProfileLoadAnonymousCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileFilterCreateOrUpdateCommand, ProfileFilterCreateOrUpdateCommandResult>, ProfileFilterCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileFilterDeleteCommand>, ProfileFilterDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<SettingsUpdateCommand>, SettingsUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<SettingsUpdateAutoLoadingProfileCommand>, SettingsUpdateAutoLoadingProfileCommandHandler>();
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
        logFilterContainer.RegisterLogFilter<MessageProfileFilter, MessageFilterProcessor>(
            new LogFilter(new Guid("c78616c5-fe0d-4f9b-b46b-38a4b26727e6"), "Message filter"));
        logFilterContainer.RegisterLogFilter<LoggersProfileFilter, LoggersFilterProcessor>(
            new LogFilter(new Guid("ad1398bc-a17e-4584-b7fa-d82fa547b5fe"), "Logger filter"));
        logFilterContainer.RegisterLogFilter<LogLevelsProfileFilter, LogLevelsFilterProcessor>(
            new LogFilter(new Guid("bd1ffa05-8534-4555-ab17-92fd3f53fe13"), "Level filter"));
        logFilterContainer.RegisterLogFilter<ThreadsProfileFilter, ThreadsFilterProcessor>(
            new LogFilter(new Guid("11235ba9-cf84-413c-b094-d2c2c6672f4f"), "Thread filter"));
        logFilterContainer.RegisterLogFilter<TimeAgoProfileFilter, TimeAgoFilterProcessor>(
            new LogFilter(new Guid("8f5c5e27-5f4d-489f-8534-5fbaa8ee8571"), "Time ago filter"));
        logFilterContainer.RegisterLogFilter<TimeRangeProfileFilter, TimeRangeFilterProcessor>(
            new LogFilter(new Guid("4ba18116-122b-4580-afc9-97211c0a53af"), "Time range filter"));
    }

    private static void RegisterLogCodecs(IServiceProvider serviceProvider)
    {
        var logCodecContainer = serviceProvider.GetRequiredService<ILogCodecContainer>();
        logCodecContainer.RegisterLogCodec<PlainTextProfileLogCodec, PlainTextLogCodecProcessor>(
            new LogCodec(new Guid("a38a40b6-c07f-49d5-a143-5c9f9f42149b"), "Plain Text"));
        logCodecContainer.RegisterLogCodec<XmlProfileLogCodec, XmlLogCodecProcessor>(
            new LogCodec(new Guid("0cb976bc-6d87-4450-8202-530d9db09b40"), "XML"));
    }

    private static void RegisterTypeDiscriminators(IServiceProvider serviceProvider)
    {
        var typeDiscriminators = serviceProvider.GetRequiredService<ITypeDiscriminators>();

        // Filters
        typeDiscriminators.AddMapping<MessageProfileFilter>("msg-profile-filter");
        typeDiscriminators.AddMapping<LoggersProfileFilter>("loggers-profile-filter");
        typeDiscriminators.AddMapping<LogLevelsProfileFilter>("loglevels-profile-filter");
        typeDiscriminators.AddMapping<ThreadsProfileFilter>("threads-profile-filter");
        typeDiscriminators.AddMapping<TimeAgoProfileFilter>("timeago-profile-filter");
        typeDiscriminators.AddMapping<TimeRangeProfileFilter>("timerange-profile-filter");

        // Codecs
        typeDiscriminators.AddMapping<PlainTextProfileLogCodec>("plaintext-profile-log-codec");
        typeDiscriminators.AddMapping<XmlProfileLogCodec>("xml-profile-log-codec");

        // Misc
        typeDiscriminators.AddMapping<ProfileSettings>("profile-settings");
    }
}
