using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using RobotFieldUi.ViewModels;

namespace RobotFieldUi.Views.Controls;

public partial class CanvasControl : UserControl
{
    public CanvasControl()
    {
        InitializeComponent();
    }
    
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not CanvasViewModel vm) return;

        var pos = e.GetPosition(this);
        vm.CursorX = Math.Round(pos.X, 2);
        vm.CursorY = Math.Round(pos.Y, 2);
    }
}