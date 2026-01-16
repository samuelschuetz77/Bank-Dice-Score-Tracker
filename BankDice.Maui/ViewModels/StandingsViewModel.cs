using System.Collections.ObjectModel;
using BankDice.Maui.Models;
using BankDice.Maui.Services;

namespace BankDice.Maui.ViewModels;

public sealed class StandingsViewModel : ObservableObject
{
    private readonly GameSessionService _session;

    public ObservableCollection<StandingsPlayerRow> PlayerRows { get; } = new();
    public ObservableCollection<StandingsPlayerCard> PlayerCards { get; } = new();

    private string _status = "";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public StandingsViewModel(GameSessionService session)
    {
        _session = session;
        Refresh();
    }

    public void Refresh()
    {
        PlayerRows.Clear();
        PlayerCards.Clear();

        if (!_session.HasActiveGame)
        {
            Status = "No active game.";
            return;
        }

        var currentRound = _session.Engine.CurrentRoundNumber;

        // Create simple table rows: Player | This Round | Total
        var ordered = _session.Players
            .Select(p => new
            {
                p.Id,
                p.Name,
                Total = _session.GetTotal(p.Id),
                ThisRoundPoints = _session.ScoreByPlayer[p.Id]
                    .FirstOrDefault(rs => rs.RoundNumber == currentRound)?.Points ?? 0
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        foreach (var p in ordered)
            PlayerRows.Add(new StandingsPlayerRow(p.Name, p.ThisRoundPoints, p.Total));

        Status = "";
    }
}
