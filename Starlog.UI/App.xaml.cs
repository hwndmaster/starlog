global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Windows;
global using Genius.Atom.Infrastructure;
global using Genius.Atom.UI.Forms;
using Genius.Starlog.Core;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.Comparison;
using Genius.Starlog.UI.Views.LogSearchAndFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Genius.Starlog.UI;

public partial class App : Application
{
#pragma warning disable CS8618 // These fields are being initialized in OnStartup() method.
    public static IServiceProvider ServiceProvider { get; private set; }
#pragma warning restore CS8618

    // Feature Toggles
    public static readonly bool ComparisonFeatureEnabled = false;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();

        var config = LoadConfiguration(serviceCollection);
        ConfigureLogging(serviceCollection, config);
        ConfigureServices(serviceCollection, config);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        Atom.Data.Module.Initialize(ServiceProvider);
        Atom.Infrastructure.Module.Initialize(ServiceProvider);
        Starlog.Core.Module.Initialize(ServiceProvider);
        Atom.UI.Forms.Module.Initialize(ServiceProvider);

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        var mainController = ServiceProvider.GetRequiredService<IMainController>();
        var consoleParser = ServiceProvider.GetRequiredService<IConsoleParser>();

        mainWindow.Loaded += (_, __) => mainController.NotifyMainWindowIsLoaded();
        mainWindow.Show();

        consoleParser.Process(e.Args);
        Task.Run(() => mainController.AutoLoadProfileAsync());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var currentProfile = ServiceProvider.GetRequiredService<ICurrentProfile>();
        (currentProfile as IDisposable)?.Dispose();

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        Atom.Data.Module.Configure(services);
        Atom.Infrastructure.Module.Configure(services);
        Atom.UI.Forms.Module.Configure(services, this);
        Starlog.Core.Module.Configure(services);

        // Views, View models, View model factories, Controllers
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IMainController, MainController>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IMainViewModel, MainViewModel>();
        services.AddSingleton<IProfilesViewModel, ProfilesViewModel>();
        services.AddTransient<IProfileViewModel, ProfileViewModel>();
        services.AddSingleton<ILogsViewModel, LogsViewModel>();
        services.AddTransient<ILogsFilteringViewModel, LogsFilteringViewModel>();
        services.AddTransient<ILogsSearchViewModel, LogsSearchViewModel>();
        services.AddSingleton<IComparisonViewModel, ComparisonViewModel>();
        services.AddSingleton<ISettingsViewModel, SettingsViewModel>();

        // AutoGrid builders
        services.AddTransient<ComparisonAutoGridBuilder>();
        services.AddTransient<LogItemAutoGridBuilder>();
        services.AddTransient<PlainTextLineRegexTemplatesAutoGridBuilder>();
        services.AddTransient<ProfileAutoGridBuilder>();

        // Services and Helpers:
        services.AddTransient<IClipboardHelper, ClipboardHelper>();
        services.AddTransient<ILogArtifactsFormatter, LogArtifactsFormatter>();
        services.AddTransient<IConsoleParser, ConsoleParser>();
    }

    private static IConfiguration LoadConfiguration(ServiceCollection serviceCollection)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true);
        IConfiguration config = builder.Build();
        serviceCollection.AddSingleton<IConfiguration>(config);
        return config;
    }

    private static void ConfigureLogging(ServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        // Framework:
        services.AddLogging(x =>
        {
            x.AddSerilog();
        });
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var logger = ServiceProvider.GetService<ILogger<App>>();
            logger?.LogCritical(e.Exception, e.Exception.Message);
        }
        catch (Exception) {}

#if !DEBUG
        MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
#endif
    }
}
