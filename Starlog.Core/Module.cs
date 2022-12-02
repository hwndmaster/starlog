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
        services.AddSingleton<IProfileRepository>(sp => sp.GetService<ProfileRepository>()!);
        services.AddSingleton<ISettingsRepository, SettingsRepository>();

        // Query services
        services.AddSingleton<IProfileQueryService>(sp => sp.GetService<ProfileRepository>()!);

        // LogFlow and LogFiltering services
        services.AddSingleton<ILogFilterContainer, LogFilterContainer>();
        services.AddSingleton<ILogContainer, LogContainer>();
        services.AddSingleton<ILogReaderContainer, LogReaderContainer>();
        services.AddTransient<IFilterProcessor, ThreadFilterProcessor>();
        services.AddTransient<IFilterProcessor, LoggerFilterProcessor>();
        services.AddTransient<ILogReaderProcessor, PlainTextProfileLogReaderProcessor>();
        services.AddTransient<ILogReaderProcessor, XmlProfileLogReaderProcessor>();

        // Services, Converters
        services.AddSingleton<IJsonConverter, LogReaderJsonConverter>();

        // Command Handlers
        services.AddScoped<ICommandHandler<ProfileCreateCommand, Guid>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileUpdateCommand>, ProfileCreateOrUpdateCommandHandler>();
        services.AddScoped<ICommandHandler<ProfileDeleteCommand>, ProfileDeleteCommandHandler>();
    }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        var logFilterContainer = serviceProvider.GetRequiredService<ILogFilterContainer>();

        logFilterContainer.RegisterLogFilter<ThreadProfileFilter, ThreadFilterProcessor>(
            new LogFilter(new Guid("11235ba9-cf84-413c-b094-d2c2c6672f4f"), "Thread filter"));
        logFilterContainer.RegisterLogFilter<LoggerProfileFilter, LoggerFilterProcessor>(
            new LogFilter(new Guid("ad1398bc-a17e-4584-b7fa-d82fa547b5fe"), "Logger filter"));

        var logReaderContainer = serviceProvider.GetRequiredService<ILogReaderContainer>();
        logReaderContainer.RegisterLogReader<PlainTextProfileLogReader, PlainTextProfileLogReaderProcessor>(
            new LogReader(new Guid("a38a40b6-c07f-49d5-a143-5c9f9f42149b"), "Plain Text"));
        logReaderContainer.RegisterLogReader<XmlProfileLogReader, XmlProfileLogReaderProcessor>(
            new LogReader(new Guid("0cb976bc-6d87-4450-8202-530d9db09b40"), "XML"));

        var typeDiscriminators = serviceProvider.GetRequiredService<ITypeDiscriminators>();
        typeDiscriminators.AddMapping<LoggerProfileFilter>("logger-profile-filter");
        typeDiscriminators.AddMapping<ThreadProfileFilter>("thread-profile-filter");
        typeDiscriminators.AddMapping<PlainTextProfileLogReader>("plaintext-profile-log-reader");
        typeDiscriminators.AddMapping<XmlProfileLogReader>("xml-profile-log-reader");
    }
}
