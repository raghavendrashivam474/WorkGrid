using System.Windows.Input;
using WorkGrid.App.ViewModels.Base;

namespace WorkGrid.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private string _title = "WorkGrid";
    private string _statusMessage = "Phase 0 — Architectural Baseline";
    private int _interactionCount;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int InteractionCount
    {
        get => _interactionCount;
        private set => SetProperty(ref _interactionCount, value);
    }

    public ICommand PrimaryActionCommand { get; }

    public MainViewModel()
    {
        PrimaryActionCommand = new Command(ExecutePrimaryAction);
    }

    private void ExecutePrimaryAction()
    {
        InteractionCount++;
        StatusMessage = $"Interactions: {InteractionCount}";
    }
}
