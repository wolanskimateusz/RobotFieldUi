using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using RobotFieldUi.Models;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.Services;
using RobotFieldUi.Views.Dialogs;

namespace RobotFieldUi.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly UndoRedoService _undoRedo = new();

        public MenuBarViewModel     MenuBar    { get; } = new();
        public RibbonViewModel      Ribbon     { get; } = new();
        public StatusBarViewModel   StatusBar  { get; } = new();
        public TreePanelViewModel   TreePanel  { get; }
        public CanvasViewModel      Canvas     { get; }
        public PropertiesViewModel  Properties { get; }

        public Func<IEnumerable<Mission>, string, Task<(string Name, Mission Mission)?>>? ShowAddPathDialog { get; set; }
        public Func<Window?>? GetMainWindow { get; set; }

        private MissionPath? _activePath;
        private bool _syncingToTree;
        private bool _syncingToCanvas;
        private string? _currentFilePath;

        // ── Aktywna ścieżka ───────────────────────────────────────────────────

        private void SetActivePath(MissionPath? path)
        {
            if (ReferenceEquals(_activePath, path)) return;
            if (_activePath != null) _activePath.IsActive = false;
            _activePath = path;
            if (_activePath != null) _activePath.IsActive = true;
        }

        // ── Konstruktor ───────────────────────────────────────────────────────

        public MainWindowViewModel()
        {
            TreePanel  = new TreePanelViewModel(_undoRedo);
            Canvas     = new CanvasViewModel(_undoRedo);
            Properties = new PropertiesViewModel(_undoRedo);

            WireCanvas();
            WireRibbon();
            WireTreePanel();
            WireMenuBar();
            WireProperties();
            HandleNewFile();
        }

        // ── Canvas ────────────────────────────────────────────────────────────

        private void WireCanvas()
        {
            Canvas.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(CanvasViewModel.CursorX):
                        StatusBar.CursorX = Canvas.CursorX; break;
                    case nameof(CanvasViewModel.CursorY):
                        StatusBar.CursorY = Canvas.CursorY; break;
                    case nameof(CanvasViewModel.SelectedPoint):
                        if (_syncingToCanvas) break;
                        if (Canvas.SelectedPoint != null)
                        {
                            _syncingToTree = true;
                            TreePanel.SelectedItem = Canvas.SelectedPoint;
                            _syncingToTree = false;
                        }
                        break;
                    case nameof(CanvasViewModel.Scale):
                        StatusBar.Scale = Canvas.Scale; break;
                    case nameof(CanvasViewModel.ActivePath):
                        SetActivePath(Canvas.ActivePath);
                        if (_syncingToCanvas) break;
                        if (Canvas.ActivePath != null &&
                            !ReferenceEquals(TreePanel.SelectedItem, Canvas.ActivePath))
                        {
                            _syncingToTree = true;
                            TreePanel.SelectedItem = Canvas.ActivePath;
                            _syncingToTree = false;
                        }
                        break;
                }
            };

            Canvas.SelectedPointsDeleted += (_, _) =>
            {
                if (TreePanel.SelectedItem is PathPoint)
                {
                    TreePanel.SelectedItem = Canvas.ActivePath;
                    Properties.SelectedObject = TreePanel.SelectedItem;
                }
            };
        }

        // ── Ribbon ────────────────────────────────────────────────────────────

        private void WireRibbon()
        {
            Ribbon.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(RibbonViewModel.ActiveTool):
                        Canvas.ActiveTool = Ribbon.ActiveTool; break;
                    case nameof(RibbonViewModel.DrawSegment):
                        Canvas.DrawSegment = Ribbon.DrawSegment; break;
                }
            };

            Ribbon.AddPathRequested        += async (_, _) => await HandleAddPath();
            Ribbon.CircleRequested         += async (_, _) => await HandleCircle();
            Ribbon.RotateRequested         += async (_, _) => await HandleRotate();
            Ribbon.ScaleTransformRequested += async (_, _) => await HandleScale();
            Ribbon.MirrorRequested         += async (_, _) => await HandleMirror();
        }

        // ── TreePanel ─────────────────────────────────────────────────────────

        private void WireTreePanel()
        {
            TreePanel.ItemDeleted += (_, item) =>
            {
                switch (item)
                {
                    case MissionPath path when ReferenceEquals(Canvas.ActivePath, path):
                        Canvas.ActivePath = null; break;
                    case PathPoint point when ReferenceEquals(Canvas.SelectedPoint, point):
                        Canvas.SelectedPoint = null; break;
                    case Mission mission when ReferenceEquals(Canvas.ActiveMission, mission):
                        Canvas.ActiveMission = null;
                        Canvas.ActivePath = null; break;
                    case Project:
                        Canvas.ActiveMission = null;
                        Canvas.ActivePath    = null;
                        Canvas.SelectedPoint = null; break;
                }
            };

            TreePanel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(TreePanelViewModel.SelectedItem)) return;

                Properties.SelectedObject = TreePanel.SelectedItem;
                if (_syncingToTree) return;

                _syncingToCanvas = true;
                switch (TreePanel.SelectedItem)
                {
                    case MissionPath path:
                        Canvas.ActivePath    = path;
                        Canvas.ActiveMission = FindParentMission(path); break;
                    case Mission mission:
                        Canvas.ActiveMission = mission; break;
                    case PathPoint point:
                        var parent = FindParentPath(point);
                        if (parent != null) Canvas.ActivePath = parent;
                        Canvas.ActiveMission = parent != null ? FindParentMission(parent) : null;
                        Canvas.SelectedPoints.Clear();
                        Canvas.SelectedPoints.Add(point);
                        Canvas.SelectedPoint = point; break;
                }
                _syncingToCanvas = false;
            };

            TreePanel.SelectedTreeItems.CollectionChanged += OnTreeSelectionChanged;
        }

        private void OnTreeSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_syncingToTree) return;
            var points = TreePanel.SelectedTreeItems.OfType<PathPoint>().ToList();
            if (points.Count == 0) return;
            if (points.Count == 1 && ReferenceEquals(Canvas.SelectedPoint, points[0])) return;

            _syncingToCanvas = true;
            var parentPath = FindParentPath(points[0]);
            if (parentPath != null && !ReferenceEquals(Canvas.ActivePath, parentPath))
            {
                Canvas.ActivePath    = parentPath;
                Canvas.ActiveMission = FindParentMission(parentPath);
            }
            Canvas.SelectedPoints.Clear();
            foreach (var pt in points) Canvas.SelectedPoints.Add(pt);
            Canvas.SelectedPoint = points[^1];
            _syncingToCanvas = false;
        }

        // ── Menu ──────────────────────────────────────────────────────────────

        private void WireMenuBar()
        {
            MenuBar.NewFileRequested    += (_, _) => HandleNewFile();
            MenuBar.OpenFileRequested   += async (_, _) => await HandleOpenFile();
            MenuBar.SaveFileRequested   += async (_, _) => await HandleSaveFile();
            MenuBar.SaveFileAsRequested += async (_, _) => await HandleSaveFileAs();
            MenuBar.CloseRequested      += (_, _) => GetMainWindow?.Invoke()?.Close();
            MenuBar.SelectAllRequested  += (_, _) => Canvas.SelectAllPointsCommand.Execute(null);
            MenuBar.UndoRequested       += (_, _) => _undoRedo.Undo();
            MenuBar.RedoRequested       += (_, _) => _undoRedo.Redo();

            MenuBar.LineToolRequested += (_, _) =>
            {
                Ribbon.DrawSegment = SegmentType.Line;
                Ribbon.ActiveTool  = ActiveTool.AddPoint;
            };
            MenuBar.ArcToolRequested += (_, _) =>
            {
                Ribbon.DrawSegment = SegmentType.Arc;
                Ribbon.ActiveTool  = ActiveTool.AddPoint;
            };

            MenuBar.CircleRequested    += async (_, _) => await HandleCircle();
            MenuBar.TranslateRequested += async (_, _) => await HandleTranslate();
            MenuBar.RotateRequested    += async (_, _) => await HandleRotate();
            MenuBar.ScaleRequested     += async (_, _) => await HandleScale();
            MenuBar.MirrorRequested    += async (_, _) => await HandleMirror();
        }

        // ── Plik ─────────────────────────────────────────────────────────────

        private void HandleNewFile()
        {
            _undoRedo.Clear();
            TreePanel.Projects.Clear();
            Canvas.ActivePath    = null;
            Canvas.ActiveMission = null;
            Canvas.SelectedPoint = null;
            _currentFilePath     = null;

            var project = new Project("Nowy projekt");
            TreePanel.Projects.Add(project);
        }

        private async Task HandleOpenFile()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var files = await TopLevel.GetTopLevel(win)!.StorageProvider
                .OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Otwórz projekt",
                    FileTypeFilter = [new FilePickerFileType("Robot Field Plan") { Patterns = ["*.rfp"] }],
                    AllowMultiple = false
                });

            if (files.Count == 0) return;
            var path = files[0].Path.LocalPath;

            try
            {
                var json     = await File.ReadAllTextAsync(path);
                var projects = ProjectSerializer.Deserialize(json);
                _undoRedo.Clear();
                TreePanel.Projects.Clear();
                Canvas.ActivePath    = null;
                Canvas.ActiveMission = null;
                Canvas.SelectedPoint = null;
                foreach (var p in projects) TreePanel.Projects.Add(p);
                _currentFilePath = path;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Błąd odczytu: {ex.Message}");
            }
        }

        private async Task HandleSaveFile()
        {
            if (_currentFilePath == null)
                await HandleSaveFileAs();
            else
                await SaveToFile(_currentFilePath);
        }

        private async Task HandleSaveFileAs()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var file = await TopLevel.GetTopLevel(win)!.StorageProvider
                .SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title            = "Zapisz projekt",
                    DefaultExtension = "rfp",
                    FileTypeChoices  = [new FilePickerFileType("Robot Field Plan") { Patterns = ["*.rfp"] }]
                });

            if (file == null) return;
            _currentFilePath = file.Path.LocalPath;
            await SaveToFile(_currentFilePath);
        }

        private async Task SaveToFile(string path)
        {
            try
            {
                var json = ProjectSerializer.Serialize(TreePanel.Projects);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Błąd zapisu: {ex.Message}");
            }
        }

        // ── Rysowanie — narzędzia ─────────────────────────────────────────────

        private async Task HandleCircle()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var dlg = new CircleDialog();
            await dlg.ShowDialog(win);
            if (dlg.Result is not { } r) return;

            // Użyj aktywnej ścieżki; jeśli brak — utwórz nową w aktywnej misji
            var path       = Canvas.ActivePath;
            var mission    = Canvas.ActiveMission
                ?? TreePanel.Projects.SelectMany(p => p.Missions).FirstOrDefault();
            if (mission == null) return;

            bool newPath = path == null;
            if (newPath)
            {
                path = new MissionPath($"Ścieżka {mission.Paths.Count + 1}");
                mission.Paths.Add(path);
                Canvas.ActivePath    = path;
                Canvas.ActiveMission = mission;
                TreePanel.SelectedItem = path;
            }

            var (cx, cy, radius, n, dir) = r;
            var step = (dir == ArcDirection.CW ? 1 : -1) * 2 * Math.PI / n;

            var added = new List<PathPoint>();
            for (int i = 0; i <= n; i++)
            {
                var angle = i * step;
                var pt    = new PathPoint(
                    Math.Round(cx + radius * Math.Cos(angle), 2),
                    Math.Round(cy + radius * Math.Sin(angle), 2))
                {
                    ArcRadius = radius   // ustawione dla wszystkich perimeter (umożliwia identyfikację przy resize)
                };
                if (i < n)
                {
                    pt.SegmentOut   = SegmentType.Arc;
                    pt.ArcDirection = dir;
                }
                added.Add(pt);
            }

            // Punkt środkowy — przechowuje promień okręgu
            var center = new PathPoint(Math.Round(cx, 2), Math.Round(cy, 2))
            {
                IsCenter  = true,
                ArcRadius = radius
            };
            added.Add(center);

            var startIndex = path!.Points.Count;
            foreach (var pt in added)
                path.Points.Add(pt);

            Canvas.SelectedPoints.Clear();
            Canvas.SelectedPoints.Add(center);
            Canvas.SelectedPoint   = center;
            TreePanel.SelectedItem = center;

            if (newPath)
            {
                // Undo usuwa całą ścieżkę
                _undoRedo.Record(new LambdaAction(
                    UndoAction: () =>
                    {
                        mission.Paths.Remove(path);
                        if (ReferenceEquals(Canvas.ActivePath, path))
                            Canvas.ActivePath = null;
                    },
                    RedoAction: () =>
                    {
                        mission.Paths.Add(path);
                        Canvas.ActivePath    = path;
                        Canvas.ActiveMission = mission;
                    }
                ));
            }
            else
            {
                // Undo usuwa tylko dodane punkty
                _undoRedo.Record(new CircleAction(path, startIndex, added));
            }
        }

        // ── Transformacje ─────────────────────────────────────────────────────

        private async Task HandleTranslate()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var dlg = new TranslateDialog();
            await dlg.ShowDialog(win);
            if (dlg.Result is not { } r) return;

            var pts = GetTransformTarget();
            if (pts.Count == 0) return;

            var before = Snapshot(pts);
            foreach (var pt in pts) { pt.X += r.Dx; pt.Y += r.Dy; }
            _undoRedo.Record(new TransformPointsAction(before, Snapshot(pts)));
        }

        private async Task HandleRotate()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var dlg = new RotateDialog();
            await dlg.ShowDialog(win);
            if (dlg.Result is not { } angleDeg) return;

            var pts = GetTransformTarget();
            if (pts.Count == 0) return;

            var before        = Snapshot(pts);
            var (cx, cy)      = Centroid(pts);
            var rad           = angleDeg * Math.PI / 180.0;
            var cos           = Math.Cos(rad);
            var sin           = Math.Sin(rad);

            foreach (var pt in pts)
            {
                var dx = pt.X - cx;
                var dy = pt.Y - cy;
                pt.X = Math.Round(cx + dx * cos - dy * sin, 4);
                pt.Y = Math.Round(cy + dx * sin + dy * cos, 4);
            }
            _undoRedo.Record(new TransformPointsAction(before, Snapshot(pts)));
        }

        private async Task HandleScale()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var dlg = new ScaleDialog();
            await dlg.ShowDialog(win);
            if (dlg.Result is not { } factor || factor <= 0) return;

            var pts = GetTransformTarget();
            if (pts.Count == 0) return;

            var before   = Snapshot(pts);
            var (cx, cy) = Centroid(pts);

            foreach (var pt in pts)
            {
                pt.X = Math.Round(cx + (pt.X - cx) * factor, 4);
                pt.Y = Math.Round(cy + (pt.Y - cy) * factor, 4);
                if (pt.SegmentOut == SegmentType.Arc)
                    pt.ArcRadius = Math.Max(pt.ArcRadius * factor, 1);
            }
            _undoRedo.Record(new TransformPointsAction(before, Snapshot(pts)));
        }

        private async Task HandleMirror()
        {
            var win = GetMainWindow?.Invoke();
            if (win == null) return;

            var dlg = new MirrorDialog();
            await dlg.ShowDialog(win);
            if (dlg.Result is not { } angleDeg) return;

            var pts = GetTransformTarget();
            if (pts.Count == 0) return;

            var before   = Snapshot(pts);
            var (cx, cy) = Centroid(pts);
            var theta    = angleDeg * Math.PI / 180.0;
            var cos2     = Math.Cos(2 * theta);
            var sin2     = Math.Sin(2 * theta);

            foreach (var pt in pts)
            {
                var tx = pt.X - cx;
                var ty = pt.Y - cy;
                pt.X = Math.Round(cx + tx * cos2 + ty * sin2, 4);
                pt.Y = Math.Round(cy + tx * sin2 - ty * cos2, 4);
                if (pt.SegmentOut == SegmentType.Arc)
                    pt.ArcDirection = pt.ArcDirection == ArcDirection.CW
                        ? ArcDirection.CCW : ArcDirection.CW;
            }
            _undoRedo.Record(new TransformPointsAction(before, Snapshot(pts)));
        }

        // ── Helpery ───────────────────────────────────────────────────────────

        private IList<PathPoint> GetTransformTarget()
        {
            if (Canvas.SelectedPoints.Count > 0) return Canvas.SelectedPoints;
            if (Canvas.ActivePath?.Points is { Count: > 0 } pts) return pts;
            return [];
        }

        private static (double cx, double cy) Centroid(IList<PathPoint> pts)
            => (pts.Average(p => p.X), pts.Average(p => p.Y));

        private static List<PointSnapshot> Snapshot(IList<PathPoint> pts)
            => pts.Select(p => new PointSnapshot(p, p.X, p.Y, p.ArcRadius, p.ArcDirection)).ToList();

        // ── Dodaj ścieżkę (dialog) ────────────────────────────────────────────

        private async Task HandleAddPath()
        {
            if (ShowAddPathDialog == null) return;

            var missions    = TreePanel.Projects.SelectMany(p => p.Missions).ToList();
            if (missions.Count == 0) return;

            var activeMission = Canvas.ActiveMission ?? missions.First();
            var defaultName   = $"Ścieżka {activeMission.Paths.Count + 1}";

            var result = await ShowAddPathDialog(missions, defaultName);
            if (result is not { } r) return;

            var path = new MissionPath(r.Name);
            r.Mission.Paths.Add(path);
            Canvas.ActivePath    = path;
            Canvas.ActiveMission = r.Mission;
            TreePanel.SelectedItem = path;
            _undoRedo.Record(new AddPathAction(r.Mission, path));
        }

        // ── Properties ────────────────────────────────────────────────────────

        private void WireProperties()
        {
            Properties.GetParentPath = point => TreePanel.FindParentPath(point);
        }

        // ── Find helpers ──────────────────────────────────────────────────────

        private Mission? FindParentMission(MissionPath path) => TreePanel.FindParentMission(path);
        private MissionPath? FindParentPath(PathPoint point) => TreePanel.FindParentPath(point);
    }
}
