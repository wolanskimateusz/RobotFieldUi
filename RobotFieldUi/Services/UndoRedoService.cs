using System.Collections.Generic;

namespace RobotFieldUi.Services;

public interface IUndoableAction
{
    void Undo();
    void Redo();
}

public class UndoRedoService
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Record(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.TryPop(out var action))
        {
            action.Undo();
            _redoStack.Push(action);
        }
    }

    public void Redo()
    {
        if (_redoStack.TryPop(out var action))
        {
            action.Redo();
            _undoStack.Push(action);
        }
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
