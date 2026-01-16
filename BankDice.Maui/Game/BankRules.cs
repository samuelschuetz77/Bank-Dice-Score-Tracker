namespace BankDice.Maui.Game;

public static class BankRules
{
    public const int SafetyRolls = 3;
    public const int SevenBonusInSafety = 70;

    public static bool IsSeven(int sum) => sum == 7;
    public static bool IsDoubles(int die1, int die2) => die1 == die2;
}
