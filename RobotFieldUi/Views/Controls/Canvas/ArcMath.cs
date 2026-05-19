using System;
using Avalonia;
using Avalonia.Media;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.Views.Controls.Canvas;

/// <summary>
/// Geometria łuków kołowych w układzie ekranowym (Y rośnie w dół).
/// Przechowujemy: radius + ArcDirection (CW/CCW).
/// </summary>
internal static class ArcMath
{
    // ── Środek łuku ────────────────────────────────────────────────────────────

    /// <summary>
    /// Wyznacza środek okręgu na podstawie dwóch punktów ścieżki, promienia i kierunku.
    /// Zwraca null gdy promień jest za mały (r &lt; chord/2).
    /// </summary>
    public static Point? Center(Point p1, Point p2, double radius, ArcDirection dir)
    {
        var mx = (p1.X + p2.X) / 2;
        var my = (p1.Y + p2.Y) / 2;
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;
        var d  = Math.Sqrt(dx * dx + dy * dy);
        if (d < 1e-6 || radius < d / 2 - 1e-4) return null;
        var h  = Math.Sqrt(Math.Max(0, radius * radius - (d / 2) * (d / 2)));
        // Wersja prostopadła do cięciwy (w lewo od P1→P2): (-dy/d, dx/d)
        var px = -dy / d;
        var py =  dx / d;
        // CCW: środek po stronie przeciwnej do wybrzuszenia (center jest "w górę" gdy arc idzie "w dół")
        var sign = dir == ArcDirection.CCW ? -1.0 : 1.0;
        return new Point(mx + sign * h * px, my + sign * h * py);
    }

    // ── Uchwyt do przeciągania ─────────────────────────────────────────────────

    /// <summary>
    /// Punkt na łuku najdalszy od cięciwy — tu rysujemy pomarańczowy uchwyt.
    /// </summary>
    public static Point Handle(Point p1, Point p2, double radius, ArcDirection dir)
    {
        var c = Center(p1, p2, radius, dir);
        if (c == null)
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        var cx = c.Value.X;
        var cy = c.Value.Y;
        var mx = (p1.X + p2.X) / 2;
        var my = (p1.Y + p2.Y) / 2;
        var lx = mx - cx;
        var ly = my - cy;
        var len = Math.Sqrt(lx * lx + ly * ly);
        if (len < 1e-6)
            return new Point(mx, my);
        return new Point(cx + radius * lx / len, cy + radius * ly / len);
    }

    // ── Wyznaczenie łuku z punktu pośredniego ─────────────────────────────────

    /// <summary>
    /// Oblicza promień i kierunek łuku przechodzącego przez P1, via, P2
    /// (koło opisane na trójkącie).
    /// </summary>
    public static (double radius, ArcDirection dir, bool valid)
        FromVia(Point p1, Point via, Point p2)
    {
        var ax = via.X - p1.X;
        var ay = via.Y - p1.Y;
        var bx = p2.X  - p1.X;
        var by = p2.Y  - p1.Y;
        var D  = 2 * (ax * by - ay * bx);
        if (Math.Abs(D) < 1e-10) return (0, ArcDirection.CCW, false);

        var ux = (by * (ax * ax + ay * ay) - ay * (bx * bx + by * by)) / D;
        var uy = (ax * (bx * bx + by * by) - bx * (ax * ax + ay * ay)) / D;
        var r  = Math.Sqrt(ux * ux + uy * uy);

        // Iloczyn wektorowy (P2-P1) × (via-P1) — znak określa stronę
        var cross = bx * (via.Y - p1.Y) - by * (via.X - p1.X);
        var dir   = cross > 0 ? ArcDirection.CCW : ArcDirection.CW;

        return (r, dir, true);
    }

    // ── Konwersja dla Avalonii ─────────────────────────────────────────────────

    public static SweepDirection ToSweep(ArcDirection dir)
        => dir == ArcDirection.CW
            ? SweepDirection.Clockwise
            : SweepDirection.CounterClockwise;
}
