using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RobotFieldUi.Views.Controls;

public class GridOverlay : Control
{
    // Właściwość odstępu siatki — można bindować z zewnątrz
    public static readonly StyledProperty<double> GridSpacingProperty =
        AvaloniaProperty.Register<GridOverlay, double>(
            nameof(GridSpacing), defaultValue: 20.0);

    public double GridSpacing
    {
        get => GetValue(GridSpacingProperty);
        set => SetValue(GridSpacingProperty, value);
    }

    // Pen dla linii siatki
    private readonly Pen _gridPen = new(
        new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
        thickness: 0.5);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var width = Bounds.Width;
        var height = Bounds.Height;
        var spacing = GridSpacing;

        // Linie pionowe
        for (double x = 0; x < width; x += spacing)
            context.DrawLine(_gridPen,
                new Point(x, 0),
                new Point(x, height));

        // Linie poziome
        for (double y = 0; y < height; y += spacing)
            context.DrawLine(_gridPen,
                new Point(0, y),
                new Point(width, y));
    }
}