using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.ViewModels;

public enum ActiveTool { Select, AddPoint, Pan, Delete, Move }

public partial class RibbonViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelect), nameof(IsAddPoint), nameof(IsPan), nameof(IsDelete), nameof(IsMove))]
    private ActiveTool _activeTool = ActiveTool.Select;

    public bool IsSelect   => ActiveTool == ActiveTool.Select;
    public bool IsAddPoint => ActiveTool == ActiveTool.AddPoint;
    public bool IsPan      => ActiveTool == ActiveTool.Pan;
    public bool IsDelete   => ActiveTool == ActiveTool.Delete;
    public bool IsMove     => ActiveTool == ActiveTool.Move;

    [RelayCommand]
    private void SetTool(ActiveTool tool) => ActiveTool = tool;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLine), nameof(IsArc))]
    private SegmentType _drawSegment = SegmentType.Line;

    public bool IsLine => DrawSegment == SegmentType.Line;
    public bool IsArc  => DrawSegment == SegmentType.Arc;

    [RelayCommand]
    private void SetSegment(SegmentType seg) => DrawSegment = seg;

    // Akcja — nie narzędzie canvas, otwiera dialog
    public event EventHandler? AddPathRequested;

    [RelayCommand]
    private void AddPath() => AddPathRequested?.Invoke(this, EventArgs.Empty);

    // Operacje geometryczne — otwierają dialogi
    public event EventHandler? CircleRequested;
    public event EventHandler? RotateRequested;
    public event EventHandler? ScaleTransformRequested;
    public event EventHandler? MirrorRequested;

    [RelayCommand] private void Circle()         => CircleRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Rotate()         => RotateRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void ScaleTransform() => ScaleTransformRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void Mirror()         => MirrorRequested?.Invoke(this, EventArgs.Empty);
}
