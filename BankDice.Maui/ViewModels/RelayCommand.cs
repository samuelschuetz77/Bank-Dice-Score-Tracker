namespace BankDice.Maui.ViewModels;

public sealed class RelayCommand : Command
{
    public RelayCommand(Action execute) : base(execute) { }
    public RelayCommand(Action execute, Func<bool> canExecute) : base(execute, canExecute) { }
}

public sealed class RelayCommand<T> : Command
{
    public RelayCommand(Action<T> execute) : base(o => execute((T)o!)) { }
    public RelayCommand(Action<T> execute, Func<T, bool> canExecute) : base(o => canExecute((T)o!)) { }
}
