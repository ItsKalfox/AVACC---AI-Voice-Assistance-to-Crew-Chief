using System.Net.Http;
using System.Windows;
using AVACC.AI;
using AVACC.AI.Providers;
using AVACC.App.ViewModels;
using AVACC.Audio;
using AVACC.Core.Interfaces;
using AVACC.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AVACC.App;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.DispatcherUnhandledException += (s, args) =>
        {
            var logger = _serviceProvider?.GetService<IEventLogService>();
            logger?.LogError($"Unhandled UI Exception: {args.Exception.Message}", args.Exception.ToString());
            MessageBox.Show($"An unexpected error occurred:\n\n{args.Exception.Message}\n\nDetails:\n{args.Exception}",
                "AVACC Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                var logger = _serviceProvider?.GetService<IEventLogService>();
                logger?.LogError($"Unhandled Domain Exception: {ex.Message}", ex.ToString());
            }
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize Settings & Command Registry
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        await settingsService.LoadSettingsAsync();

        var settings = settingsService.CurrentSettings;

        // Auto-reconcile audio devices: if configured device is disconnected/unplugged, auto-fallback to default and save
        var micManager = _serviceProvider.GetRequiredService<MicrophoneManager>();
        if (micManager.ReconcileAudioDevices(settings, out var changeMsg))
        {
            await settingsService.SaveSettingsAsync(settings);
            var eventLogger = _serviceProvider.GetService<IEventLogService>();
            eventLogger?.LogInfo($"Audio device auto-fallback: {changeMsg}");
        }

        var registryService = _serviceProvider.GetRequiredService<ICommandRegistryService>();
        await registryService.LoadCommandsAsync();

        // Initialize PTT Service
        var pttService = _serviceProvider.GetRequiredService<IPushToTalkService>();
        pttService.Initialize(settings.PttKey, settings.IsGlobalPttEnabled, settings.DisablePttWhenInactive);

        settingsService.SettingsChanged += (s, newSettings) =>
        {
            pttService.UpdateConfiguration(newSettings.PttKey, newSettings.IsGlobalPttEnabled, newSettings.DisablePttWhenInactive);
        };

        // Initialize PTT Sound Feedback Service
        _ = _serviceProvider.GetRequiredService<ISoundEffectService>();

        // Initialize Crew Chief Monitor
        var crewChiefMonitor = _serviceProvider.GetRequiredService<ICrewChiefMonitorService>();
        crewChiefMonitor.StartMonitoring();

        // Auto-launch Crew Chief alongside AVACC if enabled
        if (settings.AutoLaunchCrewChiefOnStartup && !crewChiefMonitor.IsRunning)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(800);
                await crewChiefMonitor.LaunchCrewChiefAsync();
            });
        }

        // Show MainWindow
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging();

        // HTTP Client
        services.AddSingleton<HttpClient>();

        // Audio
        services.AddSingleton<MicrophoneManager>();
        services.AddSingleton<IAudioEndpointService>(sp => sp.GetRequiredService<MicrophoneManager>());
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<IPushToTalkService, PushToTalkService>();
        services.AddSingleton<ISoundEffectService, SoundEffectService>();
        services.AddSingleton<ICrewChiefBridgeService, CrewChiefBridgeService>();

        // Infrastructure
        services.AddSingleton<ISecureKeyStorage, SecureKeyStorage>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ICommandRegistryService, CommandRegistryService>();
        services.AddSingleton<IEventLogService, EventLogService>();
        services.AddSingleton<ICrewChiefMonitorService, CrewChiefMonitorService>();

        // AI Providers
        services.AddSingleton<GeminiProvider>();
        services.AddSingleton<OpenAIProvider>();
        services.AddSingleton<GroqProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();

        services.AddSingleton<IMultimodalVoiceProvider>(sp => sp.GetRequiredService<GeminiProvider>());
        services.AddSingleton<ISpeechToTextProvider>(sp => sp.GetRequiredService<GeminiProvider>());
        services.AddSingleton<ICommandInterpreter>(sp => sp.GetRequiredService<GeminiProvider>());

        services.AddSingleton<SpeechToTextService>();
        services.AddSingleton<IntentInterpreter>();
        services.AddSingleton<IModelDiscoveryService, ModelDiscoveryService>();

        // ViewModels
        services.AddSingleton<VisualCMDViewModel>();
        services.AddSingleton<CommandEditorViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider != null)
        {
            if (_serviceProvider.GetService<IPushToTalkService>() is IDisposable ptt)
            {
                ptt.Dispose();
            }

            if (_serviceProvider.GetService<IAudioCaptureService>() is IDisposable audio)
            {
                audio.Dispose();
            }

            if (_serviceProvider.GetService<ICrewChiefMonitorService>() is IDisposable ccMonitor)
            {
                ccMonitor.Dispose();
            }
        }

        base.OnExit(e);
    }
}
