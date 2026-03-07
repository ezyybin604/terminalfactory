
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)
// https://github.com/ezyybin604/terminalfactory/

// todo:
/*
    - saving (save data serialize)
    - world ticking
    - make the machines do recipes
    - pipe stuff (and energy)
    - do Factory.getRegions function (section world into 3x3 areas, region top-left points are returned)
    - finish FileManagement.saveStuff function
    - regions loader
    - make adjustCamera not a disaster (extra low priority) (dont make it use weird while loops)
*/

public struct Tile
{
    public char type = ' ';
    public string subtype = ""; ///machine type/resource in tile/fruit type
    public int prog; // Amount of tile in tile/machine progress/stored amount/energy amount
    //public string item; // machine output/storage type
    public int amount; // amount of item for those tiles that need it
    public Tile() {}
    public Tile(char t, string subt, int prg, int amt)
    {
        type = t;
        subtype = subt;
        prog = prg;
        amount = amt;
    }
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
    public Point(Point point)
    {
        x = point.x;
        y = point.y;
    }
    public void transform(Point point)
    {
        x += point.x;
        y += point.y;
    }
    public Point getTransform(Point point)
    {
        return new Point(x+point.x, y+point.y);
    }
    public Point getReverse()
    {
        return new Point(-x, -y);
    }
}

class TopBar
{
    //couldnt bother to go through all tip variable refs so i renamed it
    public string tipt = "Have you tried waiting?";
    public long lastTipChange = DateTime.MinValue.Ticks;
    public Dictionary<string, string[]> tips = new Dictionary<string, string[]>();
    public int menuSelection = 0;
    public int menuScroll = 0;
    public bool manualTip;
    public int tipPriority;
    public void changeTip(string tipi, int priority, int extrams=0, bool forced=false)
    {
        if (priority >= tipPriority || forced)
        {
            lastTipChange = DateTime.Now.Ticks - (extrams * TimeSpan.TicksPerMillisecond);
            tipPriority = priority;
            tipt = tipi;
        }
    }
    public void changeTip(int priority, string tipi, bool forced=false)
    {
        changeTip(tipi, priority, 0, forced);
    }
}

// in-ven-tory
public class Slot
{
    public int num = 0;
    public string item = "";
    public Slot Copy()
    {
        Slot slot = new Slot();
        slot.item = item;
        slot.num = num;
        return slot;
    }
    public Slot(string ite, int nu)
    {
        item = ite;
        num = nu;
    }
    public Slot(string ite)
    {
        item = ite;
        num = 1;
    }
    public Slot() {}
}

class Game
{
    // 4 Scenes: game,end,(invintory/inv caus i dont know how to spell),pause,craft
    string scene = "game";
    Thread? gameThread;
    Point scroll = new Point();
    Point cursor = new Point(2,2);
    Factory factory = new Factory();
    List<ConsoleKeyInfo> readkeylog = new List<ConsoleKeyInfo>();
    DateTime time = DateTime.Now;
    TopBar topbar = new TopBar();
    public Dictionary<string, string[]> menus = new Dictionary<string, string[]>();
    Inventory inventory = new Inventory();
    FileManagement gdm = new FileManagement();
    string currentTipText = "";
    int? usingItem = null;
    public void loadData()
    {
        inventory.data = gdm.LoadWorld(factory);
        inventory.hasData = true;
        Point[] curscr = gdm.LoadMachines(factory);
        cursor = curscr[0];
        scroll = curscr[1];
        generateNeeded();
        adjustCamera();
    }
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
            "Press I to open inventory",
            "Press L to view tile contents",
            "Press M to exhange contents with tile"
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
            "Press H to delete item",
            "If your inventory is empty, you will exit.",
            "When an item is deleted, selection will be too."
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
        menus.Add("end", ["Why are you reading this exactly?"]);
        menus.Add("craft_raw", []);
        menus.Add("craft", []);
        menus.Add("craft_desc", []);
        menus.Add("inv", new string[Inventory.Length]); // dynamic menu, based off inventory variable

        gameThread = new Thread(runTheGameIg);
        gameThread.Name = "Game Logic";

