namespace CrystalCatalystLibrary.net;

/// <summary>
/// Specifies the mouse buttons and scroll directions.
/// </summary>
public enum CrystalMouseButton
{
    /// <summary>No button.</summary>
    None = 0,

    /// <summary>Primary (left) mouse button.</summary>
    Left = 1,
    /// <summary>Middle mouse button (scroll wheel click).</summary>
    Middle = 2,
    /// <summary>Secondary (right) mouse button.</summary>
    Right = 3,

    /// <summary>Vertical scroll wheel up.</summary>
    WheelUp = 4,
    /// <summary>Vertical scroll wheel down.</summary>
    WheelDown = 5,
    /// <summary>Horizontal scroll wheel left.</summary>
    WheelLeft = 6,
    /// <summary>Horizontal scroll wheel right.</summary>
    WheelRight = 7,

    /// <summary>Additional button 1.</summary>
    X1 = 8,
    /// <summary>Additional button 2.</summary>
    X2 = 9
}