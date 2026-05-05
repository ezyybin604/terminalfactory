
using System.Reflection;
using SDL3;

namespace gameRunner;

public class UIElement
{
    // Text input, Button
    public required int id;
    public string type = "";
    public SDL.FRect rect;
    public string font = "";
    public string contents = "";
    public required WindowHandler window;
    public SDL.Color[] color = [];
    public bool hovering = false;
    private SDL.FColor prevc;
    private SDL.FColor curcol;
    public int col_idx = 0;
    public double transition_time = 1; // in seconds
    public double time = 0;
    private double flash_time = 0;
    private bool flash = false;
    public int cursorpos = -1;
    private SDL.FColor getStatic()
    {
        if (col_idx == 0)
        {
            return SDLTools.Cast(color[col_idx]);
        } else
        {
            return SDLTools.Multiply(SDLTools.Cast(color[0]), SDLTools.Cast(color[col_idx]));
        }
    }
    private void getColor()
    {
        if (time != transition_time)
        {
            curcol = SDLTools.Lerp(
                prevc,
                getStatic(),
                (float)(time/transition_time)
            );
        } else if (!curcol.Equals(color[col_idx]))
        {
            curcol = getStatic();
        }
    }
    public void Draw()
    {
        flash_time += window.deltaTime;
        if (flash_time > 0.5)
        {
            flash_time = 0;
            flash = !flash;
        }
        int curve = 12;
        if (type == "input") curve = 5;
        window.drawRect(rect, SDLTools.Cast(curcol), color[1], curve, 1);
        if (type == "button")
        {
            window.writeText(contents, rect.X+(rect.W/2), rect.Y+(rect.H/2), font, color[2], SDLTools.Get(TextA.CENTER, TextA.CENTER));
        } else if (type == "input")
        {
            string contentswc = contents;
            if (flash && WindowHandler.selected == id && cursorpos > -1) contentswc = contentswc.Insert(cursorpos, "|");
            window.writeText(contentswc, 10+rect.X, rect.Y+(rect.H/2), font, color[2],  SDLTools.Get(TextA.LEFT, TextA.CENTER), new SDL.FRect
            {
                X = 0,
                W = 100
            });
        }
        getColor();
        hovering = SDL.PointInRectFloat(WindowHandler.cursor, rect);
        if (time != transition_time)
        {
            time += window.deltaTime;
            time = Math.Min(time, transition_time);
        }
    }
    public void changeColor(int idx)
    {
        if (idx != col_idx)
        {
            time = 0;
            prevc = SDLTools.Copy(curcol);
            col_idx = idx;
        }
    }
}
