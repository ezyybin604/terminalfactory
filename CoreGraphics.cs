
using Raylib_cs;

namespace gameRunner;

// raylib stuff mainly instead of corewrite handling terminal

public class WindowHandler
{
    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    [System.STAThread]
    public void Loop()
    {
        Raylib.InitWindow(800, 480, "terminalfactory");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            Raylib.DrawText("Hello, world!", 12, 12, 20, Color.White);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}