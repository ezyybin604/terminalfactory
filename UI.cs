
using E604terminalfactory;
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
        window.drawRect(rect, color[0], color[1], 12, 1);
        if (type == "button")
        {
            window.writeText(contents, rect.X+(rect.W/2), rect.Y+(rect.H/2), font, color[2], SDLTools.Get(TextA.CENTER, TextA.CENTER));
        } else if (type == "input")
        {
            // stuff here later
        }
    }
}
