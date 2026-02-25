
using System;
using System.Dynamic;
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)
// https://github.com/ezyybin604/terminalfactory/

public struct Tile
{
    public char type;
    public string subtype; ///machine type/resource in tile/fruit type
    public int prog; // Amount of tile in tile/machine progress/stored amount/energy amount
    public string item; // machine output/storage type
}
public struct Point
{
    public int x;
    public int y;
    public Point(int ix, int iy)
    {
        x = ix;
        y = iy;
    }
    public Point()
    {
        x = 0;
        y = 0;
    }
}
public struct Chunk
{
    public int x;
    public int y;
    // [x][y]
    public Tile[][] data;
    public string customToString()
    {
        string result = "[";
        for (int i=0;i<data.Length;i++)
        {
            result += "[";
            for (int z=0;z<data[i].Length;z++) {
                result += String.Format("\"{0}\"", data[i][z].type.ToString());
                if (z+1 < data[i].Length)
                {
                    result += ",";
                }
            }
            result += "]";
            if (i+1 < data.Length)
            {
                result += ",\n";
            }
        }
        result += "]";
        return result;
    }
}

class TopBar
{
    public bool showTips = true;
    public string tip = "default tip";
    public long lastTipChange = DateTime.MinValue.Ticks;
    public Dictionary<string, string[]> tips = new Dictionary<string, string[]>();
    public int menuSelection = 0;
    public bool manualTip;
}

// in-ven-tory
class Slot
{
    public int num;
    public string item = "";
}

