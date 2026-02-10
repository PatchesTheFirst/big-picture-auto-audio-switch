using System.Threading;

namespace BigPictureAutoAudioSwitch;

public static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main()
    {
        // Ensure single instance
        _mutex = new Mutex(true, AppConstants.MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            return;
        }

        try
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}
