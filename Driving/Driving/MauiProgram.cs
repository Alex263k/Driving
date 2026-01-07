using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio; // 1. Namespace for the cross-platform audio plugin

namespace Driving
{
    /// <summary>
    /// The entry point of the application. Configures the .NET MAUI host, 
    /// registers services, and defines application resources.
    /// </summary>
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 2. Register the Audio Manager as a Singleton.
            // This ensures a single instance handles all game sounds (engine, coins, crashes)
            // across different pages without restarting the audio hardware.
            builder.Services.AddSingleton(AudioManager.Current);

#if DEBUG
            // Enable debug logging for the development environment
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}