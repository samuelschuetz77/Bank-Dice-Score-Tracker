using BankDice.Maui.Services;
using BankDice.Maui.ViewModels;
using BankDice.Maui.Views;
using Microsoft.Extensions.Logging;

namespace BankDice.Maui;

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

        // Services
        builder.Services.AddSingleton<GameSessionService>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

        // ViewModels
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<SetupViewModel>();
        builder.Services.AddSingleton<ScoringViewModel>();
        builder.Services.AddSingleton<StandingsViewModel>();

        // Pages
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<SetupPage>();
        builder.Services.AddSingleton<ScoringPage>();
        builder.Services.AddSingleton<StandingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
