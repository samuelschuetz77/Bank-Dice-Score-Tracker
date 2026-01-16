using BankDice.Maui.Game;
using BankDice.Maui.Models;

namespace BankDice.Maui.Services;

public sealed class GameSessionService
{
    public bool HasActiveGame { get; private set; }

    public List<Player> Players { get; } = new();
    public int TargetRounds { get; private set; } = 20;

    // Scores stored per player per round
    public Dictionary<Guid, List<RoundScore>> ScoreByPlayer { get; } = new();

    // For current round
    public HashSet<Guid> BankedThisRound { get; } = new();

    public BankGameEngine Engine { get; } = new();

    public void QuitGame()
    {
        HasActiveGame = false;
        Players.Clear();
        ScoreByPlayer.Clear();
        BankedThisRound.Clear();
        // Engine is reusable; state will be overwritten on next start.
    }

    public void StartGame(List<string> orderedNames, int rounds)
    {
        QuitGame(); // clear
        TargetRounds = rounds;

        foreach (var n in orderedNames)
        {
            var name = (n ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Player name cannot be blank.");

            Players.Add(new Player { Name = name });
        }

        foreach (var p in Players)
            ScoreByPlayer[p.Id] = new List<RoundScore>();

        Engine.StartNewGame(Players.Select(p => p.Id), rounds);
        BankedThisRound.Clear();

        HasActiveGame = true;
    }

    public IReadOnlyList<Guid> OrderedPlayerIds => Players.Select(p => p.Id).ToList();

    public string GetPlayerName(Guid id) => Players.First(p => p.Id == id).Name;

    public int GetTotal(Guid playerId)
    {
        if (!ScoreByPlayer.TryGetValue(playerId, out var list) || list.Count == 0) return 0;
        return list[^1].RunningTotal;
    }

    public bool IsPlayerInRound(Guid id) => Engine.InRound.Contains(id);

    public void RecordBankForPlayers(IEnumerable<Guid> bankedIds, int roundNumber, int bankPot)
    {
        foreach (var id in bankedIds)
        {
            BankedThisRound.Add(id);

            var list = ScoreByPlayer[id];
            var prevTotal = list.Count == 0 ? 0 : list[^1].RunningTotal;

            list.Add(new RoundScore
            {
                RoundNumber = roundNumber,
                Points = bankPot,
                RunningTotal = prevTotal + bankPot
            });
        }
    }

    public void RecordZeroForUnbankedOnSeven(int roundNumber)
    {
        var unbanked = Players.Select(p => p.Id)
            .Where(id => !BankedThisRound.Contains(id))
            .ToList();

        foreach (var id in unbanked)
        {
            var list = ScoreByPlayer[id];
            var prevTotal = list.Count == 0 ? 0 : list[^1].RunningTotal;

            list.Add(new RoundScore
            {
                RoundNumber = roundNumber,
                Points = 0,
                RunningTotal = prevTotal
            });
        }
    }

    public void RecordZeroForPlayersWhoNeverBankedBecauseAllOthersBankedDoesNotApply()
    {
        // Not used; when all bank, everyone has banked.
    }

    public void StartNextRound()
    {
        BankedThisRound.Clear();
        Engine.NextRound(OrderedPlayerIds);
    }

    public bool IsGameOverAndWinnerDetermined(out Guid winnerId)
    {
        winnerId = Guid.Empty;

        // Only consider "official end" once we have reached TargetRounds OR beyond.
        // But ties are not allowed: if tie at top after TargetRounds, keep playing extra rounds until broken.

        // We can only decide if everybody has a score entry for the latest round number.
        var round = Engine.CurrentRoundNumber;
        var allHaveThisRound = Players.All(p => ScoreByPlayer[p.Id].Any(rs => rs.RoundNumber == round));
        if (!allHaveThisRound) return false;

        // If we are below TargetRounds, not done.
        if (round < TargetRounds) return false;

        // Find top score and see if unique.
        var totals = Players.Select(p => (p.Id, Total: GetTotal(p.Id))).ToList();
        var max = totals.Max(t => t.Total);
        var top = totals.Where(t => t.Total == max).ToList();

        if (top.Count == 1)
        {
            winnerId = top[0].Id;
            return true;
        }

        // Tie -> keep playing (extra rounds)
        return false;
    }
}
