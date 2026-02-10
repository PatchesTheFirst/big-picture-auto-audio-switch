using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using BigPictureAutoAudioSwitch.Services;
using BigPictureAutoAudioSwitch.ViewModels;
using BigPictureAutoAudioSwitch.Views;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Application = System.Windows.Application;

namespace BigPictureAutoAudioSwitch;

public partial class App : Application
{
    private static readonly LoggingLevelSwitch LevelSwitch = new(LogEventLevel.Information);
    
    private readonly IHost _host;
    private TaskbarIcon? _trayIcon;
    private IThemeService? _themeService;

    public App()
    {
        var logPath = Path.Combine(AppConstants.LogsFolder, "app-.log");

        // Configure Serilog with dynamic level switch
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LevelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithThreadId()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: AppConstants.LogRetainedFileCount,
                fileSizeLimitBytes: AppConstants.LogFileSizeLimitBytes,  // 50MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Logging level switch for dynamic control
                services.AddSingleton(LevelSwitch);
                
                // Services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<ILoggingService, LoggingService>();
                services.AddSingleton<IAudioService, AudioService>();
                services.AddSingleton<IBigPictureDetector, BigPictureDetector>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IStartupService, StartupService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ISettingsValidator, SettingsValidator>();

                // ViewModels
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AboutViewModel>();
                services.AddSingleton<TrayIconViewModel>();

                // Views
                services.AddTransient<SettingsWindow>();
                services.AddTransient<AboutWindow>();
            })
            .Build();
    }

    public static IServiceProvider Services => ((App)Current)._host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log.Fatal((Exception)args.ExceptionObject, "Unhandled exception");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled dispatcher exception");
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        try
        {
            await _host.StartAsync();

            // Initialize theme service first
            _themeService = Services.GetRequiredService<IThemeService>();
            _themeService.ApplyTheme();
            _themeService.ThemeChanged += OnThemeChanged;
            _themeService.StartListening();

            // Initialize services
            var settingsService = Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            // Check and apply logging level (with auto-disable check)
            var loggingService = Services.GetRequiredService<ILoggingService>();
            await loggingService.CheckAutoDisableAsync();

            // Log startup information
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Log.Information("Application starting. Version: {Version}, OS: {OS}, Runtime: {Runtime}",
                version, Environment.OSVersion, RuntimeInformation.FrameworkDescription);

            if (settingsService.Settings.VerboseLogging)
            {
                Log.Warning("Verbose logging is enabled - logs will be larger than normal");
            }

            var audioService = Services.GetRequiredService<IAudioService>();
            audioService.Initialize();

            var detector = Services.GetRequiredService<IBigPictureDetector>();
            detector.Start();

            // Create tray icon
            _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            _trayIcon.DataContext = Services.GetRequiredService<TrayIconViewModel>();
            _trayIcon.ForceCreate();
            
            // Apply theme to context menu initially
            UpdateContextMenuTheme();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during application startup");
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }

    private void OnThemeChanged(object? sender, bool isDarkMode)
    {
        // Update context menu theme when Windows theme changes
        Dispatcher.Invoke(UpdateContextMenuTheme);
    }

    private void UpdateContextMenuTheme()
    {
        if (_trayIcon?.ContextMenu == null || _themeService == null) return;
        _themeService.ApplyThemeToContextMenu(_trayIcon.ContextMenu);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Application shutting down");
            
            // Clear any pending toast notifications
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to clear toast notifications");
            }
            
            // Stop theme listening
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
                _themeService.StopListening();
            }

            var detector = Services.GetRequiredService<IBigPictureDetector>();
            detector.Stop();

            // Dispose AudioService (implements IDisposable)
            if (Services.GetRequiredService<IAudioService>() is IDisposable disposableAudio)
            {
                disposableAudio.Dispose();
            }

            // Dispose TrayIconViewModel (implements IDisposable)
            if (Services.GetRequiredService<TrayIconViewModel>() is IDisposable disposableTrayVm)
            {
                disposableTrayVm.Dispose();
            }

            _trayIcon?.Dispose();

            await _host.StopAsync();
            _host.Dispose();
            
            // Flush and close Serilog
            await Log.CloseAndFlushAsync();
        }
        catch (Exception ex)
        {
            // Best effort logging during shutdown - don't throw
            try
            {
                Log.Error(ex, "Error during application shutdown");
                await Log.CloseAndFlushAsync();
            }
            catch
            {
                // Ignore - we're shutting down anyway
            }
        }

        base.OnExit(e);
    }
}
