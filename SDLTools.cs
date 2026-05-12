
using SDL3;

namespace gameRunner;


// my solution to gamerunner classes needing sdl tools
// and the one handling with unsafe stuff

public enum Algn
{
    leftlower = 0,
    leftcenter = 1,
    rightcenter = 2,
    leftupper = 3, // default
    centercenter = 4
}

public enum TextA
{
    LEFT = 0, // regular
    CENTER = 1,
    RIGHT = 2,
    UPPER = 0, // regular
    LOWER = 2
}

public class SDLTools // MY sdl tools :))
{
    public static int[] Get(TextA x, TextA y)
    {
        return [(int)x, (int)y];
    }
    public static int[] Get(Algn algn)
    {
        switch (algn)
        {
            case Algn.leftlower:
                return Get(TextA.LEFT, TextA.LOWER);
            case Algn.leftcenter:
                return Get(TextA.LEFT, TextA.CENTER);
            case Algn.rightcenter:
                return Get(TextA.RIGHT, TextA.CENTER);
            case Algn.leftupper:
                return Get(TextA.LEFT, TextA.UPPER);
            case Algn.centercenter:
                return Get(TextA.CENTER, TextA.CENTER);
        }
        return Get(Algn.leftupper);
    }
    public static SDL.Color Invert(SDL.Color color)
    {
        return WindowHandler.createColor((byte)(255-color.R), (byte)(255-color.G), (byte)(255-color.B), color.A);
    }
    public static SDL.FRect DivideRect(SDL.FRect rect, float divend)
    {
        return new SDL.FRect{X = rect.X/divend, Y=rect.Y/divend, W=rect.W/divend, H=rect.H/divend};
    }
    public static SDL.FPoint[] DividePoints(SDL.FPoint[] points, float divend)
    {
        SDL.FPoint[] res = new SDL.FPoint[points.Length];
        for (int i=0;i<points.Length;i++)
        {
            res[i] = new SDL.FPoint{X = points[i].X/divend, Y = points[i].Y/divend};
        }
        return res;
    }
    private static float between(float n1, float n2, float prog)
    {
        return n1+((n2-n1)*prog);
    }
    public static SDL.FColor Cast(SDL.Color color)
    {
        return new SDL.FColor{R = color.R, G = color.G, B = color.B, A = color.A};
    }
    public static SDL.Color Cast(SDL.FColor color)
    {
        return new SDL.Color{R = (byte)color.R, G = (byte)color.G, B = (byte)color.B, A = (byte)color.A};
    }
    public static SDL.Rect Cast(SDL.FRect rect)
    {
        return new SDL.Rect{X = (int)rect.X, Y = (int)rect.Y, W = (int)rect.W, H = (int)rect.H};
    }
    public static SDL.FColor Lerp(SDL.FColor cols, SDL.FColor cole, float prog)
    {
        float rprog = Math.Clamp(0, prog, 1);
        return new SDL.FColor
        {
            R = between(cols.R, cole.R, rprog),
            G = between(cols.G, cole.G, rprog),
            B = between(cols.B, cole.B, rprog),
            A = between(cols.A, cole.A, rprog),
        };
    }
    public static SDL.FRect Lerp(SDL.FRect rect1, SDL.FRect rect2, float prog)
    {
        float rprog = Math.Clamp(0, prog, 1);
        return WindowHandler.createRectF(
            between(rect1.X, rect2.X, rprog),
            between(rect1.Y, rect2.Y, rprog),
            between(rect1.W, rect2.W, rprog),
            between(rect1.H, rect2.H, rprog)
        );
    }

    public static SDL.FColor Multiply(SDL.FColor col, SDL.FColor colm)
    {
        return new SDL.FColor { R = col.R * colm.R/255, G = col.G * colm.G/255, B = col.B * colm.B/255, A = col.A * colm.A/255 };
    }
    
    public static SDL.FColor Copy(SDL.FColor value)
    {
        return new SDL.FColor{R = value.R, G = value.G, B = value.B, A = value.A};
    }
    public static SDL.FRect Copy(SDL.FRect value)
    {
        return new SDL.FRect{X = value.X, Y = value.Y, W = value.W, H = value.H};
    }
    public static string RemoveChars(string s, int idx, int len=1)
    {
        return s.Substring(0,idx) + s.Substring(idx+len);
    }
    public static SDL.FRect Transform(SDL.FRect rect, SDL.FPoint pt)
    {
        return WindowHandler.createRectF(rect.X+pt.X, rect.Y+pt.Y, rect.W, rect.H);
    }
}

public unsafe class PointerTools
{ // if this ever crashes (poking into unsafe memory) BLAME THIS CLASS
    public static char GetChar(nint ntn)
    {
        return *(char*)ntn.ToPointer();
    }
    public static SDL.Surface GetSurface(nint ntn)
    {
        return *(SDL.Surface*)ntn.ToPointer();
    }
    public static nint GetPointer(SDL.PixelFormat obj)
    {
        SDL.PixelFormat* ptr = &obj;
        return (nint)ptr;
    }
    public static nint GetPointer(SDL.Rect obj)
    {
        SDL.Rect* ptr = &obj;
        return (nint)ptr;
    }
}
