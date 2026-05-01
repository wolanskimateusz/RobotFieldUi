using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels;

public enum ActiveTool { Select, AddPoint, AddPath, Pan, Delete }

public partial class RibbonViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsSelect), nameof(IsAddPoint), nameof(IsAddPath),
        nameof(IsPan), nameof(IsDelete))]
    private ActiveTool _activeTool = ActiveTool.Select;

    public bool IsSelect   => ActiveTool == ActiveTool.Select;
    public bool IsAddPoint => ActiveTool == ActiveTool.AddPoint;
    public bool IsAddPath  => ActiveTool == ActiveTool.AddPath;
    public bool IsPan      => ActiveTool == ActiveTool.Pan;
    public bool IsDelete   => ActiveTool == ActiveTool.Delete;

    [RelayCommand]
    private void SetTool(ActiveTool tool) => ActiveTool = tool;
}
