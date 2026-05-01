using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RobotFieldUi.Views.Controls;

public class GridOverlay : Control
{
    public static readonly StyledProperty<double> GridSpacingProperty =
        AvaloniaProperty.Register<GridOverlay, double>(nameof(GridSpacing), defaultValue: 40.0);

    public static readonly StyledProperty<double> OffsetXProperty =
        AvaloniaProperty.Register<GridOverlay, double>(nameof(OffsetX));

    public static readonly StyledProperty<double> OffsetYProperty =
        AvaloniaProperty.Register<GridOverlay, double>(nameof(OffsetY));

    public double GridSpacing { get => GetValue(GridSpacingProperty); set => SetValue(GridSpacingProperty, value); }
    public double OffsetX     { get => GetValue(OffsetXProperty);     set => SetValue(OffsetXProperty, value); }
    public double OffsetY     { get => GetValue(OffsetYProperty);     set => SetValue(OffsetYProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OffsetXProperty ||
            change.Property == OffsetYProperty ||
            change.Property == GridSpacingProperty)
            InvalidateVisual();
    }

    private readonly Pen _gridPen = new(
        new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), thickness: 0.5);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var w       = Bounds.Width;
        var h       = Bounds.Height;
        var spacing = GridSpacing;

        // Przesunięcie fazy — rysujemy od ujemnej reszty, żeby siatka zawsze wypełniała cały obszar
        var phaseX = ((OffsetX % spacing) + spacing) % spacing;
        var phaseY = ((OffsetY % spacing) + spacing) % spacing;

        for (var x = phaseX - spacing; x < w; x += spacing)
            context.DrawLine(_gridPen, new Point(x, 0), new Point(x, h));

        for (var y = phaseY - spacing; y < h; y += spacing)
            context.DrawLine(_gridPen, new Point(0, y), new Point(w, y));
    }
}
