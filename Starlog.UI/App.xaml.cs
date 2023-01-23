global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Windows;
global using Genius.Atom.Infrastructure;
global using Genius.Atom.UI.Forms;

using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.LogSearchAndFiltering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI;

public partial class App : Application
{
#pragma warning disable CS8618 // These fields are being initialized in OnStartup() method.
    public static IServiceProvider ServiceProvider { get; private set; }
#pragma warning restore CS8618

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
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

    private void ConfigureServices(IServiceCollection services)
    {
        Atom.Data.Module.Configure(services);
        Atom.Infrastructure.Module.Configure(services);
        Atom.UI.Forms.Module.Configure(services, this);
        Starlog.Core.Module.Configure(services);

        // Framework:
        services.AddLogging(x =>
        {
#if DEBUG
            x.SetMinimumLevel(LogLevel.Trace);
            x.AddDebug();
#endif
        });

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
        services.AddSingleton<ISettingsViewModel, SettingsViewModel>();

        // AutoGrid builders
        services.AddTransient<ProfileAutoGridBuilder>();
        services.AddTransient<LogItemAutoGridBuilder>();
        services.AddTransient<PlainTextLineRegexTemplatesAutoGridBuilder>();

        // Services and Helpers:
        services.AddTransient<ILogArtifactsFormatter, LogArtifactsFormatter>();
        services.AddTransient<IConsoleParser, ConsoleParser>();
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
#if !DEBUG
        MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
#endif
    }
}
