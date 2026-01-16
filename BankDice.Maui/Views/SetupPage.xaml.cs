using BankDice.Maui.ViewModels;

namespace BankDice.Maui.Views;

public partial class SetupPage : ContentPage
{
    public SetupPage(SetupViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
