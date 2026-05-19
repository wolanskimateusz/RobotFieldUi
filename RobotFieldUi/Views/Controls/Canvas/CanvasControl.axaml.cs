using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaCanvas = Avalonia.Controls.Canvas;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.Services;
using RobotFieldUi.ViewModels;
using RobotFieldUi.Views.Controls.Canvas;

namespace RobotFieldUi.Views.Controls;

public partial class CanvasControl : UserControl
{
    // ── pan ───────────────────────────────────────────────────────────────────
    private bool   _isPanning;
    private double _panStartX, _panStartY;
    private double _offsetXAtPanStart, _offsetYAtPanStart;

    // ── arc drag ──────────────────────────────────────────────────────────────
    private bool         _isArcDragging;
    private PathPoint?   _arcDragStart;
    private PathPoint?   _arcDragEnd;
    private double       _arcDragOldRadius;
    private ArcDirection _arcDragOldDir;

    // ── rubber-band selection ─────────────────────────────────────────────────
    private bool   _isRubberBanding;
    private double _rubberStartX, _rubberStartY;

    // ── drag-move ─────────────────────────────────────────────────────────────
    private bool   _dragMovePossible;
    private bool   _isDragMoving;
    private double _dragMoveStartSX, _dragMoveStartSY; // screen coords
    private double _dragMoveStartLX, _dragMoveStartLY; // logical coords
    private List<PointSnapshot> _dragMoveSnapshot = new();

    // ── rubber-band overlay ───────────────────────────────────────────────────
    private readonly Rectangle _rubberRect;

    public CanvasControl()
    {
        InitializeComponent();

        _rubberRect = new Rectangle
        {
            Fill             = new SolidColorBrush(Color.FromArgb(40,  0, 120, 215)),
            Stroke           = new SolidColorBrush(Color.FromArgb(180, 0, 120, 215)),
            StrokeThickness  = 1,
            IsVisible        = false,
            IsHitTestVisible = false
        };
        DrawingCanvas.Children.Add(_rubberRect);
    }

    private CanvasViewModel? Vm => DataContext as CanvasViewModel;

    private (double x, double y) ToLogical(double sx, double sy)
        => ((sx - Vm!.OffsetX) / Vm!.Scale, (sy - Vm!.OffsetY) / Vm!.Scale);

    private void ShowRubberRect(double x1, double y1, double x2, double y2)
    {
        var left = Math.Min(x1, x2);
        var top  = Math.Min(y1, y2);
        AvaloniaCanvas.SetLeft(_rubberRect, left);
        AvaloniaCanvas.SetTop(_rubberRect, top);
        _rubberRect.Width    = Math.Max(Math.Abs(x2 - x1), 0);
        _rubberRect.Height   = Math.Max(Math.Abs(y2 - y1), 0);
        _rubberRect.IsVisible = true;
    }

    // ── ruch myszy ────────────────────────────────────────────────────────────

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

        if (_isArcDragging && _arcDragStart != null && _arcDragEnd != null)
        {
            var (r, dir, valid) = ArcMath.FromVia(
                new Point(_arcDragStart.X, _arcDragStart.Y),
                new Point(lx, ly),
                new Point(_arcDragEnd.X,   _arcDragEnd.Y));
            if (valid)
            {
                _arcDragStart.ArcRadius    = r;
                _arcDragStart.ArcDirection = dir;
            }
            vm.CursorX = Math.Round(lx, 2);
            vm.CursorY = Math.Round(ly, 2);
            return;
        }

        // Sprawdź czy zaczął się drag-move (próg 4 px ekranowych)
        if (_dragMovePossible && !_isDragMoving)
        {
            var dx = pos.X - _dragMoveStartSX;
            var dy = pos.Y - _dragMoveStartSY;
            if (dx * dx + dy * dy > 16.0)
            {
                _isDragMoving     = true;
                _dragMovePossible = false;

                // Gdy zaznaczono środek okręgu, przesuwamy cały okrąg (wszystkie punkty ścieżki)
                if (vm.SelectedPoints.Any(p => p.IsCenter) && vm.ActivePath != null)
                    _dragMoveSnapshot = vm.ActivePath.Points
                        .Select(p => new PointSnapshot(p, p.X, p.Y, p.ArcRadius, p.ArcDirection))
                        .ToList();
                else
                    _dragMoveSnapshot = vm.SelectedPoints
                        .Select(p => new PointSnapshot(p, p.X, p.Y, p.ArcRadius, p.ArcDirection))
                        .ToList();
            }
        }

        if (_isDragMoving)
        {
            var dlx = lx - _dragMoveStartLX;
            var dly = ly - _dragMoveStartLY;
            foreach (var s in _dragMoveSnapshot)
            {
                s.Pt.X = Math.Round(s.X + dlx, 2);
                s.Pt.Y = Math.Round(s.Y + dly, 2);
            }
            vm.CursorX = Math.Round(lx, 2);
            vm.CursorY = Math.Round(ly, 2);
            return;
        }

