global using Genius.Atom.Infrastructure;

using System.Diagnostics.CodeAnalysis;
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

namespace Genius.Starlog.Core;

[ExcludeFromCodeCoverage]
public static class Module
{
    public static void Configure(IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<ProfileRepository>();
        services.AddSingleton<IProfileRepository>(sp => sp.GetRequiredService<ProfileRepository>());
        services.AddSingleton<SettingsRepository>();
        services.AddSingleton<ISettingsQueryService>(sp => sp.GetRequiredService<SettingsRepository>());
        services.AddSingleton<ISettingsRepository>(sp => sp.GetRequiredService<SettingsRepository>());

        // Query services
        services.AddSingleton<IProfileQueryService>(sp => sp.GetService<ProfileRepository>()!);

        // LogFlow and LogFiltering services
        services.AddSingleton<LogContainer>();
        services.AddSingleton<ICurrentProfile>(x => x.GetRequiredService<LogContainer>());
        services.AddSingleton<ILogContainer>(x => x.GetRequiredService<LogContainer>());
        services.AddSingleton<ILogFilterContainer, LogFilterContainer>();
        services.AddSingleton<ILogReaderContainer, LogReaderContainer>();
        services.AddTransient<IFilterProcessor, ThreadsFilterProcessor>();
        services.AddTransient<IFilterProcessor, LoggersFilterProcessor>();
        services.AddTransient<IFilterProcessor, LogLevelsFilterProcessor>();
        services.AddTransient<IFilterProcessor, LogSeveritiesFilterProcessor>();
        services.AddTransient<ILogReaderProcessor, PlainTextProfileLogReaderProcessor>();
        services.AddTransient<ILogReaderProcessor, XmlProfileLogReaderProcessor>();
        services.AddTransient<ILogRecordMatcher, LogRecordMatcher>();
        services.AddTransient<IQuickFilterProvider, QuickFilterProvider>();

        // Services
        // TO BE DONE LATER

        // Converters
        services.AddSingleton<IJsonConverter, LogReaderJsonConverter>();
        services.AddSingleton<IJsonConverter, LogFilterJsonConverter>();

        // Command Handlers
        services.AddScoped<ICommandHandler<ProfileCreateCommand, Guid>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileUpdateCommand>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileDeleteCommand>, ProfileDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileFilterCreateOrUpdateCommand, ProfileFilterCreateOrUpdateCommandResult>, ProfileFilterCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileFilterDeleteCommand>, ProfileFilterDeleteCommandHandler>();
        services.AddScoped<ICommandHandler<SettingsUpdateCommand>, SettingsUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<SettingsUpdateAutoLoadingProfileCommand>, SettingsUpdateAutoLoadingProfileCommandHandler>();
    }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        var logFilterContainer = serviceProvider.GetRequiredService<ILogFilterContainer>();

        logFilterContainer.RegisterLogFilter<LoggersProfileFilter, LoggersFilterProcessor>(
            new LogFilter(new Guid("ad1398bc-a17e-4584-b7fa-d82fa547b5fe"), "Logger filter"));
        logFilterContainer.RegisterLogFilter<LogLevelsProfileFilter, LogLevelsFilterProcessor>(
            new LogFilter(new Guid("bd1ffa05-8534-4555-ab17-92fd3f53fe13"), "Level filter"));
        logFilterContainer.RegisterLogFilter<LogSeveritiesProfileFilter, LogSeveritiesFilterProcessor>(
            new LogFilter(new Guid("1123d366-5aa1-4e00-b16f-7832b0880ee8"), "Severity filter"));
        logFilterContainer.RegisterLogFilter<ThreadsProfileFilter, ThreadsFilterProcessor>(
            new LogFilter(new Guid("11235ba9-cf84-413c-b094-d2c2c6672f4f"), "Thread filter"));

        var logReaderContainer = serviceProvider.GetRequiredService<ILogReaderContainer>();
        logReaderContainer.RegisterLogReader<PlainTextProfileLogRead, PlainTextProfileLogReaderProcessor>(
            new LogReader(new Guid("a38a40b6-c07f-49d5-a143-5c9f9f42149b"), "Plain Text"));
        logReaderContainer.RegisterLogReader<XmlProfileLogRead, XmlProfileLogReaderProcessor>(
            new LogReader(new Guid("0cb976bc-6d87-4450-8202-530d9db09b40"), "XML"));

        var typeDiscriminators = serviceProvider.GetRequiredService<ITypeDiscriminators>();
        typeDiscriminators.AddMapping<LoggersProfileFilter>("loggers-profile-filter");
        typeDiscriminators.AddMapping<LogLevelsProfileFilter>("loglevels-profile-filter");
        typeDiscriminators.AddMapping<LogSeveritiesProfileFilter>("logseverities-profile-filter");
        typeDiscriminators.AddMapping<ThreadsProfileFilter>("threads-profile-filter");
        typeDiscriminators.AddMapping<PlainTextProfileLogRead>("plaintext-profile-log-reader");
        typeDiscriminators.AddMapping<XmlProfileLogRead>("xml-profile-log-reader");
    }
}
