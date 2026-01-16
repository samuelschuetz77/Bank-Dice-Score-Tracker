using BankDice.Maui.Services;

namespace BankDice.Maui.ViewModels;

public sealed class HomeViewModel : ObservableObject
{
    private readonly GameSessionService _session;

    public HomeViewModel(GameSessionService session)
    {
        _session = session;
        StartOrRestartCommand = new RelayCommand(StartOrRestart);
        GoSetupCommand = new RelayCommand(async () => await Shell.Current.GoToAsync("//setup"));
    }

    public string Title => "Bank Dice";
    public string RulesText =>
@"Bank is a press-your-luck dice game.
• 20-ish rounds (you set 7–30)
• Shared pot (“Bank”) grows with rolls
• First 3 rolls: safe (7 adds 70, doubles add sum, else add sum)
• Roll 4+: doubles DOUBLE the pot, 7 wipes pot and ends round for unbanked players
• After roll 3, any player can Bank before the next roll to lock the pot for the round.";

    public string StartButtonText =>
        _session.HasActiveGame ? "Quit Existing Game and Start a New Game" : "Start a New Game";

    public Command StartOrRestartCommand { get; }
    public Command GoSetupCommand { get; }

    private async void StartOrRestart()
    {
            if (_session.HasActiveGame)
            {
                _session.QuitGame();
                Raise(nameof(StartButtonText));
            }

            await Shell.Current.GoToAsync("//setup");
        }

    public void Refresh() => Raise(nameof(StartButtonText));
}
