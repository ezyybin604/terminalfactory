
// entry point

using System.ComponentModel.DataAnnotations;
using E604terminalfactory;

namespace gameRunner;

class Runner {
    public static void Main() {
        TileConsole tc = new TileConsole();
        if (File.Exists("modeoverride"))
        {
            tc.runnerType = File.ReadAllText("modeoverride").Replace("\n", "");
        }
        WindowHandler wh = new WindowHandler{tc = tc};
        new Game
        {
            cusc = tc,
            windowHandler = wh
        }.Start();
    }
}

/*
Save select

Title screen
    - Continue
    - New game
    - Worlds
    - Options ? (style, volume)

New game
    - prompt for world name
    - Enter tutorial automatically
    - (Slow fading introduction/w skip button via esc key, tell how to skip at topright screen, "Hold ESC to skip")

Worlds
    - List worlds, when select one, return to title screen

Options
    - Back
    - Options
*/

public class TileConsole
{
    public static void Error(string s)
    {
        Console.WriteLine(s); // replace with the err function in unity
        Console.ReadLine();
        Environment.Exit(0);
    }
    public static void Log(string s)
    {
        Console.WriteLine(s); // replace with the log function in unity
    }
    public static void Log()
    {
        Console.WriteLine(""); // replace in unity with AIR!!!!!!!!!!!!!!!!!!!!!!!!!!!!
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
    public void startGame(Game game) // dont try and merge this with the main function (runthegameig) it wont end well
    {
        Thread gameThread = new Thread(game.runTheGameIg)
        {
            Name = "Game Logic"
        };
        gameThread.Start();
        if (runnerType == "sdl")
        {
            game.windowHandler.Loop();
            return;
        }
        string[] instantExitModes = ["demo", "tutorial"];
        ConsoleKeyInfo input;
        while (game.scene != "end")
        {
            input = Console.ReadKey(true);
            Game.readkeylog.Add(input);
            if (instantExitModes.Contains(game.specialMode) && game.scene == "pause" && input.KeyChar == 'z' && game.menus["pause"][game.topbar.menuSelection].Split("|")[1] == "quit")
            {
                while (gameThread != null && gameThread.IsAlive)
                {
                    Log("Waiting to restart");
                    Thread.Sleep(100);
                }
            }
        }
    }
    public bool choice(string prompt, Game game, string extra="")
    {
        char res = '\0';
        while (res == '\0')
        {
            Console.Clear();
            if (extra == "hi")
            {
                game.hi();
            }
            Console.Write(prompt);
            res = Console.ReadKey().KeyChar.ToString().ToLower()[0];
            if (res != 'y' && res != 'n')
            {
                res = '\0';
            }
        }
        return res == 'y';
    }
    public static void setCustomMenu(Game game, string[] menu)
    {
        string[] data = new string[menu.Length];
        string[] opts = new string[menu.Length];
        string[] res;
        for (int i=0;i<menu.Length;i++)
        {
            res = menu[i].Split("|");
            data[i] = res[0];
            opts[i] = res[1];
        }
        game.scene = "custom";
        game.menus["custom"] = data;
        game.menus["customopt"] = opts;
        game.displayStuff();
    }
    public static void startSceneSelect(Game game, string scene)
    {
        // Unimplimented: options
        switch (scene)
        {
            case "title":
                bool sf = game.gdm.savefileExists(game.factory.savefile);
                if (game.gdm.optionExists("defaultsave"))
                {
                    game.factory.savefile = game.gdm.getOption("defaultsave");
                    sf = game.gdm.savefileExists(game.factory.savefile);
                }
                List<string> menu = [
                    "New Game|nameprompt",
                    "Options|opt"
                ];
                if (sf)
                {
                    menu.Insert(0, "Continue|start");
                    menu.Insert(2, "Worlds|listworld");
                }
                setCustomMenu(game, menu.ToArray());
                break;
            case "worldlist":
                string[] saves = Directory.GetDirectories(game.gdm.worldFolder);
                List<string> wlist = ["Back|back"];
                foreach (string save in saves)
                {
                    wlist.Add(JPI.getFilename(save) + "|selectworl");
                }
                setCustomMenu(game, wlist.ToArray());
                break;
        }
    }
    //public static void SaveSelect(Game game)
    //{
    //    return;
        // Deprecated (fancy word)
        // bool alsf = true;
        // if (sf || Directory.GetDirectories(game.gdm.worldFolder).Length > 0)
        // {
        //     if (!sf)
        //     {
        //         game.factory.savefile = JPI.getFilename(Directory.GetDirectories(game.gdm.worldFolder)[0]);
        //         sf = game.gdm.savefileExists(game.factory);
        //         if (!sf)
        //         {
        //             Error("Oh no something REALLY went wrong with save " + game.factory.savefile + "\n\nalso Yeah no. I'm not making a edge case for this one. Go away.");
        //         }
        //     }
        //     string? inp = null;
        //     while (inp == null)
        //     {
        //         Console.Clear(); // change to ask tileconsole for savefile prompt
        //         game.hi();
        //         string prmp = String.Format("A save was found. Load a different one? ({0} is selected, type list to list saves)\n(Press ENTER for default):", game.factory.savefile);
        //         Console.WriteLine(prmp);
        //         inp = Console.ReadLine();
        //         if (inp != null && (inp.Contains("/") || inp.Contains("\\")))
        //         {
        //             inp = null;
        //         }
        //         if (inp == "list")
        //         {
        //             string[] saves = Directory.GetDirectories(game.gdm.worldFolder);
        //             Console.Clear();
        //             for (int i=0;i<saves.Length;i++)
        //             {
        //                 Console.WriteLine(JPI.getFilename(saves[i]));
        //             }
        //             Console.WriteLine("(Press ENTER to exit)");
        //             Console.ReadLine();
        //             inp = null;
        //         }
        //         if (inp == "creative" || inp == "demo" || inp == "tutorial")
        //         {
        //             Console.WriteLine("creative: All crafts in crafting menu are free");
        //             Console.WriteLine("demo: No saving, there is a restart button instead of exiting.");
        //             Console.WriteLine("tutorial: Spawns a tutorial map that is one screen large. Changes dynamically.");
        //             Console.WriteLine("Activate selected Special Mode?");
        //             if (Console.ReadKey().KeyChar == 'y')
        //             {
        //                 game.specialMode = inp;
        //                 inp = "";
        //             }
        //         }
        //     }
        //     if (inp != "")
        //     {
        //         game.factory.savefile = inp;
        //         alsf = Directory.Exists(Path.Join(game.gdm.worldFolder, game.factory.savefile));
        //     }
        //     if (alsf && game.specialMode != "tutorial")
        //     {
        //         game.loadData();
        //     }
        // }
        // if (!sf || !alsf)
        // {
        //     if (sf)
        //     {
        //         Console.WriteLine("New save detected.");
        //     } else
        //     {
        //         Console.WriteLine("No save found.");
        //     }
        //     game.introduction();
        // }
    //}
    public string runnerType = "sdl"; // change this to change between sdl/console
    // replace this with interface to unity
    // 3 modes: console, world, menu, prompt
    public string mode = "console";
    public List<string> currentText = [];
    public Dictionary<string,string> misctext = new Dictionary<string, string>();
    public void sendTiles(Point startp, Tile[] tiles)
    {
        // empty
    }
    public void setSplash(string text, string versionstr)
    {
        misctext.TryAdd("name", "TERMINAL|FACTORY");;
        misctext.TryAdd("vers", String.Format("terminalfactory {0}, by:ezyybin604/Ezra", versionstr));
        misctext.TryAdd("quote", "\"" + text + "\"");
        if (runnerType == "sdl")
        {
            return;
        }
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine(misctext["name"].Replace("|", ""));
        Console.Write(misctext["quote"]);
        Console.SetCursorPosition(0, Console.WindowHeight-1);
        Console.Write(misctext["vers"]);
        Console.SetCursorPosition(0, 3);
    }
    public void changeMode(string mod)
    {
        mode = mod;
        currentText.Clear();
    }
    public void writeText(string text)
    {
        switch (mode)
        {
            case "console": case "menu":
                currentText.Add(text);
                break;
            case "prompt":
                currentText = [text];
                break;
        }
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
