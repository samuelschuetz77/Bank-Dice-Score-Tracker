using BankDice.Maui.Game;
using Xunit;

namespace BankDice.Tests;

public sealed class BankGameEngineTests
{
    private static List<Guid> Players(int n)
        => Enumerable.Range(0, n).Select(_ => Guid.NewGuid()).ToList();

    [Fact]
    public void StartNewGame_RejectsRoundsBelow7()
    {
        var e = new BankGameEngine();
        Assert.Throws<ArgumentOutOfRangeException>(() => e.StartNewGame(Players(3), 6));
    }

    [Fact]
    public void StartNewGame_RejectsRoundsAbove30()
    {
        var e = new BankGameEngine();
        Assert.Throws<ArgumentOutOfRangeException>(() => e.StartNewGame(Players(3), 31));
    }

    [Fact]
    public void StartNewGame_RejectsPlayersBelow3()
    {
        var e = new BankGameEngine();
        Assert.Throws<ArgumentOutOfRangeException>(() => e.StartNewGame(Players(2), 7));
    }

    [Fact]
    public void StartNewGame_RejectsPlayersAbove8()
    {
        var e = new BankGameEngine();
        Assert.Throws<ArgumentOutOfRangeException>(() => e.StartNewGame(Players(9), 7));
    }

