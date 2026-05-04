
using SDL3;

namespace gameRunner;

public class UIElement
{
    // Text input, Button
    public string type = "";
    public SDL.FRect rect;
    public string font = "";
    public string contents = "";
    public required WindowHandler window;
    public SDL.Color[] color = [];
    public void Draw()
    {
        window.drawRect(rect, color[0], color[1], 20, 2);
    }
}
