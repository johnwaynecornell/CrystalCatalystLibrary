namespace CrystalCatalystLibrary.net;

/// <summary>
/// Specifies the allowed operations for drag-and-drop actions.
/// </summary>
public enum DragActions
{
    /// <summary>No drag operation allowed.</summary>
    DRAG_OPERATION_NONE = 0,
    /// <summary>Data should be copied to the target.</summary>
    DRAG_OPERATION_COPY = 1 << 0,
    /// <summary>Data should be moved to the target.</summary>
    DRAG_OPERATION_MOVE = 1 << 1,
    /// <summary>A link to the data should be created at the target.</summary>
    DRAG_OPERATION_LINK = 1 << 2
}