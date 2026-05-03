
using E604terminalfactory;
using SDL3;

namespace gameRunner;

// sdl stuff mainly instead of corewrite handling terminal
// also rule: ONLY file w/unsafe functions

public class WindowHandler
{
    static Dictionary<string, nint> fonts = new Dictionary<string, nint>();
    public required TileConsole tc;
    public static void initFont(string font, string file, float size)
    {
        string id = font + "_" + ((int)size).ToString();
        fonts.Add(id, TTF.OpenFont("data/" + file, size));
        Console.WriteLine(String.Format("Initalized font {0} in size {1} as {2}", font, size, id));
        if (fonts[id] == 0)
        {
            SDL.LogError(SDL.LogCategory.System, $"Font could not initalize: {SDL.GetError()}");
        }
    }
    public static void initFonts(string font, string file, float[] sizes)
    {
        foreach (float size in sizes)
        {
            initFont(font, file, size);
        }
    }
    public unsafe void drawRect(SDL.Rect rect, SDL.Color col)
    {
        SDL.SetRenderDrawColor(renderer, col.R, col.G, col.B, col.A);
        SDL.RenderFillRect(renderer, (nint)(&rect));
    }
    public static SDL.Color createColor(byte r, byte g, byte b, byte a=(byte)SDL.AlphaOpaque) {
        return new SDL.Color { R = r, G = g, B = b, A = a };
    }
    public static SDL.Rect createRect(int x, int y, int w, int h) {
        return new SDL.Rect { X = x, Y = y, W = w, H = h };
    }
    public const nint NULL = 0;
    public nint renderer;
    nint window;
    nint windowSurface;
    SDL.FRect textRect;
    Point windowSize;
    List<UIElement> ui = new List<UIElement>();
    public static Point getWindowSize(nint window)
    {
        int w, h;
        SDL.GetWindowSize(window, out w, out h);
        return new Point(w, h);
    }
    public static int align(int algn, int p, int size)
    {
        return p-(size/2*algn);
    }
    unsafe void writeText(string c, int x, int y, string font, SDL.Color fg, int[]? alignment=null) {
        if (alignment == null)
        {
            alignment = [0, 0];
        }
        nint surface = TTF.RenderTextBlended(fonts[font], c, (uint)c.Length, fg);
        SDL.Surface surf = *(SDL.Surface*)surface.ToPointer();
        nint texture = SDL.CreateTextureFromSurface(renderer, surface);
        textRect.W = surf.Width;
        textRect.H = surf.Height;
        textRect.X = align(alignment[0], x, surf.Width);
        textRect.Y = align(alignment[1], y, surf.Height);
        SDL.DestroySurface(surface);
        SDL.RenderTexture(renderer, texture, NULL, textRect);
        SDL.DestroyTexture(texture);
    }
    [STAThread]
    public void Loop()
    {
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("SDL could not initialize: {0}", SDL.GetError()));
            return;
        }
        if (!TTF.Init())
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL_TTF could not initialize: {SDL.GetError()}");
            return;
        }
        if (!SDL.CreateWindowAndRenderer("terminalfactory", 1200, 600, 0, out window, out renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
            return;
        }
        windowSize = getWindowSize(window);
        windowSurface = SDL.GetWindowSurface(window);
        initFont("consbold", "consbold.ttf", 30); // consbold_30
        initFonts("sans", "opensans.ttf", [20, 8, 15]); // opensans_ 20,8,15
        bool loop = true;
        SDL.Color black = createColor(0, 0, 0);
        SDL.Color titleColor = createColor(255, 128, 0);

        // Text alignments
        int[] leftlower = SDLTools.Get(TextA.LEFT, TextA.LOWER);
        int[] leftcenter = SDLTools.Get(TextA.LEFT, TextA.CENTER);
        int[] rightcenter = SDLTools.Get(TextA.RIGHT, TextA.CENTER);
        // Text alignments end
        //SDL.GLSetAttribute(SDL.GLAttr.DoubleBuffer, 0);
        ui.Add(new UIElement
        {
            window = this,
            type = "button",
            contents = "Test",
            rect = createRect((windowSize.x/2)-75, 150, 150, 50),
            color = black,
            font = "opensans_15"
        });
        ulong lastTick;
        int nsDelay = 1000/30;
        int nearestSleep = 0;
        while (loop)
        {
            lastTick = SDL.GetTicks();
            while (SDL.PollEvent(out SDL.Event e))
            {
                if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                {
                    loop = false;
                }
            }
            SDL.SetRenderDrawColor(renderer, 255, 255, 255, 0);
            SDL.RenderClear(renderer);
            writeText(nearestSleep.ToString(), 0, 0, "sans_8", black);
            foreach (UIElement element in ui)
            {
                element.Draw();
            }
            switch (tc.mode)
            {
                case "prompt":
                    if (tc.misctext.ContainsKey("vers"))
                    {
                        writeText(tc.misctext["vers"], 10, windowSize.y-10, "sans_20", black, leftlower);
                    }
                    if (tc.misctext.ContainsKey("name"))
                    {
                        string[] spln = tc.misctext["name"].Split("|");
                        writeText(spln[0], (windowSize.x/2)-10, 100, "consbold_30", titleColor, rightcenter);
                        writeText(spln[1], (windowSize.x/2)+10, 100, "consbold_30", SDLTools.Invert(titleColor), leftcenter);
                    }
                    if (tc.currentText.Count > 0)
                    {
                        // add prompt stuff here
                    }
                    break;
            }
            SDL.RenderPresent(renderer);
            nearestSleep = (int)(SDL.GetTicks()-lastTick);
            Thread.Sleep(Math.Max(1, nsDelay-nearestSleep));
        }
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        TTF.Quit();
        SDL.Quit();
    }
}