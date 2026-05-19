using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobotFieldUi.Models.TreePanel;
using RobotFieldUi.Services;

namespace RobotFieldUi.ViewModels;

public partial class TreePanelViewModel : ViewModelBase
{
    private readonly UndoRedoService _undoRedo;

    public ObservableCollection<Project> Projects { get; } = new();

    [ObservableProperty]
    private object? _selectedItem;

    public ObservableCollection<object> SelectedTreeItems { get; } = new();

    public event EventHandler<object>? ItemDeleted;

    public TreePanelViewModel(UndoRedoService undoRedo) => _undoRedo = undoRedo;

    // ── usuwanie pojedynczego elementu ────────────────────────────────────────

    [RelayCommand]
    private void DeleteItem(object? item)
    {
        if (item == null) return;

        switch (item)
        {
            case PathPoint point:
                var parentPath = FindParentPath(point);
                if (parentPath != null)
                {
                    var idx = parentPath.Points.IndexOf(point);
                    parentPath.Points.Remove(point);
                    if (ReferenceEquals(SelectedItem, point))
                        SelectedItem = parentPath;
                    _undoRedo.Record(new DeletePointsAction(parentPath,
                        new List<(int, PathPoint)> { (idx, point) }));
                }
                break;

            case MissionPath path:
                var parentMission = FindParentMission(path);
                if (parentMission != null)
                {
                    var idx = parentMission.Paths.IndexOf(path);
                    parentMission.Paths.Remove(path);
                    if (ReferenceEquals(SelectedItem, path))
                        SelectedItem = parentMission;
                    _undoRedo.Record(new DeletePathAction(parentMission, path, idx));
                }
                break;

            case Mission mission:
                var parentProject = FindParentProject(mission);
                if (parentProject != null)
                {
                    var idx = parentProject.Missions.IndexOf(mission);
                    parentProject.Missions.Remove(mission);
                    if (ReferenceEquals(SelectedItem, mission))
                        SelectedItem = parentProject;
                    _undoRedo.Record(new DeleteMissionAction(parentProject, mission, idx));
                }
                break;

            case Project project:
                var pidx = Projects.IndexOf(project);
                Projects.Remove(project);
                if (ReferenceEquals(SelectedItem, project))
                    SelectedItem = null;
                _undoRedo.Record(new DeleteProjectAction(Projects, project, pidx));
                break;
        }

        ItemDeleted?.Invoke(this, item);
    }

    // ── zbiorcze usuwanie ─────────────────────────────────────────────────────

    public void DeleteItemsBulk(IList<object> items)
    {
        // Faza 1: zbierz (rodzic, indeks, element) przed usunięciem
        var captured = new List<(int SortIdx, IUndoableAction Action)>();
        foreach (var item in items)
        {
            switch (item)
            {
                case PathPoint point:
                    var pp = FindParentPath(point);
                    if (pp != null)
                    {
                        var idx = pp.Points.IndexOf(point);
                        captured.Add((idx, new DeletePointsAction(pp,
                            new List<(int, PathPoint)> { (idx, point) })));
                    }
                    break;
                case MissionPath path:
                    var pm = FindParentMission(path);
                    if (pm != null)
                    {
                        var idx = pm.Paths.IndexOf(path);
                        captured.Add((idx, new DeletePathAction(pm, path, idx)));
                    }
                    break;
                case Mission mission:
                    var pj = FindParentProject(mission);
                    if (pj != null)
                    {
                        var idx = pj.Missions.IndexOf(mission);
                        captured.Add((idx, new DeleteMissionAction(pj, mission, idx)));
                    }
                    break;
                case Project project:
                    var pidx = Projects.IndexOf(project);
                    captured.Add((pidx, new DeleteProjectAction(Projects, project, pidx)));
                    break;
            }
        }

        // Faza 2: usuń z modelu
        foreach (var item in items)
        {
            switch (item)
            {
                case PathPoint point:  FindParentPath(point)?.Points.Remove(point); break;
                case MissionPath path: FindParentMission(path)?.Paths.Remove(path); break;
                case Mission mission:  FindParentProject(mission)?.Missions.Remove(mission); break;
                case Project project:  Projects.Remove(project); break;
            }
        }

        SelectedItem = null;
        foreach (var item in items)
            ItemDeleted?.Invoke(this, item);

        // Faza 3: nagraj akcję undo posortowaną rosnąco po indeksie
        if (captured.Count == 0) return;
        var sorted = captured.OrderBy(x => x.SortIdx).Select(x => x.Action).ToList();
        _undoRedo.Record(sorted.Count == 1 ? sorted[0] : new BulkAction(sorted));
    }

    // ── dodawanie ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddMission(object? item)
    {
        if (item is not Project project) return;
        var mission = new Mission($"Misja {project.Missions.Count + 1}");
        project.Missions.Add(mission);
        SelectedItem = mission;
        _undoRedo.Record(new AddMissionAction(project, mission));
    }

    [RelayCommand]
    private void AddPath(object? item)
    {
        if (item is not Mission mission) return;
        var path = new MissionPath($"Ścieżka {mission.Paths.Count + 1}");
        mission.Paths.Add(path);
        SelectedItem = path;
        _undoRedo.Record(new AddPathAction(mission, path));
    }

    // ── helpery ───────────────────────────────────────────────────────────────

    public Mission? FindParentMission(MissionPath path)
    {
        foreach (var project in Projects)
            foreach (var mission in project.Missions)
                if (mission.Paths.Contains(path))
                    return mission;
        return null;
    }

    public Project? FindParentProject(Mission mission)
    {
        foreach (var project in Projects)
            if (project.Missions.Contains(mission))
                return project;
        return null;
    }

    public MissionPath? FindParentPath(PathPoint point)
    {
        foreach (var project in Projects)
            foreach (var mission in project.Missions)
                foreach (var path in mission.Paths)
                    if (path.Points.Contains(point))
                        return path;
        return null;
    }
}
