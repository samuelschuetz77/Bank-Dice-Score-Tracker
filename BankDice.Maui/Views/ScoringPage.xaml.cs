using BankDice.Maui.ViewModels;

namespace BankDice.Maui.Views;

public partial class ScoringPage : ContentPage
{
    private readonly ScoringViewModel _vm;

    public ScoringPage(ScoringViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshFromSession();
    }
}
