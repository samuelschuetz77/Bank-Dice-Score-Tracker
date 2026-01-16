using System.Collections.ObjectModel;
using BankDice.Maui.Game;
using BankDice.Maui.Services;

namespace BankDice.Maui.ViewModels;

public sealed class ScoringViewModel : ObservableObject
{
    private readonly GameSessionService _session;

    private enum TurnStep
    {
        Rolling,
        Banking,
        RoundEnded
    }

    public ObservableCollection<ScoringPlayerRow> Players { get; } = new();

    private TurnStep _step = TurnStep.Rolling;

    public int RoundNumber => _session.Engine.CurrentRoundNumber;
    public int RollCount => _session.Engine.RollCountThisRound;
    public int BankPot => _session.Engine.BankPot;

    public bool CanEnterRoll => _session.HasActiveGame && _step == TurnStep.Rolling;
    public bool CanContinue => _session.HasActiveGame && _step == TurnStep.Banking;

    public string NextActionText =>
        _step switch
        {
            TurnStep.Rolling => RollCount < BankRules.SafetyRolls ? "Safety zone: enter the next roll (banking not allowed yet)." 
                                              : "Enter the next roll.",
            TurnStep.Banking => "Banking phase: select who banks, or continue to next roller.",
            TurnStep.RoundEnded => "Round ended. Start the next round.",
            _ => ""
        };

    public string ContinueButtonText =>
        Players.Any(p => p.IsInRound && p.BankSelected)
            ? "Confirm Banking"
            : "Nobody Banks ? Next Roller";

    public bool ShowBankingUI => _step == TurnStep.Banking;
    public bool ShowNextRound => _step == TurnStep.RoundEnded;

    public string CurrentRollerName
    {
        get
        {
            if (!_session.HasActiveGame || _session.Players.Count == 0) return "";
            var ordered = _session.OrderedPlayerIds;
            var idx = _session.Engine.CurrentRollerIndex;
            idx = Math.Clamp(idx, 0, ordered.Count - 1);
            return _session.GetPlayerName(ordered[idx]);
        }
    }

    // Roll Modal UI
    private bool _showRollModal;
    public bool ShowRollModal
    {
        get => _showRollModal;
        set => SetProperty(ref _showRollModal, value);
    }

    private bool _showSumEntry;
    public bool ShowSumEntry
    {
        get => _showSumEntry;
        set => SetProperty(ref _showSumEntry, value);
    }

    private string _sumPromptText = "";
    public string SumPromptText
    {
        get => _sumPromptText;
        set => SetProperty(ref _sumPromptText, value);
    }

    private string _sumText = "";
    public string SumText
    {
        get => _sumText;
        set => SetProperty(ref _sumText, value);
    }

    private RollKind _pendingKind;
    private int? _pendingSum; // for Seven we won't need it

    // Legacy Roll Prompt UI (keeping for compatibility)
    private bool _showValuePrompt;
    public bool ShowValuePrompt
    {
        get => _showValuePrompt;
        set => SetProperty(ref _showValuePrompt, value);
    }

    private string _promptTitle = "";
    public string PromptTitle
    {
        get => _promptTitle;
        set => SetProperty(ref _promptTitle, value);
    }

    private string _promptValue = "";
    public string PromptValue
    {
        get => _promptValue;
        set => SetProperty(ref _promptValue, value);
    }

    // Banking phase UI (after a roll, only when roll >= 3 and round not ended)
    private bool _isBankingPhase;
    public bool IsBankingPhase
    {
        get => _isBankingPhase;
        set => SetProperty(ref _isBankingPhase, value);
    }

    private bool _showBankConfirm;
    public bool ShowBankConfirm
    {
        get => _showBankConfirm;
        set => SetProperty(ref _showBankConfirm, value);
    }

    public string BankConfirmText
    {
        get
        {
            var names = Players.Where(p => p.BankSelected && p.IsInRound)
                               .Select(p => p.Name)
                               .ToList();
            return names.Count == 0
                ? "No players selected."
                : $"Bank {BankPot} points for: {string.Join(", ", names)} ?";
        }
    }

    // Status text
    private string _status = "";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private string _modalError = "";
    public string ModalError
    {
        get => _modalError;
        set => SetProperty(ref _modalError, value);
    }

    public Command RollSevenCommand { get; }
    public Command RollDoublesCommand { get; }
    public Command RollValueCommand { get; }
    public Command PromptOkCommand { get; }
    public Command PromptCancelCommand { get; }