        for (int i=0;i<Inventory.Length && !inventory.hasData;i++)
        {
            inventory.data[i] = new Slot();
        }
        inventory.gd = factory.gd;
        factory.inventory = inventory;
    }
    void displayMenuLine(int i)
    {
        string[] si = menus[scene][i].Split("|");
        int gi = i+2-topbar.menuScroll;
        Console.ResetColor();
        if (gi > Console.WindowHeight-1 || gi < 2)
        {
            return;
        }
        Console.SetCursorPosition(0, gi);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.ForegroundColor = ConsoleColor.DarkRed;
        if (i == topbar.menuSelection)
        {
            factory.invertColors();
            si[0] = "- " + si[0];
        }
        Console.SetCursorPosition(0, gi);
        Console.Write(si[0]);
    }
    void menuDisplay()
    {
        if (scene == "inv")
        {
            inventory.fix();
            menus["inv"] = inventory.getMenu();
        } // prev for loop menus[scene].Length-topbar.menuScroll
        int menuLength = Math.Min(Console.WindowHeight-2, menus[scene].Length);
        for (int i=0;i<menuLength;i++)
        {
            displayMenuLine(i+topbar.menuScroll);
        }
    }
    void updateMenu()
    {
        for (int i=0;i<menus[scene].Length;i++)
        {
            if (factory.linesToUpdate.Contains(i))
            {
                displayMenuLine(i);
            }
        }
        factory.linesToUpdate.Clear();
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
                        if (!Directory.Exists(factory.savefile))
                        {
                            Directory.CreateDirectory(factory.savefile);
                        }
                        gdm.SaveStuff(factory, cursor, scroll);
                        break;
                    case "quit":
                        scene = "end";
                        break;
                }
                break;
        }
    }
    void updateBar(bool forceReplace=false)
    {
        string tipText = "";
        tipText = topbar.tipt;
        if (tipText != currentTipText || forceReplace)
        {
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
            string tipTextCopy = tipText;
            char modifer = '\0';
            if (tipTextCopy[0] == '/')
            {
                modifer = tipTextCopy[1];
                tipTextCopy = tipTextCopy.Substring(2);
            }
            switch (modifer)
            {
                case 'd': // deny color
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 'g': // good color
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                default:
                    break;
            }
            if (tipTextCopy.Length > Console.WindowWidth)
            {
                Console.Write("TLDW");
            } else
            {
                Console.Write(tipTextCopy);
            }
            currentTipText = tipText;
        }
    }
    void lookThisOneIsJustToDrawTheBigLine()
    {
        Console.ResetColor();
        Console.SetCursorPosition(0, 1);
        Console.WriteLine(new string('~', Console.WindowWidth));
    }
    void displayStuff()
    {
        factory.linesToUpdate.Clear();
        Console.ResetColor();
        Console.Clear();
        updateBar(true);
        lookThisOneIsJustToDrawTheBigLine();
        if (scene != "game")
        {
            menuDisplay();
            lookThisOneIsJustToDrawTheBigLine();
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
            if (factory.linesToUpdate.Contains(i+scroll.y))
            {
                Console.SetCursorPosition(0, i+2);
                factory.displayLine(i+scroll.y, cursor, scroll);
            }
        }
        factory.linesToUpdate.Clear();
    }
    void adjustCamera()
    {
        if (scene != "game")
        {
            int prevScroll = topbar.menuScroll;
            //topbar.menuScroll = -(Math.Clamp(topbar.menuSelection, topbar.menuScroll, topbar.menuScroll+Console.WindowHeight-3)-topbar.menuSelection);
            while (!(topbar.menuScroll <= topbar.menuSelection))
            {
                topbar.menuScroll--;
            }
            while (!(topbar.menuSelection <= topbar.menuScroll+Console.WindowHeight-3))
            {
                topbar.menuScroll++;
            }
            if (prevScroll != topbar.menuScroll)
            {
                menuDisplay();
            }
            return;
        }
        cursor.x = Math.Max(cursor.x, 0);
        // gotta fix this distaster
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
    // make this more general-purpose later /w any catagory (so i can make different machine recipe groups or whatever)
    // leave this message for the recipe determiner ^^^^
    void updateRecipeMenu(string catg="craftingRecipe")
    {
        string[] ent = factory.gd.getKeys(catg);
        List<string> result = new List<string>();
        // i tried foreach loops for once, but then decided they didnt provide the control i was used to
        menus["craft"] = new string[ent.Length];
        menus["craft_desc"] = new string[ent.Length];
        menus["craft_raw"] = new string[ent.Length];
        for (int i=0;i<ent.Length;i++)
        {
            string[] ing = factory.gd.getFromKey(catg, ent[i]).Split(",");
            int numitem = 0;
            List<string> res = new List<string>();
            for (int x=0;x<ing.Length;x++)
            {
                string cur = ing[x];
                if (cur[0] == 'x')
                {
                    cur = cur[1..];
                    int curp;
                    if (int.TryParse(cur, out curp))
                    {
                        numitem = curp;
                    }
                } else
                {
                    res.Add("x" + numitem.ToString() + " " + factory.gd.getFromKey("itemNames", cur));
                }
            }
            menus["craft_raw"][i] = ent[i];
            menus["craft"][i] = factory.gd.getFromKey("itemNames", ent[i]);
            menus["craft_desc"][i] = String.Join(", ", res);
        }
    }
    void useInput(ConsoleKeyInfo key)
    {
        bool forceDisplay = false; // (i have no idea what im doing send help)
        char ch = key.KeyChar;
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return;
        }
        Tile tic = factory.giveMeTheTile(cursor);
        switch (scene)
        {
            case "game":
                switch (ch)
                {
                    case 'a':
                        cursor.x--;
                        factory.linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 's':
                        cursor.y++;
                        factory.linesToUpdate.Add(cursor.y);
                        factory.linesToUpdate.Add(cursor.y-1);
                        adjustCamera();
                        break;
                    case 'w':
                        cursor.y--;
                        factory.linesToUpdate.Add(cursor.y);
                        factory.linesToUpdate.Add(cursor.y+1);
                        adjustCamera();
                        break;
                    case 'd':
                        cursor.x++;
                        factory.linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 'p':
                        scene = "pause";
                        topbar.menuSelection = 0;
                        forceDisplay = true;
                        break;
                    case 'i':
                        topbar.menuSelection = 0;
                        inventory.fix();
                        menus["inv"] = inventory.getMenu();
                        if (inventory.data[0].num > 0)
                        {
                            scene = "inv";
                            forceDisplay = true;
                        } else
                        {
                            topbar.changeTip("/dInventory empty.", 1, 3000);
                        }
                        break;
                    case 'k':
                        if (factory.breakTile(cursor, topbar))
                        {
                            if (tic.type != 'i')
                            {
                                factory.linesToUpdate.Add(cursor.y);
                            }
                        }
                        break;
                    case 'o':
                        // place
                        if (factory.placeTile(usingItem, cursor))
                        {
                            factory.linesToUpdate.Add(cursor.y);
                        }
                        break;
                    case 'l': // view contents
                        if (tic.amount > 0 && factory.gd.getFromKey("tags", "containerTile").Contains(tic.type))
                        {
                            string tip = "x" + tic.amount.ToString() + " " + factory.gd.getFromKey("itemNames", tic.subtype);
                            topbar.changeTip(2, tip);
                        }
                        break;
                    case 'j': // exchange
                        if (factory.gd.getFromKey("tags", "containerTile").Contains(tic.type))
                        {
                            if (tic.amount > 0)
                            {
                                // extract
                                if (inventory.addItem(new Slot(tic.subtype, tic.amount)))
                                {
                                    tic.amount = 0;
                                    factory.setTile(cursor, tic);
                                }
                            } else if (tic.type == '+' && usingItem != null)
                            {
                                // deposit
                                tic.amount = inventory.data[(int)usingItem].num;
                                tic.subtype = inventory.data[(int)usingItem].item;
                                factory.setTile(cursor, tic);
                                inventory.data[(int)usingItem].num = 0;
                                inventory.fix();
                                usingItem = null;
                            }
                        }
                        break;
                }
                break;
            case "pause":
                switch (ch)
                {
                    case 'w':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        break;
                    case 's':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        break;
                    case 'z':
                        selectItemMenu();
                        break;
                    default:
                        break;
                }
                topbar.menuSelection = Math.Clamp(topbar.menuSelection, 0, menus["pause"].Length-1);
                break;
            case "inv":
                switch (ch)
                {
                    case 'r':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        break;
                    case 'f':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        break;
                    case 'w':
                        usingItem = topbar.menuSelection;
                        scene = "game";
                        forceDisplay = true;
                        break;
                    case 'a':
                        scene = "craft";
                        topbar.menuSelection = 0;
                        updateRecipeMenu();
                        forceDisplay = true;
                        break;
                    case 's':
                        scene = "game";
                        forceDisplay = true;
                        break;
                    case 'h':
                        inventory.data[topbar.menuSelection].num = 0;
                        inventory.fix();
                        menus["inv"] = inventory.getMenu();
                        usingItem = null;
                        topbar.menuSelection = Math.Min(topbar.menuSelection, menus["inv"].Length-1);
                        if (inventory.data[0].num < 1)
                        {
                            scene = "game";
                        }
                        forceDisplay = true;
                        break;
                }
                break;
            case "craft":
                switch (ch)
                {
                    case 'w':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        break;
                    case 's':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        break;
                    case 'x':
                        scene = "inv";
                        forceDisplay = true;
                        break;
                    case 'z':
                        // craft process goes here (doit)
                        string result = menus["craft_raw"][topbar.menuSelection];
                        Slot[] recipe = inventory.getRecipe("craftingRecipe", result);
                        if (inventory.verifyRecipe(recipe))
                        {
                            if (inventory.addItem(new Slot(result)))
                            {
                                inventory.removeItems(recipe);
                            }
                        } else
                        {
                            topbar.changeTip(1, "/dNo. You can't do it. stop");
                        }
                        break;
                }
                break;
        }
        if (scene != "game")
        {
            topbar.menuSelection = Math.Clamp(topbar.menuSelection, 0, menus[scene].Length-1);
            factory.linesToUpdate.Add(topbar.menuSelection);
            if (scene == "craft")
            {
                unnessaryFunctionForDecidingTips();
            }
            adjustCamera();
            updateMenu();
        }
        if (forceDisplay)
        {
            topbar.menuScroll = 0;
            topbar.menuSelection = 0;
            displayStuff();
        }
    }
    void unnessaryFunctionForDecidingTips()
    {
        bool repeatTime = time.Ticks-(TimeSpan.TicksPerSecond * 5) > topbar.lastTipChange;
        if (scene == "game")
        {
            Tile curs = factory.giveMeTheTile(cursor);
            string info = factory.gd.autoTilePick(curs, 0);
            bool force = false;
            if (repeatTime && topbar.tipPriority > 1)
            {
                force = true;
            }
            if (info == "")
            {
                topbar.manualTip = false;
            } else
            {
                topbar.manualTip = true;
                topbar.changeTip(1, info, force);
            }
        } else if (scene == "craft")
        {
            topbar.manualTip = false;
            if (factory.linesToUpdate.Count > 0)
            {
                string tpr = menus["craft_desc"][topbar.menuSelection];;
                if (!inventory.verifyRecipe("craftingRecipe", menus["craft_raw"][topbar.menuSelection]))
                {
                    tpr = "/d" + tpr;
                }
                topbar.changeTip(1, tpr);
                //topbar.tip = String.Format("{0}, {1}, {2}", topbar.menuSelection, topbar.menuScroll, topbar.menuScroll+Console.WindowHeight-3);
            }
        } else
        {
            topbar.manualTip = false;
        }
        if (topbar.manualTip && topbar.tipPriority < 2)
        {
            topbar.lastTipChange = DateTime.MinValue.Ticks;
        }
        if (repeatTime && !topbar.manualTip)
        {
            int itip = (int)Math.Round(factory.generateRange(0, topbar.tips[scene].Length-1));
            topbar.changeTip(0, topbar.tips[scene][itip], true);
            topbar.lastTipChange = time.Ticks;
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
        int timer = 0;
        while (scene != "end")
        {
            if (previousScene != scene)
            {
                previousScene = scene;
                topbar.lastTipChange = DateTime.MinValue.Ticks;
                topbar.menuScroll = 0;
            }
            if (!previousCamera.Equals(scroll))
            {
                generateNeeded();
                displayStuff();
                previousCamera = scroll;
            }
            time = DateTime.Now;
            unnessaryFunctionForDecidingTips();
            windowSize = new Point(Console.WindowWidth, Console.WindowHeight);
            if (!windowSize.Equals(windowSizePrevious))
            {
                windowSizePrevious = windowSize;
                generateNeeded();
                adjustCamera();
                displayStuff();
            }
            if (scene != "pause")
            {
                if (timer%10 == 0)
                {
                    factory.updateMachines();
                }
                // tick function goes here
            }
            if (scene == "game")
            {
                updateScreen();
            }
            updateBar();
            while (readkeylog.Count > 0)
            {
                useInput(readkeylog[0]);
                readkeylog.RemoveAt(0);
            }
            timer++;
            timer = Math.Max(-1, timer);
            Thread.Sleep(50);
        }
        bye();
    }
    void bye()
    {
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("bye");
        Thread.Sleep(1000);
        Environment.Exit(0);
    }
    public static void Main()
    {
        Console.Clear();
        Game game = new Game();
        bool sf = Directory.Exists(game.factory.savefile);
        bool alsf = true;
        if (sf)
        {
            string? inp = null;
            while (inp == null)
            {
                Console.Clear();
                Console.Write("A save was found. Load a different one? (Press ENTER for default):");
                inp = Console.ReadLine();
                if (inp != null && (inp.Contains("/") || inp.Contains("\\")))
                {
                    inp = null;
                }
            }
            if (inp != "")
            {
                game.factory.savefile = inp;
                alsf = Directory.Exists(game.factory.savefile);
            }
            game.loadData();
        }
        if (!sf && alsf)
        {
            if (sf)
            {
                Console.WriteLine("New save detected.");
            } else
            {
                Console.WriteLine("No save found.");
            }
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
        }
        try
        {
            Console.Title = "terminalfactory";
        }
        catch (PlatformNotSupportedException)
        {
            Console.Write("no");
        }
        while (game.factory.gd.state == "prep")
        {
            Console.Clear();
            Console.WriteLine("Preparing.");
            Thread.Sleep(100);
        }
        if (game.factory.gd.state == "done")
        {
            game.initStuff();
            if (game.gameThread != null)
            {
                game.gameThread.Start();
                game.inputSutff();
            }
        } else
        {
            Console.WriteLine("\nAn error happened with GameData: " + game.factory.gd.state);
            Console.ReadLine();
        }
        game.bye();
    }
}
