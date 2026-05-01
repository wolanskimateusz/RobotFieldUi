using System;
using Avalonia.Controls;
using Avalonia.Input;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.ViewModels;

namespace RobotFieldUi.Views.Controls;

public partial class CanvasControl : UserControl
{
    private bool   _isPanning;
    private double _panStartX, _panStartY;
    private double _offsetXAtPanStart, _offsetYAtPanStart;

    public CanvasControl() => InitializeComponent();

    private CanvasViewModel? Vm => DataContext as CanvasViewModel;

    // Przelicza współrzędne ekranowe na logiczne (z uwzględnieniem offsetu panu)
    private (double x, double y) ToLogical(double sx, double sy)
        => (sx - Vm!.OffsetX, sy - Vm!.OffsetY);

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Vm is not { } vm) return;
        var pos = e.GetPosition(this);

        if (_isPanning)
        {
            vm.OffsetX = _offsetXAtPanStart + (pos.X - _panStartX);
            vm.OffsetY = _offsetYAtPanStart + (pos.Y - _panStartY);
            return;
        }

        var (lx, ly) = ToLogical(pos.X, pos.Y);
        vm.CursorX = Math.Round(lx, 2);
        vm.CursorY = Math.Round(ly, 2);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is not { } vm) return;
        var pos = e.GetPosition(this);
        var (lx, ly) = ToLogical(pos.X, pos.Y);

        switch (vm.ActiveTool)
        {
            case ActiveTool.Select:
                vm.SelectedPoint = FindNearest(vm, lx, ly, 10.0);
                break;

            case ActiveTool.AddPoint:
                if (vm.ActivePath is { } path)
                {
                    var pt = new PathPoint(Math.Round(lx, 2), Math.Round(ly, 2));
                    path.Points.Add(pt);
                    vm.SelectedPoint = pt;
                }
                break;

            case ActiveTool.Delete:
                if (vm.ActivePath is { } delPath)
                {
                    var pt = FindNearest(vm, lx, ly, 10.0);
                    if (pt != null)
                    {
                        if (vm.SelectedPoint == pt) vm.SelectedPoint = null;
                        delPath.Points.Remove(pt);
                    }
                }
                break;

            case ActiveTool.Pan:
                _isPanning = true;
                _panStartX = pos.X;
                _panStartY = pos.Y;
                _offsetXAtPanStart = vm.OffsetX;
                _offsetYAtPanStart = vm.OffsetY;
                if (sender is IInputElement el) e.Pointer.Capture(el);
                break;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanning) return;
        _isPanning = false;
        e.Pointer.Capture(null);
    }

    private static PathPoint? FindNearest(CanvasViewModel vm, double lx, double ly, double radius)
    {
        if (vm.ActivePath == null) return null;

        PathPoint? nearest = null;
        double minDist = radius;

        foreach (var pt in vm.ActivePath.Points)
        {
            var dx = pt.X - lx;
            var dy = pt.Y - ly;
            var d = Math.Sqrt(dx * dx + dy * dy);
            if (d < minDist) { minDist = d; nearest = pt; }
        }

        return nearest;
    }
}
