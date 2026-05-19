using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.Services;

// ── pomocnicze typy ────────────────────────────────────────────────────────────

public record PrevPointChange(
    PathPoint Point,
    SegmentType OldSegment, double OldRadius, ArcDirection OldDir,
    SegmentType NewSegment, double NewRadius, ArcDirection NewDir);

public record PointSnapshot(PathPoint Pt, double X, double Y, double ArcRadius, ArcDirection ArcDir);

// ── edycja punktów ─────────────────────────────────────────────────────────────

public class AddPointAction(
    MissionPath Path, PathPoint Pt, int Index, PrevPointChange? PrevChange) : IUndoableAction
{
    public void Undo()
    {
        Path.Points.RemoveAt(Index);
        if (PrevChange is { } c)
        {
            c.Point.SegmentOut   = c.OldSegment;
            c.Point.ArcRadius    = c.OldRadius;
            c.Point.ArcDirection = c.OldDir;
        }
    }

    public void Redo()
    {
        if (PrevChange is { } c)
        {
            c.Point.SegmentOut   = c.NewSegment;
            c.Point.ArcRadius    = c.NewRadius;
            c.Point.ArcDirection = c.NewDir;
        }
        Path.Points.Insert(Index, Pt);
    }
}

// Usunięcie jednego lub wielu punktów z tej samej ścieżki.
// Lista musi być posortowana rosnąco po Index — undo wstawia w tej kolejności.
public class DeletePointsAction(
    MissionPath Path, List<(int Index, PathPoint Pt)> Removed) : IUndoableAction
{
    public void Undo()
    {
        foreach (var (idx, pt) in Removed.OrderBy(r => r.Index))
            Path.Points.Insert(idx, pt);
    }

    public void Redo()
    {
        foreach (var (_, pt) in Removed)
            Path.Points.Remove(pt);
    }
}

public class ArcHandleChangedAction(
    PathPoint Pt,
    double OldRadius, ArcDirection OldDir,
    double NewRadius, ArcDirection NewDir) : IUndoableAction
{
    public void Undo() { Pt.ArcRadius = OldRadius; Pt.ArcDirection = OldDir; }
    public void Redo() { Pt.ArcRadius = NewRadius; Pt.ArcDirection = NewDir; }
}

// ── transformacje geometryczne ─────────────────────────────────────────────────

public class TransformPointsAction(
    List<PointSnapshot> Before, List<PointSnapshot> After) : IUndoableAction
{
    public void Undo()
    {
        foreach (var s in Before)
        {
            s.Pt.X            = s.X;
            s.Pt.Y            = s.Y;
            s.Pt.ArcRadius    = s.ArcRadius;
            s.Pt.ArcDirection = s.ArcDir;
        }
    }

    public void Redo()
    {
        foreach (var s in After)
        {
            s.Pt.X            = s.X;
            s.Pt.Y            = s.Y;
            s.Pt.ArcRadius    = s.ArcRadius;
            s.Pt.ArcDirection = s.ArcDir;
        }
    }
}

public class CircleAction(MissionPath Path, int StartIndex, List<PathPoint> Added) : IUndoableAction
{
    public void Undo()
    {
        for (int i = Added.Count - 1; i >= 0; i--)
            Path.Points.RemoveAt(StartIndex + i);
    }

    public void Redo()
    {
        for (int i = 0; i < Added.Count; i++)
            Path.Points.Insert(StartIndex + i, Added[i]);
    }
}

// ── struktura drzewa ───────────────────────────────────────────────────────────

public class AddPathAction(Mission Parent, MissionPath Path) : IUndoableAction
{
    public void Undo() => Parent.Paths.Remove(Path);
    public void Redo() => Parent.Paths.Add(Path);
}

public class AddMissionAction(Project Parent, Mission Mission) : IUndoableAction
{
    public void Undo() => Parent.Missions.Remove(Mission);
    public void Redo() => Parent.Missions.Add(Mission);
}

public class DeletePathAction(Mission Parent, MissionPath Path, int Index) : IUndoableAction
{
    public void Undo() => Parent.Paths.Insert(Index, Path);
    public void Redo() => Parent.Paths.Remove(Path);
}

public class DeleteMissionAction(Project Parent, Mission Mission, int Index) : IUndoableAction
{
    public void Undo() => Parent.Missions.Insert(Index, Mission);
    public void Redo() => Parent.Missions.Remove(Mission);
}

public class DeleteProjectAction(
    ObservableCollection<Project> Projects, Project Project, int Index) : IUndoableAction
{
    public void Undo() => Projects.Insert(Index, Project);
    public void Redo() => Projects.Remove(Project);
}

// ── właściwości ────────────────────────────────────────────────────────────────

// apply jest wywoływane zarówno na model jak i na draft w Properties panel
public class RenameAction(
    Action<object, string> Apply, object Target,
    string OldName, string NewName) : IUndoableAction
{
    public void Undo() => Apply(Target, OldName);
    public void Redo() => Apply(Target, NewName);
}

public class MovePointAction(
    Action<PathPoint, double, double> Apply, PathPoint Pt,
    double OldX, double OldY, double NewX, double NewY) : IUndoableAction
{
    public void Undo() => Apply(Pt, OldX, OldY);
    public void Redo() => Apply(Pt, NewX, NewY);
}

// ── lambda (do prostych operacji w MainWindowViewModel) ───────────────────────

public class LambdaAction(Action UndoAction, Action RedoAction) : IUndoableAction
{
    public void Undo() => UndoAction();
    public void Redo() => RedoAction();
}

// ── zbiorcze ──────────────────────────────────────────────────────────────────

// Actions musi być posortowane rosnąco po indeksie w obrębie tego samego rodzica.
// Undo: kolejność rosnąca (wstawiamy od niskiego indeksu).
// Redo: kolejność malejąca (usuwamy od wysokiego indeksu żeby nie przesuwać indeksów).
public class BulkAction(IReadOnlyList<IUndoableAction> Actions) : IUndoableAction
{
    public void Undo()
    {
        foreach (var a in Actions)
            a.Undo();
    }

    public void Redo()
    {
        for (int i = Actions.Count - 1; i >= 0; i--)
            Actions[i].Redo();
    }
}