class Game
{
    // 4 Scenes: game,end,(invintory/inv caus i dont know how to spell),pause,craft
    string scene = "game";
    GameData gd = new GameData("gamedata");
    Thread? gameThread;
    Point scroll = new Point();
    Point cursor = new Point();
    Factory factory = new Factory();
    List<ConsoleKeyInfo> readkeylog = new List<ConsoleKeyInfo>();
    DateTime time = DateTime.Now;
    TopBar topbar = new TopBar();
    public Dictionary<string, string[]> menus = new Dictionary<string, string[]>();
    Slot[] inventory = new Slot[150];
    HashSet<int> linesToUpdate = new HashSet<int>(); // i didnt renember what the data type was called so i had to google it
    string currentTipText = "";
    List<string> invlist = new List<string>();
    void generateNeeded()
    {
        int w = (int)Math.Ceiling((double)(Console.WindowWidth/Factory.chunkSize));
        int h = (int)Math.Ceiling((double)((Console.WindowHeight-2)/Factory.chunkSize));
        w+=2;
        h+=2;
        int sx = (int)Math.Floor((double)(scroll.x/Factory.chunkSize));
        int sy = (int)Math.Floor((double)(scroll.y/Factory.chunkSize));
        for (int x=0;x<w;x++)
        {
            for (int y=0;y<h;y++)
            {
                factory.generateChunk(x+sx, y+sy);
            }
        }
    }
    void initStuff()
    {
        factory.initFactory();
        topbar.tips.Add("game", [
            "Use WASD to move",
            "Press P to pause",
            "Press K to break/collect",
            "Press O to place",
            "Press I to open inventory"
        ]);
        topbar.tips.Add("pause", [
            "Use WS to change selection",
            "Press Z to select"
        ]);
        topbar.tips.Add("inv", [
            "Use RF to change selection",
            "Press W to select (use item)",
            "Press A to enter crafts menu",
            "Press S to go back",
            "Press H to delete item"
        ]);
        topbar.tips.Add("craft", [
            "Use WS to change selection",
            "Press Z to select",
            "Press X to go back"
        ]);
        topbar.tips.Add("end", ["now go away"]);

        menus.Add("pause", [
            "Resume Game|resume",
            "Save|save",
            "Quit (go away)|quit"
        ]);
        menus.Add("craft", []); // figure this one out later
        menus.Add("inv", new string[150]); // dynamic menu, based off inventory variable

        gameThread = new Thread(runTheGameIg);

        for (int i=0;i<inventory.Length;i++)
        {
            inventory[i] = new Slot();
        }
    }
    void displayMenuLine(int i)
    {
        string[] si = menus[scene][i].Split("|");
        Console.ResetColor();
        if (i+2 > Console.WindowHeight-1)
        {
            return;
        }
        Console.SetCursorPosition(0, i+2);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.ForegroundColor = ConsoleColor.DarkRed;
        if (i == topbar.menuSelection)
        {
            factory.invertColors();
            si[0] = "- " + si[0];
        }
        Console.SetCursorPosition(0, i+2);
        Console.WriteLine(si[0]);
    }
    void menuDisplay()
    {
        if (scene == "inv")
        {
            updateInventory();
        }
        Console.SetCursorPosition(0, 2);
        for (int i=0;i<menus[scene].Length;i++)
        {
            displayMenuLine(i);
        }
    }
    void updateMenu()
    {
        for (int i=0;i<menus[scene].Length;i++)
        {
            if (linesToUpdate.Contains(i))
            {
                displayMenuLine(i);
            }
        }
        linesToUpdate.Clear();
    }
    void selectItemMenu()
    {
        string[] st = menus[scene][topbar.menuSelection].Split("|");
        switch (scene)
        {
            case "pause":
                switch (st[1])
                {
                    case "resume":
                        scene = "game";
                        displayStuff();
                        break;
                    case "save":
                        // finish later
                        break;
                    case "quit":
                        scene = "end";
                        break;
                }
                break;
        }
    }
    void updateBar()
    {
        string tipText = "";
        tipText = topbar.tip;
        if (tipText != currentTipText)
        {
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(tipText);
        } else
        {
            Console.WriteLine();
        }
    }
    void displayStuff()
    {
        linesToUpdate.Clear();
        Console.Clear();
        updateBar();
        Console.WriteLine(new string('~', Console.WindowWidth));
        Console.SetCursorPosition(0, 2);
        if (scene != "game")
        {
            menuDisplay();
            return;
        }
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            factory.displayLine(i+scroll.y, cursor, scroll);
        }
    }
    void updateScreen()
    {
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            if (linesToUpdate.Contains(i+scroll.y))
            {
                Console.SetCursorPosition(0, i+2);
                factory.displayLine(i+scroll.y, cursor, scroll);
            }
        }
        linesToUpdate.Clear();
    }
    void adjustCamera()
    {
        cursor.x = Math.Max(cursor.x, 0);
        while (!(scroll.x <= cursor.x))
        {
            scroll.x--;
        }
        while (!(cursor.x <= scroll.x+Console.WindowWidth-1))
        {
            scroll.x++;
        }

        while (!(scroll.y <= cursor.y))
        {
            scroll.y--;
        }
        while (!(cursor.y <= scroll.y+Console.WindowHeight-3))
        {
            scroll.y++;
        }

        generateNeeded();
    }
    void updateInventory()
    {
        invlist.Clear();
        for (int i=0;i<inventory.Length;i++)
        {
            menus["inv"][i] = "";
            Slot slot = inventory[i];
            if (slot.num > 0)
            {
                invlist.Add(slot.item);
            }
        }
        for (int i=0;i<invlist.Count;i++)
        {
            menus["inv"][i] = invlist[i];
        }
    }
    void useInput(ConsoleKeyInfo key)
    {
        char ch = key.KeyChar;
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return;
        }
        switch (scene)
        {
            case "game":
                switch (ch)
                {
                    case 'a':
                        cursor.x--;
                        linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 's':
                        cursor.y++;
                        linesToUpdate.Add(cursor.y);
                        linesToUpdate.Add(cursor.y-1);
                        adjustCamera();
                        break;
                    case 'w':
                        cursor.y--;
                        linesToUpdate.Add(cursor.y);
                        linesToUpdate.Add(cursor.y+1);
                        adjustCamera();
                        break;
                    case 'd':
                        cursor.x++;
                        linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 'p':
                        scene = "pause";
                        topbar.menuSelection = 0;
                        displayStuff();
                        break;
                    case 'i':
                        scene = "inv";
                        topbar.menuSelection = 0;
                        updateInventory();
                        displayStuff();
                        break;
                    case 'k':
                        // break/collect
                        int i=0;
                        Tile curs = factory.giveMeTheTile(cursor.x, cursor.y);
                        string info = gd.getInfo(curs.type.ToString() + "." + curs.subtype)[1];
                        if (info == "")
                        {
                            info = gd.getInfo(curs.type.ToString())[1];
                        }
                        if (info != "")
                        {
                            while (inventory[i].num < 1 &&
                                !(inventory[i].item == "" ||
                                inventory[i].item == info)
                                && inventory[i].num < 1000
                                && i < inventory.Length)
                            {
                                i++;
                            }
                            if (i < inventory.Length)
                            {
                                bool giveitem = true;
                                if (curs.type == 'i')
                                {
                                    // idk dont do anything
                                } else if (curs.type == 'f')
                                {
                                    curs.prog--;
                                    if (curs.prog < 1)
                                    {
                                        curs.subtype = "";
                                        curs.type = '`';
                                    }
                                } else if (curs.type == 'b')
                                {
                                    if (curs.prog < 1)
                                    {
                                        giveitem = false;
                                    } else
                                    {
                                        curs.prog--;
                                    }
                                } else
                                {
                                    curs.subtype = "";
                                    curs.type = '`';
                                }
                                if (giveitem)
                                {
                                    if (inventory[i].item == "")
                                    {
                                        inventory[i].item = info;
                                    }
                                    inventory[i].num++;
                                }
                                factory.setTile(cursor.x, cursor.y, curs);
                            }
                        }
                        break;
                    case 'o':
                        // place
                        break;
                }
                break;
            case "pause":
                switch (ch)
                {
                    case 'w':
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        linesToUpdate.Add(topbar.menuSelection);
                        updateMenu();
                        break;
                    case 's':
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        linesToUpdate.Add(topbar.menuSelection);
                        updateMenu();
                        break;
                    case 'z':
                        selectItemMenu();
                        break;
                    default:
                        break;
                }
                topbar.menuSelection = Math.Clamp(topbar.menuSelection, 0, menus["pause"].Length-1);
                break;
        }
    }
    void unnessaryFunctionForDecidingManualTips()
    {
        if (scene == "game")
        {
            //string tip = "";
            Tile curs = factory.giveMeTheTile(cursor.x, cursor.y);
            string info = gd.getInfo(curs.type.ToString() + "." + curs.subtype)[0];
            if (info == "")
            {
                info = gd.getInfo(curs.type.ToString())[0];
            }
            if (info == "")
            {
                topbar.manualTip = false;
            } else
            {
                topbar.manualTip = true;
                topbar.tip = info;
            }
        } else if (scene == "craft")
        {
            // do later, craft ingredienterwkjsn
        } else
        {
            topbar.manualTip = false;
        } // maybe inv later but idk what that could be
        if (topbar.manualTip)
        {
            topbar.lastTipChange = DateTime.MinValue.Ticks;
        }
    }
    void inputSutff() // dont try and merge this with the main function (runthegameig) it wont end well
    {
        ConsoleKeyInfo input;
        while (true)
        {
            input = Console.ReadKey(true);
            readkeylog.Add(input);
        }
    }
    void runTheGameIg()
    {
        Point windowSizePrevious = new Point(Console.WindowWidth, Console.WindowHeight);
        Point windowSize = new Point();
        Point previousCamera = new Point(-1, 0);
        string previousScene = scene;
        while (scene != "end")
        {
            if (previousScene != scene)
            {
                previousScene = scene;
                topbar.lastTipChange = DateTime.MinValue.Ticks;
            }
            if (!previousCamera.Equals(scroll))
            {
                generateNeeded();
                displayStuff();
                previousCamera = scroll;
            }
            time = DateTime.Now;
            unnessaryFunctionForDecidingManualTips();
            if (time.Ticks-(TimeSpan.TicksPerSecond * 5) > topbar.lastTipChange && !topbar.manualTip)
            {
                int itip = (int)Math.Round(factory.generateRange(0, topbar.tips[scene].Length-1));
                topbar.tip = topbar.tips[scene][itip];
                topbar.lastTipChange = time.Ticks;
            }
            windowSize = new Point(Console.WindowWidth, Console.WindowHeight);
            if (!windowSize.Equals(windowSizePrevious))
            {
                windowSizePrevious = windowSize;
                generateNeeded();
                adjustCamera();
                displayStuff();
            }
            while (readkeylog.Count > 0)
            {
                useInput(readkeylog[0]);
                readkeylog.RemoveAt(0);
            }
            updateScreen();
            updateBar();
            Thread.Sleep(50);
            //topbar.tip = String.Format("({0}, {1}), ({2}, {3})", scroll.x, scroll.y, cursor.x, cursor.y);
        }
        bye();
    }
    void bye()
    {
        Console.Clear();
        Console.WriteLine("bye");
        Thread.Sleep(1000);
        Environment.Exit(0);
    }
    public static void Main()
    {
        Console.Clear();
        Game game = new Game();
        game.factory = new Factory();
        if (!File.Exists(game.factory.savefile))
        {
            Console.WriteLine("No savefile found.");
            Console.Write("Skip intro? (y/n):");
            char res = '\0';
            if (!((res = Console.ReadKey().KeyChar) == 'y'))
            {
                Console.Clear();
                Console.WriteLine(@"
Your town was overtaken by a DRAGON.
A group survived, (that includes you)
But it is hungry.
None of you know how to feed a creature of such scale,
But you have the knowledge of a empty field nearby
that could be the perfect spot for a factory to
pump out continous food and water for the dragon.

(Press ENTER to continue)");
                Console.ReadLine();
                Console.WriteLine(@"
Nobody follows, so to keep secrecy while you travel.

(Press ENTER to start)");
                Console.ReadLine();
            }
            Console.Title = "terminalfactory";
            while (game.gd.state == "prep")
            {
                Console.Clear();
                Console.WriteLine("Preparing.");
                Thread.Sleep(100);
            }
            if (game.gd.state == "done")
            {
                game.initStuff();
                if (game.gameThread != null)
                {
                    game.gameThread.Start();
                    game.inputSutff();
                }
            } else
            {
                Console.WriteLine("An error happened with GameData: " + game.gd.state);
            }
            game.bye();
        }
    }
}
