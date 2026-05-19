using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    public static readonly StyledProperty<ObservableCollection<PathPoint>?> SelectedPointsProperty =
        AvaloniaProperty.Register<PathRenderer, ObservableCollection<PathPoint>?>(nameof(SelectedPoints));

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

    public ObservableCollection<PathPoint>? SelectedPoints
    {
        get => GetValue(SelectedPointsProperty);
        set => SetValue(SelectedPointsProperty, value);
    }

    // ── Subskrypcja zmian ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActivePathProperty)
        {
            if (change.OldValue is MissionPath old)
            {
                old.Points.CollectionChanged -= OnPointsChanged;
                foreach (var pt in old.Points)
                    pt.PropertyChanged -= OnPointPropChanged;
            }
            if (change.NewValue is MissionPath @new)
            {
                @new.Points.CollectionChanged += OnPointsChanged;
                foreach (var pt in @new.Points)
                    pt.PropertyChanged += OnPointPropChanged;
            }
        }

        if (change.Property == SelectedPointsProperty)
        {
            if (change.OldValue is ObservableCollection<PathPoint> oldSel)
                oldSel.CollectionChanged -= OnSelectionChanged;
            if (change.NewValue is ObservableCollection<PathPoint> newSel)
                newSel.CollectionChanged += OnSelectionChanged;
        }

        if (change.Property == ActivePathProperty ||
            change.Property == SelectedPointProperty ||
            change.Property == SelectedPointsProperty)
            InvalidateVisual();
    }

    private void OnPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (PathPoint pt in e.NewItems)
                pt.PropertyChanged += OnPointPropChanged;
        if (e.OldItems != null)
            foreach (PathPoint pt in e.OldItems)
                pt.PropertyChanged -= OnPointPropChanged;
        InvalidateVisual();
    }

    private void OnPointPropChanged(object? sender, PropertyChangedEventArgs e)
        => InvalidateVisual();

    private void OnSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    // ── Pędzle ────────────────────────────────────────────────────────────────

    private readonly Pen    _linePen         = new(new SolidColorBrush(Color.FromRgb(37,  99,  235)), 1.5);
    private readonly Pen    _selectedPen     = new(new SolidColorBrush(Color.FromRgb(251, 191, 36)),  2.0);
    private readonly Pen    _arcHandlePen    = new(new SolidColorBrush(Color.FromRgb(249, 115, 22)),  2.0);
    private readonly Pen    _centerPen       = new(new SolidColorBrush(Color.FromRgb(20,  184, 166)),  1.5);
    private readonly IBrush _pointBrush      = new SolidColorBrush(Color.FromRgb(37,  99,  235));
    private readonly IBrush _firstPointBrush = new SolidColorBrush(Color.FromRgb(34,  197, 94));
    private readonly IBrush _arcHandleBrush  = new SolidColorBrush(Color.FromArgb(120, 249, 115, 22));

    // ── Render ────────────────────────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (ActivePath == null || ActivePath.Points.Count == 0) return;

        var pts = ActivePath.Points;

        // Segmenty (pomijamy segmenty do/z punktu środkowego okręgu)
        for (int i = 0; i < pts.Count - 1; i++)
        {
            if (pts[i].IsCenter || pts[i + 1].IsCenter) continue;

            var p1 = new Point(pts[i].X,     pts[i].Y);
            var p2 = new Point(pts[i+1].X,   pts[i+1].Y);

            if (pts[i].SegmentOut == SegmentType.Arc &&
                ArcMath.Center(p1, p2, pts[i].ArcRadius, pts[i].ArcDirection) != null)
            {
                var sg = new StreamGeometry();
                using var sgc = sg.Open();
                sgc.BeginFigure(p1, false);
                sgc.ArcTo(p2,
                          new Size(pts[i].ArcRadius, pts[i].ArcRadius),
                          rotationAngle: 0,
                          isLargeArc: false,
                          sweepDirection: ArcMath.ToSweep(pts[i].ArcDirection));
                sgc.EndFigure(false);
                context.DrawGeometry(null, _linePen, sg);
            }
            else
            {
                context.DrawLine(_linePen, p1, p2);
            }
        }

        // Punkty ścieżki
        for (int i = 0; i < pts.Count; i++)
        {
            if (pts[i].IsCenter)
            {
                // Środek okręgu: krzyżyk (crosshair) w kolorze teal
                const double arm = 7;
                var cx = pts[i].X;
                var cy = pts[i].Y;
                context.DrawLine(_centerPen, new Point(cx - arm, cy), new Point(cx + arm, cy));
                context.DrawLine(_centerPen, new Point(cx, cy - arm), new Point(cx, cy + arm));
            }
            else
            {
                var brush = i == 0 ? _firstPointBrush : _pointBrush;
                context.DrawEllipse(brush, null, new Point(pts[i].X, pts[i].Y), 5, 5);
            }
        }

        // Uchwyty łuków (pomijamy gdy punkt lub następny to środek okręgu)
        for (int i = 0; i < pts.Count - 1; i++)
        {
            if (pts[i].SegmentOut != SegmentType.Arc) continue;
            if (pts[i].IsCenter || pts[i + 1].IsCenter) continue;
            var p1 = new Point(pts[i].X,   pts[i].Y);
            var p2 = new Point(pts[i+1].X, pts[i+1].Y);
            var h  = ArcMath.Handle(p1, p2, pts[i].ArcRadius, pts[i].ArcDirection);
            context.DrawEllipse(_arcHandleBrush, _arcHandlePen, h, 7, 7);
        }

        // Żółte kółka zaznaczenia
        if (SelectedPoints != null)
        {
            foreach (var pt in SelectedPoints)
                if (pts.Contains(pt))
                    context.DrawEllipse(null, _selectedPen, new Point(pt.X, pt.Y), 9, 9);
        }
        else if (SelectedPoint != null && pts.Contains(SelectedPoint))
        {
            context.DrawEllipse(null, _selectedPen,
                new Point(SelectedPoint.X, SelectedPoint.Y), 9, 9);
        }
    }
}
