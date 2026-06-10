using CrystalCatalystLibrary.net;

namespace SpiroSwirlyDemo;

public class ClickConverter
{
    public struct state
    {
        public bool isDown;
        public int x, y;
        public double time;
        
        public state()
        {
            isDown = false;
            x = 0;
            y = 0;
        }
        
        public void mouse_down(int x, int y, double time)
        {
            this.isDown = true;
            this.x = x;
            this.y = y;
            this.time = time;
        }

        public bool mouse_up(int x, int y, double time)
        {
            bool rc = false;
            if (time <= this.time + 1)
            {
                double xx = Math.Pow(Math.Abs(x - this.x), 2);
                double yy = Math.Pow(Math.Abs(y - this.y), 2);
                double l = Math.Sqrt(xx + yy);
                rc = l < 10;
            }
            this.isDown = false;
            
            return rc;
        }
    }
    public CrystalWindow.Delegate_on_mouse_down? on_mouse_click;
    public state[] buttons = new state[10];
    
    public ClickConverter(CrystalWindow.Delegate_on_mouse_down? on_mouse_click = null)
    {
        this.on_mouse_click = on_mouse_click;
    }
    
    public void OnMouseDown(CrystalWindow windowHandle, int button, int x, int y)
    {
        buttons[button].mouse_down(x, y, windowHandle.uptimeSeconds());
    }

    public void OnMouseUp(CrystalWindow windowHandle, int button, int x, int y)
    {
        if (buttons[button].mouse_up(x, y, windowHandle.uptimeSeconds()))
        {
            on_mouse_click?.Invoke(windowHandle, button, x, y);
        }
    }
}