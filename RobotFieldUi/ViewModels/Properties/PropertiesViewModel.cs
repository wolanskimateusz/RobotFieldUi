using CommunityToolkit.Mvvm.ComponentModel;

namespace RobotFieldUi.ViewModels;

public partial class PropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _selectedObject;
}
