using System.Collections.ObjectModel;
using BankDice.Maui.Services;

namespace BankDice.Maui.ViewModels;

public sealed class SetupViewModel : ObservableObject
{
    private readonly GameSessionService _session;

    public ObservableCollection<PlayerSetupRow> Players { get; } = new();

    private int _rounds = 20;
    public int Rounds
    {
        get => _rounds;
        set
        {
            if (SetProperty(ref _rounds, value))
                Raise(nameof(ValidationMessage));
        }
    }

    private string _newName = "";
    public string NewName
    {
        get => _newName;
        set => SetProperty(ref _newName, value);
    }

    public string ValidationMessage
    {
        get
        {
            if (Rounds < 7 || Rounds > 30) return "Rounds must be between 7 and 30.";
            if (Players.Count < 3) return "Need at least 3 players.";
            if (Players.Count > 8) return "Max 8 players.";
            if (Players.Any(p => string.IsNullOrWhiteSpace(p.Name))) return "Player names cannot be blank.";
            return "";
        }
    }

    public bool CanStart => string.IsNullOrWhiteSpace(ValidationMessage);

    private bool _isOrderingMode = false;
    public bool IsOrderingMode
    {
        get => _isOrderingMode;
        set 
        {
            if (SetProperty(ref _isOrderingMode, value))
                Raise(nameof(OrderButtonText));
        }
    }

    public string OrderButtonText => IsOrderingMode ? "Done Adjusting" : "Adjust Order";

    public Command AddPlayerCommand { get; }
    public Command<PlayerSetupRow> RemovePlayerCommand { get; }
    public Command<PlayerSetupRow> MoveUpCommand { get; }
    public Command<PlayerSetupRow> MoveDownCommand { get; }
    public Command ToggleOrderingCommand { get; }
    public Command StartGameCommand { get; }

    public SetupViewModel(GameSessionService session)
    {
        _session = session;

        AddPlayerCommand = new RelayCommand(AddPlayer);
        RemovePlayerCommand = new Command<PlayerSetupRow>(Remove);
        MoveUpCommand = new Command<PlayerSetupRow>(MoveUp);
        MoveDownCommand = new Command<PlayerSetupRow>(MoveDown);
        ToggleOrderingCommand = new RelayCommand(() => IsOrderingMode = !IsOrderingMode);
        StartGameCommand = new RelayCommand(StartGame, () => CanStart);

        Players.CollectionChanged += (_, __) => RefreshOrdersAndValidation();
    }

    private void RefreshOrdersAndValidation()
    {
        for (int i = 0; i < Players.Count; i++)
            Players[i].Order = i + 1;

        Raise(nameof(ValidationMessage));
        Raise(nameof(CanStart));
        StartGameCommand.ChangeCanExecute();
    }

    private void AddPlayer()
    {
        var name = (NewName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        if (Players.Count >= 8) return;

        Players.Add(new PlayerSetupRow { Name = name });
        NewName = "";
        RefreshOrdersAndValidation();
    }

    private void Remove(PlayerSetupRow? row)
    {
        if (row is null) return;
        Players.Remove(row);
        RefreshOrdersAndValidation();
    }

    private void MoveUp(PlayerSetupRow? row)
    {
        if (row is null) return;
        var idx = Players.IndexOf(row);
        if (idx <= 0) return;

        Players.RemoveAt(idx);
        Players.Insert(idx - 1, row);
        RefreshOrdersAndValidation();
    }

    private void MoveDown(PlayerSetupRow? row)
    {
        if (row is null) return;
        var idx = Players.IndexOf(row);
        if (idx < 0 || idx >= Players.Count - 1) return;

        Players.RemoveAt(idx);
        Players.Insert(idx + 1, row);
        RefreshOrdersAndValidation();
    }

    private async void StartGame()
    {
        if (!CanStart) return;

        var names = Players.Select(p => p.Name).ToList();
        _session.StartGame(names, Rounds);

        await Shell.Current.GoToAsync("//scoring");
    }
}
