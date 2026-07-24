using System.Runtime.CompilerServices;

namespace CrystalCatalystLibrary.net;

public class CrystalCursorInstance
{
    public CrystalCursor? byValue = null;
    
    /*
     * Note the PixData need only be preserved for the call to Apply
     */
    public Func<(PixData,int,int)>? byReference = null;
    
    public static ConditionalWeakTable<CrystalWindow, CrystalCursorInstance?> weakCursorMap = new ConditionalWeakTable<CrystalWindow, CrystalCursorInstance?>();
    
    public CrystalCursorInstance(CrystalCursor cursor)
    {
        byValue = cursor;
    }
    
    public CrystalCursorInstance(Func<(PixData,int,int)> cursor)
    {
        byReference = cursor;
    }
    
    public bool Apply(CrystalWindow window, bool force = false)
    {
        if (!force &&
            weakCursorMap.TryGetValue(window, out var cursorInstance) && 
                cursorInstance == this)
            return true;
        
        if (byReference != null)
        {
            var (pixData, hotSpotX, hotSpotY) = byReference();
            window.SetCursor(pixData.pix_format,pixData.pix_data, pixData.pix_data_length, pixData.width, pixData.height, hotSpotX, hotSpotY);
            weakCursorMap.AddOrUpdate(window, this);
            return true;
        }
        
        if (byValue != null)
        {
            window.SetStandardCursor(byValue.Value);
            weakCursorMap.AddOrUpdate(window, this);
            return true;
        }
        
        return false;
    }
    
}