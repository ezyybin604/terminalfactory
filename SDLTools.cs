
using SDL3;

namespace gameRunner;


// my solution to gamerunner classes needing sdl tools
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
    private static float getRangeBetween(float n1, float n2, float prog)
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
    public static SDL.FColor Lerp(SDL.FColor cols, SDL.FColor cole, float prog)
    {
        return new SDL.FColor
        {
            R = getRangeBetween(cols.R, cole.R, prog),
            G = getRangeBetween(cols.G, cole.G, prog),
            B = getRangeBetween(cols.B, cole.B, prog),
            A = getRangeBetween(cols.A, cole.A, prog),
        };
    }

    public static SDL.FColor Multiply(SDL.FColor col, SDL.FColor colm)
    {
        return new SDL.FColor { R = col.R * colm.R/255, G = col.G * colm.G/255, B = col.B * colm.B/255, A = col.A * colm.A/255 };
    }
    
    public static SDL.FColor Copy(SDL.FColor value)
    {
        return new SDL.FColor{R = value.R, G = value.G, B = value.B, A = value.A};
    }
}
