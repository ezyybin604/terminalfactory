
// entry point

using E604terminalfactory;

namespace gameRunner;

class Program {
    public static void Main() {
        new Game
        {
            cusc = new TileConsole()
        }.Start();
    }
}

public class TileConsole
{
    public static bool consoleMode = true;
    public static void Error(string s)
    {
        Console.WriteLine(s); // replace with the err function in unity
        Console.ReadLine();
        Environment.Exit(0);
    }
    public Point getWindowSize(string type)
    { // possible: board size, max text length (or maybe have function that returns if cursor beyond screen), window size
        // window, board, text
        switch (type)
        {
            case "text": case "window":
                return new Point(Console.WindowWidth, Console.WindowHeight);
            case "board":
                return new Point(Console.WindowWidth, Console.WindowHeight-2);
        }
        return new Point();
    }
    public static void SaveSelect(Game game)
    {
        bool sf = game.gdm.savefileExists(game.factory);
        if (game.gdm.optionExists("defaultsave"))
        {
            game.factory.savefile = game.gdm.getOption("defaultsave");
            sf = game.gdm.savefileExists(game.factory);
        }
        bool alsf = true;
        if (sf || Directory.GetDirectories(game.gdm.worldFolder).Length > 0)
        {
            if (!sf)
            {
                game.factory.savefile = JPI.getFilename(Directory.GetDirectories(game.gdm.worldFolder)[0]);
                sf = game.gdm.savefileExists(game.factory);
                if (!sf)
                {
                    TileConsole.Error("Oh no something REALLY went wrong with save " + game.factory.savefile + "\n\nalso Yeah no. I'm not making a edge case for this one. Go away.");
                }
            }
            string? inp = null;
            while (inp == null)
            {
                Console.Clear();
                game.hi();
                Console.WriteLine(String.Format("A save was found. Load a different one? ({0} is selected, type list to list saves)\n(Press ENTER for default):", game.factory.savefile));
                inp = Console.ReadLine();
                if (inp != null && (inp.Contains("/") || inp.Contains("\\")))
                {
                    inp = null;
                }
                if (inp == "list")
                {
                    string[] saves = Directory.GetDirectories(game.gdm.worldFolder);
                    Console.Clear();
                    for (int i=0;i<saves.Length;i++)
                    {
                        Console.WriteLine(JPI.getFilename(saves[i]));
                    }
                    Console.WriteLine("(Press ENTER to exit)");
                    Console.ReadLine();
                    inp = null;
                }
                if (inp == "creative" || inp == "demo" || inp == "tutorial")
                {
                    Console.WriteLine("creative: All crafts in crafting menu are free");
                    Console.WriteLine("demo: No saving, there is a restart button instead of exiting.");
                    Console.WriteLine("tutorial: Spawns a tutorial map that is one screen large. Changes dynamically.");
                    Console.WriteLine("Activate selected Special Mode?");
                    if (Console.ReadKey().KeyChar == 'y')
                    {
                        game.specialMode = inp;
                        inp = "";
                    }
                }
            }
            if (inp != "")
            {
                game.factory.savefile = inp;
                alsf = Directory.Exists(Path.Join(game.gdm.worldFolder, game.factory.savefile));
            }
            if (alsf && game.specialMode != "tutorial")
            {
                game.loadData();
            }
        }
        if (!sf || !alsf)
        {
            if (sf)
            {
                Console.WriteLine("New save detected.");
            } else
            {
                Console.WriteLine("No save found.");
            }
            game.introduction();
        }
    }
    // replace this with interface to unity
    // 3 modes: console, world, menu
    public string mode = "console";
    public void sendTiles(Point startp, Tile[] tiles)
    {
        // empty
    }
    public void setSplash(string text, string versionstr)
    {
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("TERMINALFACTORY");
        Console.Write("\"" + text + "\"");
        Console.SetCursorPosition(0, Console.WindowHeight-1);
        Console.Write(String.Format("terminalfactory {0}, runner:console", versionstr));
        Console.SetCursorPosition(0, 3);
    }
    public void resetScreen(Game game)
    {
        switch (mode)
        {
            case "console":
                Console.Clear();
                break;
            case "menu": case "world":
                game.displayStuff();
                break;
        }
    }
}
