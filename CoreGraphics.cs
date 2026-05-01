
using SDL3;

namespace gameRunner;

// sdl stuff mainly instead of corewrite handling terminal

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
    public static SDL.Color createColor(byte r, byte g, byte b, byte a=(byte)SDL.AlphaOpaque) {
        SDL.Color res = new SDL.Color
        {
            R = r,
            G = g,
            B = b,
            A = a
        };
        return res;
    }
    nint renderer;
    nint window;
    nint windowSurface;
    SDL.FRect textRect;
    //SDL.Color transparent = new() { R = 0, G = 0, B = 0, A = 0 }; // it just REFUSES to be constant (i tried to put it in writetext)
    void writeText(string c, int x, int y, string font, SDL.Color fg){
        nint surface = TTF.RenderTextBlended(fonts[font], c, (uint)c.Length, fg);
        nint texture = SDL.CreateTextureFromSurface(renderer, surface);
        textRect.X = x;
        textRect.Y = y; // fix weird text bug later
        SDL.DestroySurface(surface);
        if (!SDL.RenderTexture(renderer, texture, textRect, windowSurface))
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("help i dont know how to surface: {0}", SDL.GetError()));
        }
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
        windowSurface = SDL.GetWindowSurface(window);
        initFont("sans", "opensans.ttf", 40); // opensans_40
        bool loop = true;
        SDL.Color black = createColor(0, 0, 0);
        while (loop)
        {
            while (SDL.PollEvent(out SDL.Event e))
            {
                if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                {
                    loop = false;
                }
            }
            switch (tc.mode)
            {
                case "prompt":
                    writeText("test text", 10, 10, "sans_40", black);
                    break;
            }
            SDL.SetRenderDrawColor(renderer, 255, 255, 255, 0);
            SDL.RenderClear(renderer);
            SDL.RenderPresent(renderer);
        }
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        TTF.Quit();
        SDL.Quit();
    }
}