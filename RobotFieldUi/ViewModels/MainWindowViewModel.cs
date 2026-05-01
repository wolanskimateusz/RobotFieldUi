using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public MenuBarViewModel     MenuBar    { get; } = new();
        public RibbonViewModel      Ribbon     { get; } = new();
        public StatusBarViewModel   StatusBar  { get; } = new();
        public TreePanelViewModel   TreePanel  { get; } = new();
        public CanvasViewModel      Canvas     { get; } = new();
        public PropertiesViewModel  Properties { get; } = new();

        // Ustawiane przez MainWindow.axaml.cs — potrzebne do ShowDialog z referencją do okna
        public Func<IEnumerable<Mission>, string, Task<(string Name, Mission Mission)?>>? ShowAddPathDialog { get; set; }

        public MainWindowViewModel()
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
                        if (Canvas.SelectedPoint != null)
                            TreePanel.SelectedItem = Canvas.SelectedPoint;
                        break;
                    case nameof(CanvasViewModel.ActivePath):
                        if (Canvas.ActivePath != null &&
                            !ReferenceEquals(TreePanel.SelectedItem, Canvas.ActivePath))
                            TreePanel.SelectedItem = Canvas.ActivePath;
                        break;
                }
            };

            Ribbon.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(RibbonViewModel.ActiveTool))
                    Canvas.ActiveTool = Ribbon.ActiveTool;
            };

            Ribbon.AddPathRequested += async (_, __) => await HandleAddPath();

            TreePanel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(TreePanelViewModel.SelectedItem)) return;

                Properties.SelectedObject = TreePanel.SelectedItem;

                switch (TreePanel.SelectedItem)
                {
                    case MissionPath path:
                        Canvas.ActivePath    = path;
                        Canvas.ActiveMission = FindParentMission(path);
                        break;
                    case Mission mission:
                        Canvas.ActiveMission = mission;
                        break;
                    case PathPoint point:
                        Canvas.SelectedPoint = point;
                        var parent = FindParentPath(point);
                        if (parent != null) Canvas.ActivePath = parent;
                        Canvas.ActiveMission = parent != null ? FindParentMission(parent) : null;
                        break;
                }
            };
        }

        private async Task HandleAddPath()
        {
            if (ShowAddPathDialog == null) return;

            var missions = TreePanel.Projects
                .SelectMany(p => p.Missions)
                .ToList();

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
        }

        private Mission? FindParentMission(MissionPath path)
        {
            foreach (var project in TreePanel.Projects)
                foreach (var mission in project.Missions)
                    if (mission.Paths.Contains(path))
                        return mission;
            return null;
        }

        private MissionPath? FindParentPath(PathPoint point)
        {
            foreach (var project in TreePanel.Projects)
                foreach (var mission in project.Missions)
                    foreach (var path in mission.Paths)
                        if (path.Points.Contains(point))
                            return path;
            return null;
        }
    }
}
