using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace TheMashup_Metadata_Writer;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // Initialize the .NET MAUI Community Toolkit by adding the below line of code
            .UseMauiCommunityToolkit()
            // After initializing the .NET MAUI Community Toolkit, optionally add additional fonts
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("BLACKHAWK.otf", "BLACKHAWK");
                fonts.AddFont("MarkPro-Bold.otf", "MarkPro-Bold");
                fonts.AddFont("MarkPro-Medium.otf", "MarkPro-Medium");
                fonts.AddFont("MarkPro-Light.otf", "MarkPro-Light");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
