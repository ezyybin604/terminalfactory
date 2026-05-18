
using SDL3;

namespace gameRunner;

public class UIElement
{
    // Text input, Button
    required public int id;
    required public string type = "";
    public string action = "uidefault";
    private SDL.FRect rect;
    required public SDL.FRect dynrect;
    public Algn alignment = Algn.leftupper;
    private int[] procalign = [];
    public string font = "sans_8";
    public string contents = "";
    required public WindowHandler window;
    public SDL.Color[] color = [];
    public bool hovering = false;
    private SDL.FColor prevc;
    private SDL.FColor curcol;
    public int col_idx = 0;
    public double transition_time = 0.12f; // in seconds
    public double time = 0;
    public int cursorpos = -1;
    private float cursorscrl = 0;
    public string lastInput = "";
    public nint? texture;
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
    SDL.FPoint size;
    SDL.FRect texturesrc;
    bool assignRect = true;
    public void updateRect()
    {
        if (assignRect)
        {
            size = WindowHandler.createPoint(dynrect.W, dynrect.H);
            if (texture != null)
            {
                size = WindowHandler.getTextureSize((nint)texture);
            }
            procalign = SDLTools.Get(alignment);
            texturesrc = WindowHandler.createRectF(WindowHandler.createPoint(0, 0), SDLTools.Copy(size));
            size = WindowHandler.createPoint(dynrect.W, dynrect.H);
        }
        rect = WindowHandler.createRectF(WindowHandler.createPoint(
            WindowHandler.align(procalign[0], 0, -window.windowSize.x) + WindowHandler.align(procalign[0], dynrect.X, (int)size.X),
            WindowHandler.align(procalign[1], 0, -window.windowSize.y) + WindowHandler.align(procalign[1], dynrect.Y, (int)size.Y)
        ), size);
    }
    public void Draw()
    {
        if (assignRect) updateRect(); assignRect = false;
        int curve = 12;
        if (type == "input") curve = 5;
        if (texture == null)
        {
            window.drawRect(rect, SDLTools.Cast(curcol), color[1], curve, 1);
        } else
        {
            SDL.SetTextureColorModFloat((nint)texture, curcol.R/255, curcol.G/255, curcol.B/255);
            SDL.RenderTexture(window.renderer, (nint)texture, texturesrc, rect);
            SDL.SetTextureColorModFloat((nint)texture, 1, 1, 1);
        }
        if (type == "button")
        {
            window.writeText(contents, rect.X+(rect.W/2), rect.Y+(rect.H/2), font, color[2], Algn.centercenter);
        } else if (type == "input")
        {
            float ytex = rect.Y+(rect.H/2);
            SDL.Point size = window.getStringLength(font, contents.Substring(0, Math.Max(0, cursorpos)));
            if (size.X+5-cursorscrl > rect.W/2) // fix text streching when deleting from scroll
            {
                // right half
                cursorscrl = Math.Max(cursorscrl, Math.Max(rect.W-10, size.X)-(rect.W-10));
            } else
            {
                // left half
                int offset = 0;
                if (lastInput == "backspace") offset = 15;
                cursorscrl = Math.Min(cursorscrl, Math.Max(0, size.X-offset));
            }
            window.writeText(
                contents, 5+rect.X, ytex, font, color[2],
                Algn.leftcenter,
                new SDL.FRect{ X = cursorscrl, W = rect.W-10 }
            );
            if (id == WindowHandler.selected)
            {
                window.SetRenderDrawColor(color[2]);
                SDL.RenderLine(window.renderer, size.X+rect.X+5-cursorscrl, ytex-(size.Y/2), size.X+rect.X+5-cursorscrl, ytex+(size.Y/2));
            }
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
