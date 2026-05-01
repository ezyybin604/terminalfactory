
using SDL3;

namespace gameRunner;

// sdl stuff mainly instead of corewrite handling terminal

public class WindowHandler
{
    static Dictionary<string, nint> fonts = new Dictionary<string, nint>();
    public required TileConsole tc;
    public static void initFont(string font, float size)
    {
        fonts.Add(font + "_" + ((int)size).ToString(), TTF.OpenFont("data/" + font, size));
        Console.WriteLine(String.Format("Initalized font {0} in size {1}", font, size));
    }
    [STAThread]
    public void Loop()
    {
        if (!TTF.Init())
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("SDL_TTF could not initialize: {0}", SDL.GetError()));
            return;
        }
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("SDL could not initialize: {0}", SDL.GetError()));
            return;
        }
        if (!SDL.CreateWindowAndRenderer("terminalfactory", 1200, 600, 0, out var window, out var renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
            return;
        }
        initFont("opensans", 40); // opensans_40
        bool loop = true;
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
                    string text = "test text";
                    //SDL.Surface surface = TTF.RenderTextSolid(fonts["opensans_40"], text, , [0, 0, 0]);
                    //TTF.TTFText ttext = TTF.CreateText(fontEngine, fonts["opensans_40"], text, );
                    //TTF.DestroyText(ttext.Text);
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