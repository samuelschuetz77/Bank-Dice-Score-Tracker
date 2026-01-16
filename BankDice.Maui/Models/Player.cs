namespace BankDice.Maui.Models;

public sealed class Player
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}
