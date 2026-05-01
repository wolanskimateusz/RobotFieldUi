using System.Collections.ObjectModel;

namespace RobotFieldUi.Models.TreePanel;

public class Project
{
    public string Name { get; set; } = "Nowy Projekt";
    public ObservableCollection<Mission> Missions { get; } = new();

    public Project(string name)
    {
        Name = name;
    }
}