        if (_isRubberBanding)
            ShowRubberRect(_rubberStartX, _rubberStartY, pos.X, pos.Y);

        vm.CursorX = Math.Round(lx, 2);
        vm.CursorY = Math.Round(ly, 2);
    }

    // ── kliknięcie ────────────────────────────────────────────────────────────

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is not { } vm) return;
        var pos   = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;
        var (lx, ly) = ToLogical(pos.X, pos.Y);

        if (sender is IInputElement el) el.Focus();

        // Prawy przycisk → pan zawsze (niezależnie od aktywnego narzędzia)
        if (props.IsRightButtonPressed)
        {
            _isPanning         = true;
            _panStartX         = pos.X;
            _panStartY         = pos.Y;
            _offsetXAtPanStart = vm.OffsetX;
            _offsetYAtPanStart = vm.OffsetY;
            if (sender is IInputElement panCap) e.Pointer.Capture(panCap);
            return;
        }

        if (!props.IsLeftButtonPressed) return;

        switch (vm.ActiveTool)
        {
            case ActiveTool.Select:
            {
                var threshold = 10.0 / vm.Scale;
                var pt        = vm.FindNearest(lx, ly, threshold);
                var ctrl      = e.KeyModifiers.HasFlag(KeyModifiers.Control);

                // Uchwyt łuku (sprawdź gdy nie ma punktu w pobliżu)
                if (pt == null)
                {
                    var (arcPt, arcNext) = FindNearestArcHandle(vm, lx, ly, 12.0 / vm.Scale);
                    if (arcPt != null)
                    {
                        _isArcDragging    = true;
                        _arcDragStart     = arcPt;
                        _arcDragEnd       = arcNext;
                        _arcDragOldRadius = arcPt.ArcRadius;
                        _arcDragOldDir    = arcPt.ArcDirection;
                        if (sender is IInputElement arcCap) e.Pointer.Capture(arcCap);
                        break;
                    }
                }

                if (pt != null)
                {
                    if (!ctrl)
                    {
                        // Klik bez Ctrl: zaznacz tylko ten punkt
                        if (!vm.SelectedPoints.Contains(pt))
                        {
                            vm.SelectedPoints.Clear();
                            vm.SelectedPoints.Add(pt);
                        }
                    }
                    else
                    {
                        // Ctrl+klik: przełącz zaznaczenie
                        if (vm.SelectedPoints.Contains(pt))
                            vm.SelectedPoints.Remove(pt);
                        else
                            vm.SelectedPoints.Add(pt);
                    }
                    vm.SelectedPoint = vm.SelectedPoints.Count > 0
                        ? vm.SelectedPoints[^1] : null;
                }
                else
                {
                    // Puste miejsce → rubber-band
                    if (!ctrl)
                    {
                        vm.SelectedPoints.Clear();
                        vm.SelectedPoint = null;
                    }
                    _isRubberBanding = true;
                    _rubberStartX    = pos.X;
                    _rubberStartY    = pos.Y;
                    ShowRubberRect(pos.X, pos.Y, pos.X, pos.Y);
                    if (sender is IInputElement rbCap) e.Pointer.Capture(rbCap);
                }
                break;
            }

            case ActiveTool.AddPoint:
                vm.HandleAddPoint(lx, ly);
                break;

            case ActiveTool.Delete:
                vm.HandleDeleteAt(lx, ly, 10.0 / vm.Scale);
                break;

            case ActiveTool.Move:
            {
                var threshold = 10.0 / vm.Scale;
                var pt        = vm.FindNearest(lx, ly, threshold);
                if (pt == null) break;

                // Jeśli klikniętego punktu nie ma w zaznaczeniu, zaznacz tylko go
                if (!vm.SelectedPoints.Contains(pt))
                {
                    vm.SelectedPoints.Clear();
                    vm.SelectedPoints.Add(pt);
                    vm.SelectedPoint = pt;
                }

                _dragMovePossible = true;
                _dragMoveStartSX  = pos.X;
                _dragMoveStartSY  = pos.Y;
                _dragMoveStartLX  = lx;
                _dragMoveStartLY  = ly;
                if (sender is IInputElement moveCap) e.Pointer.Capture(moveCap);
                break;
            }

            case ActiveTool.Pan:
                _isPanning         = true;
                _panStartX         = pos.X;
                _panStartY         = pos.Y;
                _offsetXAtPanStart = vm.OffsetX;
                _offsetYAtPanStart = vm.OffsetY;
                if (sender is IInputElement panLeftCap) e.Pointer.Capture(panLeftCap);
                break;
        }
    }

    // ── zwolnienie myszy ──────────────────────────────────────────────────────

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Vm is not { } vm) return;

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            return;
        }

        if (_isArcDragging)
        {
            if (_arcDragStart != null)
                vm.RecordArcHandleChange(_arcDragStart, _arcDragOldRadius, _arcDragOldDir);
            _isArcDragging = false;
            _arcDragStart  = null;
            _arcDragEnd    = null;
            e.Pointer.Capture(null);
            return;
        }

        if (_isDragMoving)
        {
            // Zapisz undo: stan przed i po
            var before = _dragMoveSnapshot
                .Select(s => new PointSnapshot(s.Pt, s.X,      s.Y,      s.ArcRadius, s.ArcDir))
                .ToList();
            var after  = _dragMoveSnapshot
                .Select(s => new PointSnapshot(s.Pt, s.Pt.X,   s.Pt.Y,   s.Pt.ArcRadius, s.Pt.ArcDirection))
                .ToList();
            if (before.Count > 0)
                vm.RecordTransform(before, after);
            _isDragMoving = false;
            _dragMoveSnapshot.Clear();
            e.Pointer.Capture(null);
            return;
        }

        if (_dragMovePossible)
        {
            // Był tylko klik (bez drag) — zaznaczenie zostało ustawione w OnPointerPressed
            _dragMovePossible = false;
            e.Pointer.Capture(null);
            return;
        }

        if (_isRubberBanding)
        {
            _rubberRect.IsVisible = false;
            _isRubberBanding      = false;

            if (vm.ActivePath != null)
            {
                var pos          = e.GetPosition(this);
                var (lx1, ly1)   = ToLogical(_rubberStartX, _rubberStartY);
                var (lx2, ly2)   = ToLogical(pos.X, pos.Y);
                var minX         = Math.Min(lx1, lx2);
                var maxX         = Math.Max(lx1, lx2);
                var minY         = Math.Min(ly1, ly2);
                var maxY         = Math.Max(ly1, ly2);
                var ctrl         = e.KeyModifiers.HasFlag(KeyModifiers.Control);

                if (!ctrl) vm.SelectedPoints.Clear();
                foreach (var pt in vm.ActivePath.Points)
                    if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                        if (!vm.SelectedPoints.Contains(pt))
                            vm.SelectedPoints.Add(pt);

                vm.SelectedPoint = vm.SelectedPoints.Count > 0 ? vm.SelectedPoints[^1] : null;
            }
            e.Pointer.Capture(null);
            return;
        }
    }

    // ── zoom kółkiem myszy ────────────────────────────────────────────────────

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (Vm is not { } vm) return;
        var pos      = e.GetPosition(this);
        var factor   = e.Delta.Y > 0 ? 1.15 : (1.0 / 1.15);
        var newScale = Math.Clamp(vm.Scale * factor, 0.05, 50.0);
        var ratio    = newScale / vm.Scale;
        vm.OffsetX   = pos.X - (pos.X - vm.OffsetX) * ratio;
        vm.OffsetY   = pos.Y - (pos.Y - vm.OffsetY) * ratio;
        vm.Scale     = newScale;
        e.Handled    = true;
    }

    // ── klawiatura ────────────────────────────────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is not { } vm) return;

        if (e.Key == Key.Delete && vm.SelectedPoints.Count > 0)
        {
            vm.DeleteSelectedPointsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.A && e.KeyModifiers.HasFlag(KeyModifiers.Control) && vm.ActivePath != null)
        {
            vm.SelectedPoints.Clear();
            foreach (var pt in vm.ActivePath.Points)
                vm.SelectedPoints.Add(pt);
            vm.SelectedPoint = vm.SelectedPoints.Count > 0
                ? vm.SelectedPoints[^1] : null;
            e.Handled = true;
        }
    }

    // ── hit-testing ───────────────────────────────────────────────────────────

    private static (PathPoint? arcPt, PathPoint? arcNext) FindNearestArcHandle(
        CanvasViewModel vm, double lx, double ly, double thresh)
    {
        if (vm.ActivePath == null) return (null, null);
        var pts     = vm.ActivePath.Points;
        PathPoint?  bestPt   = null;
        PathPoint?  bestNext = null;
        double      minDist  = thresh;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            if (pts[i].SegmentOut != SegmentType.Arc) continue;
            var h  = ArcMath.Handle(
                new Point(pts[i].X,     pts[i].Y),
                new Point(pts[i+1].X,   pts[i+1].Y),
                pts[i].ArcRadius, pts[i].ArcDirection);
            var dx = h.X - lx;
            var dy = h.Y - ly;
            var d  = Math.Sqrt(dx * dx + dy * dy);
            if (d < minDist) { minDist = d; bestPt = pts[i]; bestNext = pts[i+1]; }
        }
        return (bestPt, bestNext);
    }
}
