using System.Collections.ObjectModel;

namespace RobotFieldUi.Models.TreePanel;

public class MissionPath
{
    public string Name { get; set; } = "Nowa Ścieżka";
    public ObservableCollection<PathPoint> Points { get; } = new();

    public MissionPath(string name)
    {
        Name = name;
    }
}