using BigPictureAutoAudioSwitch.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Window = System.Windows.Window;

namespace BigPictureAutoAudioSwitch.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<AboutViewModel>();
    }
}
