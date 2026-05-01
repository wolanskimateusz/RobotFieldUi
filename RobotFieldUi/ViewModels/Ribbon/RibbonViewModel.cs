using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RobotFieldUi.ViewModels;

public enum ActiveTool { Select, AddPoint, Pan, Delete }

public partial class RibbonViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelect), nameof(IsAddPoint), nameof(IsPan), nameof(IsDelete))]
    private ActiveTool _activeTool = ActiveTool.Select;

    public bool IsSelect   => ActiveTool == ActiveTool.Select;
    public bool IsAddPoint => ActiveTool == ActiveTool.AddPoint;
    public bool IsPan      => ActiveTool == ActiveTool.Pan;
    public bool IsDelete   => ActiveTool == ActiveTool.Delete;

    [RelayCommand]
    private void SetTool(ActiveTool tool) => ActiveTool = tool;

    // Akcja — nie narzędzie canvas, otwiera dialog
    public event EventHandler? AddPathRequested;

    [RelayCommand]
    private void AddPath() => AddPathRequested?.Invoke(this, EventArgs.Empty);
}
