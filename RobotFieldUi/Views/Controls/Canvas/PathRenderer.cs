using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.Views.Controls.Canvas;

public class PathRenderer : Control
{
    public static readonly StyledProperty<MissionPath?> ActivePathProperty =
        AvaloniaProperty.Register<PathRenderer, MissionPath?>(nameof(ActivePath));

    public static readonly StyledProperty<PathPoint?> SelectedPointProperty =
        AvaloniaProperty.Register<PathRenderer, PathPoint?>(nameof(SelectedPoint));

    public MissionPath? ActivePath
    {
        get => GetValue(ActivePathProperty);
        set => SetValue(ActivePathProperty, value);
    }

    public PathPoint? SelectedPoint
    {
        get => GetValue(SelectedPointProperty);
        set => SetValue(SelectedPointProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActivePathProperty)
        {
            if (change.OldValue is MissionPath old)
                old.Points.CollectionChanged -= OnPointsChanged;
            if (change.NewValue is MissionPath @new)
                @new.Points.CollectionChanged += OnPointsChanged;
        }

        if (change.Property == ActivePathProperty || change.Property == SelectedPointProperty)
            InvalidateVisual();
    }

    private void OnPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    private readonly Pen _linePen = new(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.5);
    private readonly Pen _selectedPen = new(new SolidColorBrush(Color.FromRgb(251, 191, 36)), 2.0);
    private readonly IBrush _pointBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
    private readonly IBrush _firstPointBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (ActivePath == null || ActivePath.Points.Count == 0) return;

        var pts = ActivePath.Points;

        for (int i = 0; i < pts.Count - 1; i++)
            context.DrawLine(_linePen,
                new Point(pts[i].X, pts[i].Y),
                new Point(pts[i + 1].X, pts[i + 1].Y));

        for (int i = 0; i < pts.Count; i++)
        {
            var brush = i == 0 ? _firstPointBrush : _pointBrush;
            context.DrawEllipse(brush, null, new Point(pts[i].X, pts[i].Y), 5, 5);
        }

        if (SelectedPoint != null && pts.Contains(SelectedPoint))
            context.DrawEllipse(null, _selectedPen,
                new Point(SelectedPoint.X, SelectedPoint.Y), 9, 9);
    }
}
