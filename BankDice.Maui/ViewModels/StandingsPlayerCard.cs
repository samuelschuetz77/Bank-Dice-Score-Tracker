using System.Collections.ObjectModel;
using BankDice.Maui.Models;

namespace BankDice.Maui.ViewModels;

public sealed class StandingsPlayerRow : ObservableObject
{
    public string Name { get; }
    public int ThisRoundPoints { get; }
    public int Total { get; }

    public StandingsPlayerRow(string name, int thisRoundPoints, int total)
    {
        Name = name;
        ThisRoundPoints = thisRoundPoints;
        Total = total;
    }
}

public sealed class StandingsPlayerCard : ObservableObject
{
    public string Name { get; }
    public int Total { get; }

    public ObservableCollection<RoundScore> Scores { get; } = new();

    public StandingsPlayerCard(string name, int total, IEnumerable<RoundScore> scores)
    {
        Name = name;
        Total = total;
        foreach (var s in scores.OrderBy(s => s.RoundNumber))
            Scores.Add(s);
    }
}
