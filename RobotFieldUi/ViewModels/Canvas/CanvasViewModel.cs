using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.Services;

namespace RobotFieldUi.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    private readonly UndoRedoService _undoRedo;

    internal UndoRedoService UndoRedo => _undoRedo;

    [ObservableProperty] private double _cursorX;
    [ObservableProperty] private double _cursorY;

    [ObservableProperty] private double _offsetX;
    [ObservableProperty] private double _offsetY;
    [ObservableProperty] private double _scale = 1.0;

    [ObservableProperty] private ActiveTool  _activeTool   = ActiveTool.Select;
    [ObservableProperty] private SegmentType _drawSegment  = SegmentType.Line;

    [ObservableProperty] private MissionPath? _activePath;
    [ObservableProperty] private Mission?     _activeMission;
    [ObservableProperty] private PathPoint?   _selectedPoint;

    public ObservableCollection<PathPoint> SelectedPoints { get; } = new();

    public event EventHandler? SelectedPointsDeleted;

    public CanvasViewModel(UndoRedoService undoRedo) => _undoRedo = undoRedo;

    partial void OnActivePathChanged(MissionPath? oldValue, MissionPath? newValue)
    {
        SelectedPoints.Clear();
        SelectedPoint = null;
    }

    // ── dodawanie / usuwanie przez narzędzia ──────────────────────────────────

    public void HandleAddPoint(double lx, double ly)
    {
        if (ActivePath is not { } path) return;

        PrevPointChange? prevChange = null;
        if (path.Points.Count > 0)
        {
            var prev     = path.Points[^1];
            var oldSeg   = prev.SegmentOut;
            var oldR     = prev.ArcRadius;
            var oldDir   = prev.ArcDirection;

            prev.SegmentOut = DrawSegment;
            var newR   = oldR;
            var newDir = oldDir;
            if (DrawSegment == SegmentType.Arc)
            {
                var cx = lx - prev.X;
                var cy = ly - prev.Y;
                var chord = Math.Sqrt(cx * cx + cy * cy);
                newR   = Math.Max(chord * 1.5, 10);
                newDir = ArcDirection.CCW;
                prev.ArcRadius    = newR;
                prev.ArcDirection = newDir;
            }
            prevChange = new PrevPointChange(prev, oldSeg, oldR, oldDir, DrawSegment, newR, newDir);
        }

        var pt  = new PathPoint(Math.Round(lx, 2), Math.Round(ly, 2));
        var idx = path.Points.Count;
        path.Points.Add(pt);
        SelectedPoints.Clear();
        SelectedPoints.Add(pt);
        SelectedPoint = pt;

        _undoRedo.Record(new AddPointAction(path, pt, idx, prevChange));
    }

    public void HandleDeleteAt(double lx, double ly, double threshold)
    {
        if (ActivePath is not { } path) return;
        var pt = FindNearest(lx, ly, threshold);
        if (pt == null) return;

        var idx = path.Points.IndexOf(pt);
        if (SelectedPoint == pt) SelectedPoint = null;
        SelectedPoints.Remove(pt);
        path.Points.Remove(pt);

        _undoRedo.Record(new DeletePointsAction(path,
            new List<(int, PathPoint)> { (idx, pt) }));
    }

    public void RecordArcHandleChange(PathPoint pt, double oldRadius, ArcDirection oldDir)
    {
        if (pt.ArcRadius == oldRadius && pt.ArcDirection == oldDir) return;
        _undoRedo.Record(new ArcHandleChangedAction(pt, oldRadius, oldDir, pt.ArcRadius, pt.ArcDirection));
    }

    public void RecordTransform(List<PointSnapshot> before, List<PointSnapshot> after)
        => _undoRedo.Record(new TransformPointsAction(before, after));

    internal PathPoint? FindNearest(double lx, double ly, double radius)
    {
        if (ActivePath == null) return null;
        PathPoint? nearest = null;
        var minDist = radius;
        foreach (var pt in ActivePath.Points)
        {
            var dx = pt.X - lx;
            var dy = pt.Y - ly;
            var d  = Math.Sqrt(dx * dx + dy * dy);
            if (d < minDist) { minDist = d; nearest = pt; }
        }
        return nearest;
    }

    // ── zaznaczenie / usuwanie ────────────────────────────────────────────────

    [RelayCommand]
    private void SelectAllPoints()
    {
        if (ActivePath == null) return;
        SelectedPoints.Clear();
        foreach (var pt in ActivePath.Points)
            SelectedPoints.Add(pt);
        SelectedPoint = SelectedPoints.Count > 0 ? SelectedPoints[^1] : null;
    }

    [RelayCommand]
    private void DeleteSelectedPoints()
    {
        if (ActivePath == null || SelectedPoints.Count == 0) return;

        var path    = ActivePath;
        var indexed = SelectedPoints
            .Select(pt => (Index: path.Points.IndexOf(pt), Pt: pt))
            .Where(x => x.Index >= 0)
            .OrderBy(x => x.Index)
            .ToList();

        foreach (var (_, pt) in indexed)
            path.Points.Remove(pt);

        SelectedPoints.Clear();
        SelectedPoint = null;
        SelectedPointsDeleted?.Invoke(this, EventArgs.Empty);

        if (indexed.Count > 0)
            _undoRedo.Record(new DeletePointsAction(path, indexed));
    }
}