    [Fact]
    public void SafetyZone_SevenAdds70()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended);
        Assert.False(ended);
        Assert.Equal(70, e.BankPot);
        Assert.Equal(1, e.RollCountThisRound);
    }

    [Fact]
    public void SafetyZone_DoublesAddsSumOnly()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.ApplyRoll(RollKind.Doubles, null, 8, ids, out var ended);
        Assert.False(ended);
        Assert.Equal(8, e.BankPot);
    }

    [Fact]
    public void SafetyZone_ValueAddsSum()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.ApplyRoll(RollKind.Value, 9, null, ids, out _);
        Assert.Equal(9, e.BankPot);
    }

    [Fact]
    public void BankingNotAllowedBeforeRoll3()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        Assert.False(e.CanBankNow);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        Assert.False(e.CanBankNow);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        Assert.False(e.CanBankNow);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        Assert.True(e.CanBankNow);
    }

    [Fact]
    public void PressZone_DoublesDoublesPot()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _); // pot = 18

        e.ApplyRoll(RollKind.Doubles, null, 4, ids, out var ended); // doubles on roll4
        Assert.False(ended);
        Assert.Equal(36, e.BankPot);
    }

    [Fact]
    public void PressZone_SevenWipesPotAndEndsRound()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);

        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended);
        Assert.True(ended);
        Assert.Equal(0, e.BankPot);
    }

    [Fact]
    public void BankPlayers_RemovesFromInRound()
    {
        var ids = Players(4);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.BankPlayers(new[] { ids[1], ids[3] });
        Assert.DoesNotContain(ids[1], e.InRound);
        Assert.DoesNotContain(ids[3], e.InRound);
        Assert.Contains(ids[0], e.InRound);
        Assert.Contains(ids[2], e.InRound);
    }

    [Fact]
    public void RoundEnds_WhenAllPlayersBank()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.BankPlayers(ids);
        Assert.True(e.IsRoundOver);
    }

    [Fact]
    public void AdvanceToNextRoller_SkipsBankedPlayers()
    {
        var ids = Players(4);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        // current roller index 0
        e.BankPlayers(new[] { ids[1] }); // remove player 2
        e.AdvanceToNextRoller(ids);

        Assert.Equal(2, e.CurrentRollerIndex); // should skip index 1
    }

    [Fact]
    public void NormalizeRollerIndex_PointsToInRoundPlayer()
    {
        var ids = Players(4);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.SetCurrentRollerIndex(1);
        e.BankPlayers(new[] { ids[1] });
        var idx = e.NormalizeRollerIndex(ids);
        Assert.NotEqual(1, idx);
        Assert.True(e.InRound.Contains(ids[idx]));
    }

    [Fact]
    public void DoublesSum_OutOfRange_Throws()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            e.ApplyRoll(RollKind.Doubles, null, 13, ids, out _));
    }

    [Fact]
    public void ValueSum_OutOfRange_Throws()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            e.ApplyRoll(RollKind.Value, 1, null, ids, out _));
    }

    [Fact]
    public void NextRound_ResetsBankAndRollCount()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);

        e.ApplyRoll(RollKind.Value, 10, null, ids, out _);
        Assert.True(e.BankPot > 0);
        Assert.True(e.RollCountThisRound > 0);

        e.NextRound(ids);
        Assert.Equal(2, e.CurrentRoundNumber);
        Assert.Equal(0, e.BankPot);
        Assert.Equal(0, e.RollCountThisRound);
        Assert.Equal(3, e.InRound.Count);
    }

    // --------- Remaining tests to reach 30 (edge-cases & combos) ---------

    [Fact]
    public void SafetyZone_ThreeRollsThenCanBank()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 2, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 2, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 2, null, ids, out _);
        Assert.True(e.CanBankNow);
    }

    [Fact]
    public void PressZone_DoublesAfterSafety_UsesMultiplyNotAdd()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 5, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 5, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 5, null, ids, out _); // 15
        e.ApplyRoll(RollKind.Doubles, null, 12, ids, out _); // double -> 30
        Assert.Equal(30, e.BankPot);
    }

    [Fact]
    public void SafetyZone_DoublesDoesNotDoublePot()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 10, null, ids, out _);
        e.ApplyRoll(RollKind.Doubles, null, 4, ids, out _); // +4, not *2
        Assert.Equal(14, e.BankPot);
    }

    [Fact]
    public void PressZone_ValueAddsAfterSafety()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _); // 18
        e.ApplyRoll(RollKind.Value, 8, null, ids, out _);  // 26
        Assert.Equal(26, e.BankPot);
    }

    [Fact]
    public void AdvanceToNextRoller_DoesNothingIfNoInRound()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.BankPlayers(ids);
        e.AdvanceToNextRoller(ids);
        Assert.Equal(0, e.CurrentRollerIndex);
    }

    [Fact]
    public void EndRoundForceClear_ClearsInRound()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.EndRoundForceClear();
        Assert.Empty(e.InRound);
    }

    [Fact]
    public void ApplyRoll_IncrementsRollCountAlways()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        Assert.Equal(2, e.RollCountThisRound);
    }

    [Fact]
    public void PressZone_SevenDoesNotIncrementPot()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 10, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 10, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 10, null, ids, out _); // 30
        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended);
        Assert.True(ended);
        Assert.Equal(0, e.BankPot);
    }

    [Fact]
    public void SafetyZone_SevenCanStackMultipleTimes()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out _);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out _);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out _);
        Assert.Equal(210, e.BankPot);
        Assert.Equal(3, e.RollCountThisRound);
    }

    [Fact]
    public void NormalizeRollerIndex_WhenCurrentIsInRound_ReturnsSame()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.SetCurrentRollerIndex(1);
        Assert.Equal(1, e.NormalizeRollerIndex(ids));
    }

    [Fact]
    public void AdvanceToNextRoller_WrapsAround()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.SetCurrentRollerIndex(2);
        e.AdvanceToNextRoller(ids);
        Assert.Equal(0, e.CurrentRollerIndex);
    }

    [Fact]
    public void BankPlayers_DuplicateIdsNoCrash()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.BankPlayers(new[] { ids[0], ids[0] });
        Assert.DoesNotContain(ids[0], e.InRound);
        Assert.Equal(2, e.InRound.Count);
    }

    [Fact]
    public void ApplyRoll_ValueSumNullThrows()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            e.ApplyRoll(RollKind.Value, null, null, ids, out _));
    }

    [Fact]
    public void ApplyRoll_DoublesSumNullThrows()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            e.ApplyRoll(RollKind.Doubles, null, null, ids, out _));
    }

    [Fact]
    public void PressZone_DoublesCanChain()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _);
        e.ApplyRoll(RollKind.Value, 6, null, ids, out _); // 18
        e.ApplyRoll(RollKind.Doubles, null, 2, ids, out _); // 36
        e.ApplyRoll(RollKind.Doubles, null, 2, ids, out _); // 72
        Assert.Equal(72, e.BankPot);
    }

    [Fact]
    public void NextRound_IncrementsRoundNumber()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.NextRound(ids);
        Assert.Equal(2, e.CurrentRoundNumber);
    }

    [Fact]
    public void ResetRound_SetsAllPlayersInRound()
    {
        var ids = Players(4);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.BankPlayers(new[] { ids[0], ids[1] });
        e.ResetRound(ids);
        Assert.Equal(4, e.InRound.Count);
    }

    [Fact]
    public void ApplyRoll_DoesNotEndRoundInSafetyOnSeven()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended);
        Assert.False(ended);
    }

    [Fact]
    public void PressZone_SevenFlagTrueOnlyAfterRoll3()
    {
        var ids = Players(3);
        var e = new BankGameEngine();
        e.StartNewGame(ids, 7);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended1);
        Assert.False(ended1);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended2);
        Assert.False(ended2);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended3);
        Assert.False(ended3);
        e.ApplyRoll(RollKind.Seven, null, null, ids, out var ended4);
        Assert.True(ended4);
    }
}
