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
            // Another instance is already running - ask it to show its Settings window
            try
            {
                using var showSettingsEvent = EventWaitHandle.OpenExisting(AppConstants.ShowSettingsEventName);
                showSettingsEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // First instance hasn't created the event yet (still starting up) - nothing to do
            }
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
