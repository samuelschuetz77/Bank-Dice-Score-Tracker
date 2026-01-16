using System.Collections.ObjectModel;

namespace BankDice.Maui.Game;

public sealed class BankGameEngine
{
    public int MaxRounds { get; private set; } = 20;

    public int CurrentRoundNumber { get; private set; } = 1; // 1-based
    public int RollCountThisRound { get; private set; } = 0; // 0..N
    public int BankPot { get; private set; } = 0;

    // players still "in" this round
    private readonly HashSet<Guid> _inRound = new();

    public IReadOnlyCollection<Guid> InRound => _inRound;

    public int CurrentRollerIndex { get; private set; } = 0;

    public void StartNewGame(IEnumerable<Guid> playerIds, int maxRounds)
    {
        if (maxRounds < 7 || maxRounds > 30)
            throw new ArgumentOutOfRangeException(nameof(maxRounds), "Rounds must be 7..30.");

        var ids = playerIds.ToList();
        if (ids.Count < 3 || ids.Count > 8)
            throw new ArgumentOutOfRangeException(nameof(playerIds), "Players must be 3..8.");

        MaxRounds = maxRounds;
        CurrentRoundNumber = 1;
        ResetRound(ids);
    }

    public void ResetRound(IReadOnlyList<Guid> orderedPlayerIds)
    {
        RollCountThisRound = 0;
        BankPot = 0;
        _inRound.Clear();
        foreach (var id in orderedPlayerIds)
            _inRound.Add(id);

        CurrentRollerIndex = 0;
    }

    public bool CanBankNow => RollCountThisRound >= BankRules.SafetyRolls;

    public bool IsRoundOver => _inRound.Count == 0;

    public bool ApplyRoll(RollKind kind, int? valueSum, int? doublesSum, IReadOnlyList<Guid> orderedPlayerIds,
        out bool sevenEndedRound)
    {
        sevenEndedRound = false;

        RollCountThisRound++;

        var isSafety = RollCountThisRound <= BankRules.SafetyRolls;

        switch (kind)
        {
            case RollKind.Seven:
                if (isSafety)
                {
                    BankPot += BankRules.SevenBonusInSafety;
                }
                else
                {
                    // Press zone: wipe pot and end round immediately
                    BankPot = 0;
                    sevenEndedRound = true;
                }
                break;

            case RollKind.Doubles:
                if (doublesSum is null || doublesSum < 2 || doublesSum > 12)
                    throw new ArgumentOutOfRangeException(nameof(doublesSum), "Doubles sum must be 2..12.");

                if (isSafety)
                {
                    BankPot += doublesSum.Value; // doubles just add sum
                }
                else
                {
                    // Press zone: doubles -> pot doubles
                    BankPot *= 2;
                }
                break;

            case RollKind.Value:
                if (valueSum is null || valueSum < 2 || valueSum > 12)
                    throw new ArgumentOutOfRangeException(nameof(valueSum), "Value must be 2..12.");

                BankPot += valueSum.Value;
                break;
        }

        if (!sevenEndedRound)
        {
            // Ensure roller index points to someone in-round if possible
            if (_inRound.Count > 0)
            {
                CurrentRollerIndex = NormalizeRollerIndex(orderedPlayerIds);
            }
        }

        return true;
    }

    public int NormalizeRollerIndex(IReadOnlyList<Guid> orderedPlayerIds)
    {
        if (orderedPlayerIds.Count == 0) return 0;

        for (int i = 0; i < orderedPlayerIds.Count; i++)
        {
            var idx = (CurrentRollerIndex + i) % orderedPlayerIds.Count;
            if (_inRound.Contains(orderedPlayerIds[idx]))
                return idx;
        }

        return 0;
    }

    public void AdvanceToNextRoller(IReadOnlyList<Guid> orderedPlayerIds)
    {
        if (_inRound.Count == 0) return;

        for (int step = 1; step <= orderedPlayerIds.Count; step++)
        {
            var idx = (CurrentRollerIndex + step) % orderedPlayerIds.Count;
            if (_inRound.Contains(orderedPlayerIds[idx]))
            {
                CurrentRollerIndex = idx;
                return;
            }
        }
    }

    public void BankPlayers(IEnumerable<Guid> playerIdsToBank)
    {
        foreach (var id in playerIdsToBank)
        {
            _inRound.Remove(id);
        }
    }

    public void EndRoundForceClear()
    {
        _inRound.Clear();
    }

    public void SetCurrentRollerIndex(int index)
    {
        CurrentRollerIndex = index;
    }

    public void NextRound(IReadOnlyList<Guid> orderedPlayerIds)
    {
        CurrentRoundNumber++;
        ResetRound(orderedPlayerIds);
    }
}
