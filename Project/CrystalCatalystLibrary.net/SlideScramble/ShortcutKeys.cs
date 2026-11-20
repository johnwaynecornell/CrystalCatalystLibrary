using CrystalCatalystLibrary.net;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SlideScramble.Tests")]

namespace SlideScramble;

internal sealed class ShortcutKeys
{
    private readonly HashSet<int> held = new();
    public bool Shift => held.Contains((int)KeyCode.Shift_Left) || held.Contains((int)KeyCode.Shift_Right);
    public bool Control => held.Contains((int)KeyCode.Control_Left) || held.Contains((int)KeyCode.Control_Right);
    public bool Other => held.Contains((int)KeyCode.Alt_Left) || held.Contains((int)KeyCode.Alt_Right) ||
                         held.Contains((int)KeyCode.Super_Left) || held.Contains((int)KeyCode.Super_Right) ||
                         held.Contains((int)KeyCode.Meta_Left) || held.Contains((int)KeyCode.Meta_Right);

    public bool Press(int key)
    {
        if (key is (int)KeyCode.Shift_Left or (int)KeyCode.Shift_Right or
            (int)KeyCode.Control_Left or (int)KeyCode.Control_Right or
            (int)KeyCode.Alt_Left or (int)KeyCode.Alt_Right or
            (int)KeyCode.Super_Left or (int)KeyCode.Super_Right or
            (int)KeyCode.Meta_Left or (int)KeyCode.Meta_Right)
        {
            held.Add(key);
            return true;
        }
        return false;
    }

    public void Release(int key) => held.Remove(key);
    public void Clear() => held.Clear();
    public bool IsReverse(int key) => !Other && Control && Shift && (key == 'r' || key == 'R' || key == 18);
    public bool AllowOrdinary => !Control && !Other;
}