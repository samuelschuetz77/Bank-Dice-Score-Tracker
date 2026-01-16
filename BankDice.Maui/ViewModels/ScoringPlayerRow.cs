namespace BankDice.Maui.ViewModels;

public sealed class ScoringPlayerRow : ObservableObject
{
    public Guid PlayerId { get; }
    public string Name { get; }

    private bool _isInRound;
    public bool IsInRound
    {
        get => _isInRound;
        set => SetProperty(ref _isInRound, value);
    }

    private bool _isBanked;
    public bool IsBanked
    {
        get => _isBanked;
        set => SetProperty(ref _isBanked, value);
    }

    private bool _bankSelected;
    public bool BankSelected
    {
        get => _bankSelected;
        set => SetProperty(ref _bankSelected, value);
    }

    public ScoringPlayerRow(Guid id, string name)
    {
        PlayerId = id;
        Name = name;
    }
}
