namespace BankDice.Maui.ViewModels;

public sealed class PlayerSetupRow : ObservableObject
{
    private int _order;
    public int Order
    {
        get => _order;
        set => SetProperty(ref _order, value);
    }

    private string _name = "";
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
