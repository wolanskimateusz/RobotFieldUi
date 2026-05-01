using CommunityToolkit.Mvvm.ComponentModel;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    [ObservableProperty] private double _cursorX;
    [ObservableProperty] private double _cursorY;

    [ObservableProperty] private double _offsetX;
    [ObservableProperty] private double _offsetY;
    [ObservableProperty] private double _scale = 1.0;

    [ObservableProperty] private ActiveTool _activeTool = ActiveTool.Select;

    [ObservableProperty] private MissionPath? _activePath;
    [ObservableProperty] private Mission?     _activeMission;
    [ObservableProperty] private PathPoint?   _selectedPoint;
}
