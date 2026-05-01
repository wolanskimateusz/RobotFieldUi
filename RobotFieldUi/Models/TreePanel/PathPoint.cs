namespace RobotFieldUi.Models.TreePanel;

public class PathPoint
{
    public double X { get; set; } = 0.0;
    public double Y { get; set; } = 0.0;
    
    public string DisplayName => $"Punkt ({X:0.##}, {Y:0.##})";

    public PathPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
    

}