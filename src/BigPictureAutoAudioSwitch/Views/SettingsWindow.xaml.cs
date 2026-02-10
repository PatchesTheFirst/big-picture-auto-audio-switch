using BigPictureAutoAudioSwitch.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Window = System.Windows.Window;

namespace BigPictureAutoAudioSwitch.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        DataContext = _viewModel;
        
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize settings window");
            System.Windows.MessageBox.Show(
                "Failed to load settings. Please try again.",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.Dispose();
    }
}
