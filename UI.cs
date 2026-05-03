
using SDL3;

namespace gameRunner;

public class UIElement
{
    // Text input, Button
    public string type = "";
    public SDL.Rect rect;
    public string font = "";
    public string contents = "";
    public required WindowHandler window;
    public SDL.Color color;
    public void Draw()
    {
        window.drawRect(rect, color);
    }
}
