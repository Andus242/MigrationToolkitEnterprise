using CommunityToolkit.Mvvm.ComponentModel;

namespace MTE.App.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private int progressPercentage;

    [ObservableProperty]
    private string currentOperation = string.Empty;
}