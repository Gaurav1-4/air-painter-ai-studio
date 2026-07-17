namespace AirPainter.Drawing
{
    /// <summary>
    /// Base interface for the Command Pattern.
    /// Allows encapsulating actions (like drawing, erasing) as objects that can be undone/redone.
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
        string Description { get; }
    }
}
