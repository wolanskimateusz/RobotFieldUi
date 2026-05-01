using CommunityToolkit.Mvvm.ComponentModel;

namespace RobotFieldUi.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _cursorX;

    [ObservableProperty]
    private double _cursorY;

    [ObservableProperty]
    private double _scale = 1.0;
    
    [ObservableProperty]
    private ActiveTool _activeTool = ActiveTool.Select;
}