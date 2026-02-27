
using System;
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
    public int menuScroll = 0;
    public bool manualTip;
}

// in-ven-tory
class Slot
{
    public int num;
    public string item = "";
    public Slot Copy()
    {
        Slot slot = new Slot();
        slot.item = item;
        slot.num = num;
        return slot;
    }
}

class Game
{
    // 4 Scenes: game,end,(invintory/inv caus i dont know how to spell),pause,craft
    // todo:
    /*
        - craft process
        - saving (save data serialize)
        - world ticking
        - machine forming
        - item names
        - make adjustCamera not a disaster
        - make the menu camera not crash
    */
    string scene = "game";
    Thread? gameThread;
    Point scroll = new Point();
    Point cursor = new Point(2,2);
    Factory factory = new Factory();
    List<ConsoleKeyInfo> readkeylog = new List<ConsoleKeyInfo>();
    DateTime time = DateTime.Now;
    TopBar topbar = new TopBar();
    public Dictionary<string, string[]> menus = new Dictionary<string, string[]>();
    Slot[] inventory = new Slot[150];
    HashSet<int> linesToUpdate = new HashSet<int>(); // i didnt renember what the data type was called so i had to google it
    string currentTipText = "";
    int? usingItem = null;
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
        menus.Add("craft", []);
        menus.Add("craft_desc", []);
        menus.Add("inv", new string[inventory.Length]); // dynamic menu, based off inventory variable

        gameThread = new Thread(runTheGameIg);
        gameThread.Name = "Game Logic";

        for (int i=0;i<inventory.Length;i++)
        {
            inventory[i] = new Slot();
        }
    }
    void displayMenuLine(int i)
    {
        string[] si = menus[scene][i].Split("|");
        int gi = i+2-topbar.menuScroll;
        Console.ResetColor();
        if (gi > Console.WindowHeight-1)
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
            updateInventory();
        }
        for (int i=0;i<menus[scene].Length-topbar.menuScroll;i++)
        {
            displayMenuLine(i+topbar.menuScroll);
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
    void updateBar(bool forceReplace=false)
    {
        string tipText = "";
        tipText = topbar.tip;
        if (tipText != currentTipText || forceReplace)
        {
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);
            Console.Write(tipText);
            currentTipText = tipText;
        }
    }
    void displayStuff()
    {
        linesToUpdate.Clear();
        Console.Clear();
        Console.ResetColor();
        updateBar(true);
        Console.SetCursorPosition(0, 1);
        Console.WriteLine(new string('~', Console.WindowWidth));
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
    int fixInventory()
    {
        Slot[] invcopy = inventory;
        int x=0;
        for (int i=0;i<inventory.Length;i++)
        {
            if (invcopy[i].num > 0)
            {
                if (x != i) { inventory[x] = invcopy[i].Copy(); }
                if (i > x)
                {
                    inventory[i].num = 0;
                }
                x++;
            }
        }
        updateInventory();
        return x;
    }
    void updateInventory()
    {
        invlist.Clear();
        for (int i=0;i<inventory.Length;i++)
        {
            Slot slot = inventory[i];
            if (slot.num > 0)
            {
                invlist.Add(String.Format("x{0}, {1}", slot.num, slot.item));
            }
        }
        menus["inv"] = new string[invlist.Count];
        for (int i=0;i<invlist.Count;i++)
        {
            menus["inv"][i] = invlist[i];
        }
    }
    void breakTile()
    { // ill be real i just wanted the useinput function to not look as giant
        int i=0;
        Tile curs = factory.giveMeTheTile(cursor.x, cursor.y);
        string info = factory.gd.autoTilePick(curs, 1, "blockinfo");
        if (info != "")
        {
            while (!(inventory[i].item == "" || inventory[i].item == info)
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
                    res.Add("x" + numitem.ToString() + " " + cur);
                }
            }
            menus["craft"][i] = ent[i];
            menus["craft_desc"][i] = String.Join(", ", res);
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
                        topbar.menuSelection = 0;
                        fixInventory();
                        updateInventory();
                        if (inventory[0].num > 0)
                        {
                            scene = "inv";
                            displayStuff();
                        }
                        break;
                    case 'k':
                        breakTile();
                        linesToUpdate.Add(cursor.y);
                        break;
                    case 'o':
                        // place
                        if (usingItem != null)
                        {
                            int invlen = fixInventory();
                            Tile tile = new Tile();
                            Slot slot = inventory[(int)usingItem];
                            string info = factory.gd.getFromKey("itemToBlock", slot.item);
                            if (slot.num > 0 && info != "")
                            {
                                string[] infol = info.Split(".");
                                tile.type = infol[0][0];
                                if (infol.Length == 2)
                                {
                                    tile.subtype = infol[1];
                                }
                                inventory[(int)usingItem].num--;
                                factory.setTile(cursor.x, cursor.y, tile);
                                linesToUpdate.Add(cursor.y);
                            }
                        }
                        break;
                }
                break;
            case "pause":
                switch (ch)
                {
                    case 'w':
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        break;
                    case 's':
                        linesToUpdate.Add(topbar.menuSelection);
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
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        break;
                    case 'f':
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        break;
                    case 'w':
                        usingItem = topbar.menuSelection;
                        scene = "game";
                        displayStuff();
                        break;
                    case 'a':
                        scene = "craft";
                        updateRecipeMenu();
                        displayStuff();
                        break;
                    case 's':
                        scene = "game";
                        displayStuff();
                        break;
                    case 'h':
                        inventory[topbar.menuSelection].num = 0;
                        fixInventory();
                        usingItem = null;
                        topbar.menuSelection = Math.Min(topbar.menuSelection, menus["inv"].Length-1);
                        if (inventory[0].num < 1)
                        {
                            scene = "game";
                        }
                        displayStuff();
                        break;
                }
                break;
            case "craft":
                switch (ch)
                {
                    case 'w':
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        adjustCamera();
                        break;
                    case 's':
                        linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        adjustCamera();
                        break;
                    case 'x':
                        scene = "inv";
                        displayStuff();
                        break;
                    case 'z':
                        // craft process goes here (doit)
                        break;
                }
                break;
        }
        if (scene != "game")
        {
            topbar.menuSelection = Math.Clamp(topbar.menuSelection, 0, menus[scene].Length-1);
            linesToUpdate.Add(topbar.menuSelection);
            if (scene == "craft")
            {
                unnessaryFunctionForDecidingManualTips();
            }
            updateMenu();
        }
    }
    void unnessaryFunctionForDecidingManualTips()
    {
        if (scene == "game")
        {
            Tile curs = factory.giveMeTheTile(cursor.x, cursor.y);
            string info = factory.gd.autoTilePick(curs, 0);
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
            topbar.manualTip = false;
            if (linesToUpdate.Count > 0)
            {
                topbar.lastTipChange = time.Ticks;
                //topbar.tip = String.Format("{0}, {1}, {2}", topbar.menuSelection, topbar.menuScroll, topbar.menuScroll+Console.WindowHeight-3);
                topbar.tip = menus["craft_desc"][topbar.menuSelection];
            }
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
                topbar.menuScroll = 0;
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
                Console.WriteLine("An error happened with GameData: " + game.factory.gd.state);
            }
            game.bye();
        }
    }
}