    public Command OpenRollModalCommand { get; }
    public Command CancelRollCommand { get; }
    public Command ChooseSevenCommand { get; }
    public Command ChooseDoublesCommand { get; }
    public Command ChooseValueCommand { get; }
    public Command ConfirmRollCommand { get; }

    public Command ContinueCommand { get; }
    public Command AdvanceRollerCommand { get; }
    public Command OpenBankConfirmCommand { get; }
    public Command ConfirmBankYesCommand { get; }
    public Command ConfirmBankNoCommand { get; }

    public Command NextRoundCommand { get; }
    public Command GoToStandingsCommand { get; }

    public ScoringViewModel(GameSessionService session)
    {
        _session = session;

        RollSevenCommand = new RelayCommand(() => ApplyRoll(RollKind.Seven, null));
        RollDoublesCommand = new RelayCommand(() => OpenPrompt(RollKind.Doubles, "Enter the doubles sum (2–12)"));
        RollValueCommand = new RelayCommand(() => OpenPrompt(RollKind.Value, "Enter the roll sum (2–12)"));

        PromptOkCommand = new RelayCommand(PromptOk);
        PromptCancelCommand = new RelayCommand(() => { ShowValuePrompt = false; PromptValue = ""; });

        OpenRollModalCommand = new RelayCommand(OpenRollModal);
        CancelRollCommand = new RelayCommand(CloseRollModal);

        ChooseSevenCommand = new RelayCommand(() => ChooseKind(RollKind.Seven));
        ChooseDoublesCommand = new RelayCommand(() => ChooseKind(RollKind.Doubles));
        ChooseValueCommand = new RelayCommand(() => ChooseKind(RollKind.Value));

        ConfirmRollCommand = new RelayCommand(ConfirmRoll);

        ContinueCommand = new RelayCommand(ContinueAfterBanking);
        AdvanceRollerCommand = new RelayCommand(AdvanceRoller);
        OpenBankConfirmCommand = new RelayCommand(OpenBankConfirm);
        ConfirmBankYesCommand = new RelayCommand(ConfirmBankYes);
        ConfirmBankNoCommand = new RelayCommand(() => { ShowBankConfirm = false; Raise(nameof(BankConfirmText)); });

        NextRoundCommand = new RelayCommand(NextRound);
        GoToStandingsCommand = new RelayCommand(async () => await Shell.Current.GoToAsync("//standings"));

        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        Players.Clear();

        if (!_session.HasActiveGame)
        {
            Status = "No active game. Go to Setup.";
            RaiseAll();
            return;
        }

        foreach (var p in _session.Players)
        {
            var row = new ScoringPlayerRow(p.Id, p.Name)
            {
                IsInRound = _session.IsPlayerInRound(p.Id),
                IsBanked = _session.BankedThisRound.Contains(p.Id),
                BankSelected = false
            };
            Players.Add(row);
        }

        // Set up property change handlers for banking button text updates
        foreach (var row in Players)
        {
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScoringPlayerRow.BankSelected))
                    Raise(nameof(ContinueButtonText));
            };
        }

        // Reset to rolling step when page loads
        _step = TurnStep.Rolling;
        IsBankingPhase = false;

        Status = "";
        RaiseAll();
    }

    private void RaiseAll()
    {
        Raise(nameof(RoundNumber));
        Raise(nameof(RollCount));
        Raise(nameof(BankPot));
        Raise(nameof(CurrentRollerName));
        Raise(nameof(BankConfirmText));
        Raise(nameof(CanEnterRoll));
        Raise(nameof(CanContinue));
        Raise(nameof(NextActionText));
        Raise(nameof(ContinueButtonText));
        Raise(nameof(ShowBankingUI));
        Raise(nameof(ShowNextRound));
    }

        private void OpenRollModal()
        {
            if (!CanEnterRoll) return;

            Status = "";
            ModalError = "";
            _pendingKind = RollKind.Value;
            _pendingSum = null;

            ShowSumEntry = false;
            SumPromptText = "";
            SumText = "";

            ShowRollModal = true;
        }

        private void CloseRollModal()
        {
            ShowRollModal = false;
            ShowSumEntry = false;
            SumText = "";
            Status = "";
            ModalError = "";
        }

        private void ChooseKind(RollKind kind)
        {
            _pendingKind = kind;
            ModalError = "";

            if (kind == RollKind.Seven)
            {
                // No sum entry needed
                _pendingSum = null;
                ShowSumEntry = false;

                // Immediately apply and close modal (clean UX)
                ShowRollModal = false;
                ApplyRoll(RollKind.Seven, null);

                // Move to Banking or RoundEnded handled inside ApplyRoll
                return;
            }

            ShowSumEntry = true;
            SumPromptText = kind == RollKind.Doubles
                ? "Enter doubles sum (2?12)"
                : "Enter roll sum (2?12)";
            SumText = "";
        }

        private void ConfirmRoll()
        {
            if (!ShowSumEntry) return;

            if (!int.TryParse(SumText?.Trim(), out var sum) || sum < 2 || sum > 12)
            {
                ModalError = "Enter a number from 2 to 12.";
                return;
            }

            // Validate doubles: must be even (2, 4, 6, 8, 10, 12)
            if (_pendingKind == RollKind.Doubles && sum % 2 != 0)
            {
                ModalError = $"Invalid doubles! {sum} is not possible with doubles. Valid: 2, 4, 6, 8, 10, 12.";
                return;
            }

            _pendingSum = sum;
            ShowRollModal = false;
            ModalError = "";

            ApplyRoll(_pendingKind, sum);
        }

        private void ContinueAfterBanking()
        {
            if (!_session.HasActiveGame) return;
            if (_step != TurnStep.Banking) return;

            var selected = Players.Where(p => p.IsInRound && p.BankSelected).ToList();

            if (selected.Count > 0)
            {
                // Confirmation: use existing bank confirmation logic
                ConfirmBank(selected.Select(s => s.PlayerId).ToList());
                return;
            }

            // Nobody banked -> next roller -> back to rolling
            _session.Engine.AdvanceToNextRoller(_session.OrderedPlayerIds);
            _step = TurnStep.Rolling;
            IsBankingPhase = false;
            Status = "Next roller. Enter the next roll.";

            RaiseAll();
            Raise(nameof(CanEnterRoll));
            Raise(nameof(CanContinue));
            Raise(nameof(NextActionText));
        }

        private void ConfirmBank(List<Guid> bankedIds)
        {
            var pot = _session.Engine.BankPot;
            _session.RecordBankForPlayers(bankedIds, _session.Engine.CurrentRoundNumber, pot);
            _session.Engine.BankPlayers(bankedIds);

            foreach (var row in Players)
            {
                row.IsBanked = _session.BankedThisRound.Contains(row.PlayerId);
                row.IsInRound = _session.IsPlayerInRound(row.PlayerId);
                row.BankSelected = false;
            }

            Raise(nameof(ContinueButtonText));

            if (_session.Engine.InRound.Count == 0)
            {
                _step = TurnStep.RoundEnded;
                IsBankingPhase = false;
                Status = "Everyone banked. Round ended.";
                
                CheckGameEndOrPrepareNextRoundUI();
                RaiseAll();
                return;
            }
            else
            {
                // stay in rolling after banking is resolved
                // If current roller banked, advance to next available
                var ordered = _session.OrderedPlayerIds;
                if (!_session.IsPlayerInRound(ordered[_session.Engine.CurrentRollerIndex]))
                {
                    _session.Engine.AdvanceToNextRoller(ordered);
                }
                _step = TurnStep.Rolling;
                IsBankingPhase = false;
                Status = "Bank confirmed. Enter the next roll.";
            }

            RaiseAll();
            Raise(nameof(CanEnterRoll));
            Raise(nameof(CanContinue));
            Raise(nameof(NextActionText));
        }

        private void OpenPrompt(RollKind kind, string title)
    {
        if (!_session.HasActiveGame) return;

        _pendingKind = kind;
        PromptTitle = title;
        PromptValue = "";
        ShowValuePrompt = true;
    }

    private void PromptOk()
    {
        if (!_session.HasActiveGame) return;

        if (!int.TryParse(PromptValue?.Trim(), out var sum) || sum < 2 || sum > 12)
        {
            Status = "Enter a number from 2 to 12.";
            return;
        }

        ShowValuePrompt = false;
        PromptValue = "";

        ApplyRoll(_pendingKind, sum);
    }

    private void ApplyRoll(RollKind kind, int? sum)
    {
        if (!_session.HasActiveGame) return;

        // clear banking selections
        foreach (var pr in Players) pr.BankSelected = false;
        Raise(nameof(ContinueButtonText));

        var ordered = _session.OrderedPlayerIds;
        bool sevenEnded;

        // In this input scheme, doubles/value both use sum; engine checks based on kind.
        _session.Engine.ApplyRoll(kind, valueSum: sum, doublesSum: sum, orderedPlayerIds: ordered, out sevenEnded);

        RaiseAll();

        if (sevenEnded)
        {
            // Press zone 7: anyone not banked gets 0, round ends immediately
            _session.RecordZeroForUnbankedOnSeven(_session.Engine.CurrentRoundNumber);
            _session.Engine.EndRoundForceClear();
            foreach (var pr in Players)
                pr.IsInRound = false;

            _step = TurnStep.RoundEnded;
            IsBankingPhase = false;
            Status = "Rolled a 7: unbanked players scored 0. Round ended.";

            CheckGameEndOrPrepareNextRoundUI();
            
            RaiseAll();
            return;
        }

        // If roll count < 3, keep rolling step (safety zone) and advance to next roller
        if (_session.Engine.RollCountThisRound <= BankRules.SafetyRolls)
        {
            _step = TurnStep.Rolling;
            IsBankingPhase = false;
            Status = "Safety zone: keep rolling.";

            // Advance to next roller after each safety zone roll
            _session.Engine.AdvanceToNextRoller(ordered);
        }
        else
        {
            // roll 3+ => banking step
            _step = TurnStep.Banking;
            IsBankingPhase = true;
            Status = "Select who banks, or continue to next roller.";
        }

        Raise(nameof(CanEnterRoll));
        Raise(nameof(CanContinue));
        Raise(nameof(NextActionText));

        // Update roller label after engine normalization
        RaiseAll();
    }

    private void OpenBankConfirm()
    {
        if (!IsBankingPhase) return;

        var any = Players.Any(p => p.BankSelected && p.IsInRound);
        if (!any)
        {
            Status = "Select at least one in-round player to bank.";
            return;
        }

        Raise(nameof(BankConfirmText));
        ShowBankConfirm = true;
    }

    private void ConfirmBankYes()
    {
        if (!_session.HasActiveGame) return;

        var bankedIds = Players
            .Where(p => p.IsInRound && p.BankSelected)
            .Select(p => p.PlayerId)
            .ToList();

        if (bankedIds.Count == 0)
        {
            ShowBankConfirm = false;
            return;
        }

        // Record points for those who banked
        var pot = _session.Engine.BankPot;
        _session.RecordBankForPlayers(bankedIds, _session.Engine.CurrentRoundNumber, pot);

        // Remove them from in-round in the engine
        _session.Engine.BankPlayers(bankedIds);

        // Update UI rows
        foreach (var row in Players)
        {
            row.IsBanked = _session.BankedThisRound.Contains(row.PlayerId);
            row.IsInRound = _session.IsPlayerInRound(row.PlayerId);
            row.BankSelected = false;
        }

        ShowBankConfirm = false;
        Raise(nameof(BankConfirmText));

        // If everyone banked, end round immediately
        if (_session.Engine.InRound.Count == 0)
        {
            _step = TurnStep.RoundEnded;
            IsBankingPhase = false;
            Status = "Everyone banked. Round ended.";
            
            CheckGameEndOrPrepareNextRoundUI();
            RaiseAll();
            return;
        }

        // If the current roller banked, advance to next available roller
        var ordered = _session.OrderedPlayerIds;
        _session.Engine.SetCurrentRollerIndex(_session.Engine.NormalizeRollerIndex(ordered));

        Status = "Bank confirmed. Advance or roll next.";
        RaiseAll();
    }

    private void AdvanceRoller()
    {
        if (!_session.HasActiveGame) return;

        // Clear selections (optional but keeps it clean)
        foreach (var row in Players) row.BankSelected = false;
        ShowBankConfirm = false;

        _session.Engine.AdvanceToNextRoller(_session.OrderedPlayerIds);
        Status = "Advanced to next roller.";
        RaiseAll();
    }

    private void CheckGameEndOrPrepareNextRoundUI()
    {
        if (_session.IsGameOverAndWinnerDetermined(out var winnerId))
        {
            Status = $"Game over. Winner: {_session.GetPlayerName(winnerId)}";
            return;
        }

        // Not over OR tie at top -> allow next round
        Status += " Tap Next Round.";
    }

    private void NextRound()
    {
        if (!_session.HasActiveGame) return;

        // Only allow next round if round actually ended (7 ended it OR all banked)
        var round = _session.Engine.CurrentRoundNumber;
        var allHaveThisRound = _session.Players.All(p => _session.ScoreByPlayer[p.Id].Any(rs => rs.RoundNumber == round));
        if (!allHaveThisRound)
        {
            Status = "Round is not finished yet.";
            return;
        }

        _session.StartNextRound();
        _step = TurnStep.Rolling; // Reset to rolling step for new round
        RefreshFromSession();

        Status = "New round started.";
    }
}
