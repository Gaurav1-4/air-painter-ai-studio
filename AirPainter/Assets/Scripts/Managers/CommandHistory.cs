using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Managers
{
    /// <summary>
    /// Manages the Undo and Redo stacks for the application.
    /// Limits history to optimize memory usage on mobile devices.
    /// </summary>
    public class CommandHistory : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Maximum number of strokes stored in history. Limits memory usage.")]
        public int maxHistorySize = 50;

        private Stack<Drawing.ICommand> undoStack = new Stack<Drawing.ICommand>();
        private Stack<Drawing.ICommand> redoStack = new Stack<Drawing.ICommand>();

        public event Action OnHistoryChanged;

        /// <summary>
        /// Executes a command and pushes it to the undo stack. Clears the redo stack.
        /// </summary>
        public void ExecuteCommand(Drawing.ICommand command)
        {
            // Execute it (though in the case of drawing, it's usually already executed as the user drew it)
            // command.Execute(); 
            
            undoStack.Push(command);
            redoStack.Clear(); // New action invalidates the future redo timeline

            TrimHistory();
            OnHistoryChanged?.Invoke();
        }

        public bool Undo()
        {
            if (undoStack.Count == 0) return false;

            var command = undoStack.Pop();
            command.Undo();
            redoStack.Push(command);

            OnHistoryChanged?.Invoke();
            return true;
        }

        public bool Redo()
        {
            if (redoStack.Count == 0) return false;

            var command = redoStack.Pop();
            command.Execute();
            undoStack.Push(command);

            OnHistoryChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }

        private void TrimHistory()
        {
            if (undoStack.Count > maxHistorySize)
            {
                // To trim a stack from the bottom (oldest), we have to reverse it, pop, and reverse back.
                // In production with high performance needs, a circular buffer (Ring Buffer) is better.
                // For simplicity here, we convert to array and take the top N.
                var commands = undoStack.ToArray();
                undoStack.Clear();
                
                // Repopulate skipping the oldest ones
                for (int i = maxHistorySize - 1; i >= 0; i--)
                {
                    undoStack.Push(commands[i]);
                }
                
                // Note: Discarded commands should ideally have a Dispose() called on them
                // to free up their StrokeRenderer back to the pool immediately.
            }
        }

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
    }
}
