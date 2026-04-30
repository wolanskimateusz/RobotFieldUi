using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels;

public partial class MenuBarViewModel : ViewModelBase
{
    // [RelayCommand] generuje NewFileCommand : ICommand gotowe do bindowania w AXAML.
    [RelayCommand]
    private void NewFile() { }

    [RelayCommand]
    private void OpenFile() { }

    [RelayCommand]
    private void SaveFile() { }

    [RelayCommand]
    private void Undo() { }

    [RelayCommand]
    private void Redo() { }
}
