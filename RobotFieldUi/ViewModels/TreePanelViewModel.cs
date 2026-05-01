using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.ViewModels;

public partial class TreePanelViewModel : ViewModelBase
{
    public ObservableCollection<Project> Projects { get; } = new();
    
    [ObservableProperty]
    private object? _selectedItem;

    public TreePanelViewModel()
    {
        // Dane testowe — usuniemy je gdy będzie zapis/odczyt pliku
        var project = new Project("Projekt testowy");
        var mission = new Mission("Misja 1");
        var path = new MissionPath("Ścieżka 1");

        path.Points.Add(new PathPoint(0, 0));
        path.Points.Add(new PathPoint(10, 5));
        path.Points.Add(new PathPoint(20, 0));

        mission.Paths.Add(path);
        project.Missions.Add(mission);
        Projects.Add(project);
    }
}