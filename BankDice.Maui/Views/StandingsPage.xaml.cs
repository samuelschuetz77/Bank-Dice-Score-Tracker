using BankDice.Maui.ViewModels;

namespace BankDice.Maui.Views;

public partial class StandingsPage : ContentPage
{
    private readonly StandingsViewModel _vm;

    public StandingsPage(StandingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Refresh();
    }
}
