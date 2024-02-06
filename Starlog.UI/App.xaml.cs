global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Windows;
global using Genius.Atom.Infrastructure;
global using Genius.Atom.UI.Forms;
using System.IO;
using System.Reflection;
using Genius.Starlog.Core;
using Genius.Starlog.UI.AutoGridBuilders;
using Genius.Starlog.UI.Console;
using Genius.Starlog.UI.Controllers;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;
using Genius.Starlog.UI.Views.Comparison;
using Genius.Starlog.UI.Views.LogSearchAndFiltering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.UI;

public partial class App : Application
{
#pragma warning disable CS8618 // These fields are being initialized in OnStartup() method.
    public static IServiceProvider ServiceProvider { get; private set; }
#pragma warning restore CS8618

    // Feature Toggles
    public static readonly bool ComparisonFeatureEnabled = false;

    [Obsolete("Shouldn't be used from anywhere, except from unit tests of non-injectable classes.")]
    internal static void OverrideServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

#if DEBUG
        // This is necessary to manually resolve Atom.UI.Forms's dependencies,
        // when referenced directly to dll, avoiding NuGet.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
            if (args.RequestingAssembly is null)
                return null;
            var proposedPath = Path.GetDirectoryName(args.RequestingAssembly!.Location) + "\\" + args.Name.Split(',')[0] + ".dll";
            if (!File.Exists(proposedPath))
                proposedPath = args.RequestingAssembly.Location;
            if (!File.Exists(proposedPath))
                return null;
            var assembly = Assembly.LoadFile(proposedPath);
            return assembly;
        };
#endif

        var serviceCollection = new ServiceCollection();

        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        Atom.Data.Module.Initialize(ServiceProvider);
        Atom.Infrastructure.Module.Initialize(ServiceProvider);
        Starlog.Core.Module.Initialize(ServiceProvider);
        Atom.UI.Forms.Module.Initialize(ServiceProvider);

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        var mainController = ServiceProvider.GetRequiredService<IMainController>();
        var profileLoadingController = ServiceProvider.GetRequiredService<IProfileLoadingController>();
        var consoleParser = ServiceProvider.GetRequiredService<IConsoleParser>();

        mainWindow.Loaded += (_, __) => mainController.NotifyMainWindowIsLoaded();
        mainWindow.Show();

        consoleParser.Process(e.Args);
        Task.Run(() => profileLoadingController.AutoLoadProfileAsync());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var currentProfile = ServiceProvider.GetRequiredService<ICurrentProfile>();
        (currentProfile as IDisposable)?.Dispose();

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        Atom.Data.Module.Configure(services);
        Atom.Infrastructure.Module.Configure(services);
        var configuration = Atom.UI.Forms.Module.Configure(services, this);
        Starlog.Core.Module.Configure(services, configuration);

        // Controllers
        services.AddSingleton<IComparisonController, ComparisonController>();
        services.AddSingleton<IConsoleController, ConsoleController>();
        services.AddSingleton<IMainController, MainController>();
        services.AddSingleton<IProfileLoadingController, ProfileLoadingController>();

        // Views, View models, View model factories
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton<IMainViewModel, MainViewModel>();
        services.AddSingleton<IErrorsViewModel, ErrorsViewModel>();
        services.AddSingleton<IProfilesViewModel, ProfilesViewModel>();
        services.AddTransient<IProfileViewModel, ProfileViewModel>();
        services.AddSingleton<ILogsViewModel, LogsViewModel>();
        services.AddTransient<ILogsFilteringViewModel, LogsFilteringViewModel>();
        services.AddTransient<ILogsSearchViewModel, LogsSearchViewModel>();
        services.AddSingleton<IComparisonViewModel, ComparisonViewModel>();
        services.AddSingleton<ISettingsViewModel, SettingsViewModel>();

        // AutoGrid builders
        services.AddTransient<ComparisonAutoGridBuilder>();
        services.AddTransient<MessageParsingTestBuilder>();
        services.AddTransient<LogItemAutoGridBuilder>();
        services.AddTransient<PlainTextLinePatternsAutoGridBuilder>();
        services.AddTransient<ProfileAutoGridBuilder>();

        // Services and Helpers:
        services.AddTransient<IClipboardHelper, ClipboardHelper>();
        services.AddTransient<ILogArtifactsFormatter, LogArtifactsFormatter>();
        services.AddTransient<IConsoleParser, ConsoleParser>();
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
