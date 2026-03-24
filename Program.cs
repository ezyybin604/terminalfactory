
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)
// https://github.com/ezyybin604/terminalfactory/

// todo:
/*
    - world ticking (resume at FactoryRunner.cs -> @ticktile)
    - splitter of items when i do logistics
    - passthrough pipes too
    - make the machines do recipes/io stuff (make mach out push items)
    - make pipes move items
    - tile updates, this tick and next tick updates
    - make adjustCamera not a disaster (extra low priority) (dont make it use weird while loops)
    - better tutorial that explains things more
    - show machine progress in inspect
*/

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
    string specialMode = "";
    string[] splashes = new string[0]; // yoinking yet another concept from minecraft
    int? usingItem = null;
    public void loadData()
    {
        factory.world = new Dictionary<int, Dictionary<int, Tile[][]>>();
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
        w+=3;
        h+=3;
        int sx = (int)Math.Floor((double)(scroll.x/Factory.chunkSize));
        int sy = (int)Math.Floor((double)(scroll.y/Factory.chunkSize));
        for (int x=-1;x<w;x++)
        {
            for (int y=-1;y<h;y++)
            {
                factory.generateChunk(Math.Max(x+sx, 0), y+sy);
            }
        }
    }
    public void introduction()
    {
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
    void initStuff()
    {
        factory.initFactory();
        topbar.tips.Add("game", [
            "Use WASD to move",
            "Press P to pause",
            "Press K to break/collect",
            "Press O to place",
            "Press I to open inventory",
            "Press L to view tile contents/view recipe",
            "Press J to exhange contents with tile",
            "Also press M to select machine recipe"
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
            "Delete Savefile (DANGER)|delete",
            "Quit (go away)|quit"
        ]);
        if (specialMode == "demo")
        {
            menus["pause"] = [
                "Resume Game|resume",
                "Restart|quit"
            ];
        }
        menus.Add("end", ["Why are you reading this exactly?"]);
        menus.Add("inv", new string[Inventory.Length]); // dynamic menu, based off inventory variable

        menus.Add("craft_raw", []);
        menus.Add("craft", []);
        menus.Add("craft_desc", []);

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
        if (i > menus[scene].Length-1)
        {
            return;
        }
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
            menus["inv"] = inventory.getMenu(factory);
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
        string world = Path.Join(FileManagement.worldFolder, factory.savefile);
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
                        Directory.CreateDirectory(world);
                        gdm.SaveStuff(factory, cursor, scroll);
                        break;
                    case "quit":
                        scene = "end";
                        break;
                    case "delete":
                        if (topbar.areyousure < 100)
                        {
                            topbar.areyousure++;
                            topbar.changeTip(1, String.Format("/dAre you REALLY sure? ({0} left until deletion)", 100-topbar.areyousure));
                        } else
                        {
                            if (!Directory.Exists(world)) return;
                            if (Directory.EnumerateDirectories(world).Count() > 0) return;
                            string[] fnames = Directory.EnumerateFiles(world).ToArray();
                            for (int i=0;i<fnames.Length;i++)
                            {
                                File.Delete(fnames[i]);
                            }
                            Directory.Delete(world);
                        }
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
    public void forceUpdateAll()
    { // only for game
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            factory.linesToUpdate.Add(i+scroll.y);
        }
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
                forceUpdateAll();
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
                    if (numitem == 0)
                    {
                        res.Add(factory.getItemName(cur));
                    } else
                    {
                        res.Add("x" + numitem.ToString() + " " + factory.getItemName(cur));
                    }
                }
            }
            menus["craft_raw"][i] = ent[i];
            menus["craft"][i] = factory.getItemName(ent[i]);
            menus["craft_desc"][i] = String.Join(", ", res);
        }
    }
    void useInput(ConsoleKeyInfo key)
    {
        bool forceDisplay = false; // (i have no idea what im doing send help)
        char ch = key.KeyChar.ToString().ToLower()[0];
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
                        menus["inv"] = inventory.getMenu(factory);
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
                            string tip = "x" + tic.amount.ToString() + " " + factory.getItemName(tic.subtype);
                            topbar.changeTip(tip, 2, 3000);
                        } else if (tic.type == 'M')
                        {
                            string tip = "Recipe: " + factory.getItemName(factory.machines[cursor].selectedRecipe);
                            topbar.changeTip(tip, 2, 3000);
                        }
                        break;
                    case 'j': // exchange / select recipe for machine
                        if (factory.gd.getFromKey("tags", "containerTile").Contains(tic.type))
                        {
                            if (tic.amount > 0)
                            {
                                // extract
                                if (!factory.gd.getFromKey("tags", "fluids").Split(",").Contains(tic.subtype) && inventory.addItem(new Slot(tic.subtype, tic.amount)))
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
                        } else if (tic.type == 'M' && factory.gd.getFromKey("tags", "macWrecipe").Split(",").Contains(tic.subtype))
                        {
                            scene = "craft";
                            topbar.returnScene = "game";
                            topbar.menuSelection = 0;
                            updateRecipeMenu(tic.subtype + "Recipes");
                            forceDisplay = true;
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
                        topbar.areyousure = 0;
                        break;
                    case 's':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        topbar.areyousure = 0;
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
                        topbar.returnScene = "inv";
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
                        menus["inv"] = inventory.getMenu(factory);
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
                        scene = topbar.returnScene;
                        forceDisplay = true;
                        break;
                    case 'z':
                        // craft process goes here (doit)
                        string result = menus["craft_raw"][topbar.menuSelection];
                        if (topbar.returnScene == "game")
                        {
                            factory.machines[cursor].selectedRecipe = result;
                            scene = topbar.returnScene;
                            forceDisplay = true;
                        } else if (topbar.returnScene == "inv")
                        {
                            Slot[] recipe = inventory.getRecipe("craftingRecipe", result);
                            if (inventory.verifyRecipe(recipe) || specialMode == "creative")
                            {
                                if (inventory.addItem(new Slot(result)))
                                {
                                    topbar.changeTip(String.Format("/gx{0} {1}", inventory.getItemAmount(result), factory.getItemName(result)), 2, 4500);
                                    if (specialMode != "creative")
                                    {
                                        inventory.removeItems(recipe);
                                    }
                                }
                            } else
                            {
                                topbar.changeTip(1, "/dNo. You can't do it. stop");
                            }
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
        } else if (scene == "craft" && topbar.returnScene == "inv")
        {
            topbar.manualTip = false;
            if (factory.linesToUpdate.Count > 0)
            {
                string tpr = menus["craft_desc"][topbar.menuSelection];;
                if (!inventory.verifyRecipe("craftingRecipe", menus["craft_raw"][topbar.menuSelection]) && specialMode != "creative")
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
        while (scene != "end")
        {
            input = Console.ReadKey(true);
            readkeylog.Add(input);
            if (specialMode == "demo" && scene == "pause" && input.KeyChar == 'z' && topbar.menuSelection == 1)
            {
                while (scene != "end")
                {
                    Console.WriteLine("Waiting to restart");
                    Thread.Sleep(100);
                }
            }
        }
    }
    void runTheGameIg()
    {
        Point windowSizePrevious = new Point(Console.WindowWidth, Console.WindowHeight);
        Point windowSize = new Point();
        Point previousCamera = new Point(-1, 0);
        string previousScene = scene;
        int timer = 0;
        generateNeeded();
        displayStuff();
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
                forceUpdateAll();
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
                if (timer%50 == 0)
                {
                    factory.updateMachines();
                }
                factory.tickStuff();
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
            timer = Math.Max(0, timer);
            Thread.Sleep(50);
        }
        if (specialMode != "demo")
        {
            bye();
        }
    }
    void bye()
    {
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("bye");
        gdm.saveDefualt(factory);
        Thread.Sleep(1000);
        Environment.Exit(0);
    }
    void hi()
    {
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("TERMINALFACTORY");
        string splash = splashes[factory.generateIntRange(1, splashes.Length)-1].Replace('\r', '\0');
        Console.Write("\"" + splash + "\"");
        Console.SetCursorPosition(0, Console.WindowHeight-1);
        Console.Write("v0.1");
        Console.SetCursorPosition(0, 3);
    }
    public static void Main()
    {
        Game game = new Game();
        Directory.CreateDirectory(FileManagement.worldFolder);
        if (File.Exists("data/splashes"))
        {
            game.splashes = File.ReadAllText("data/splashes").Split("\n");
        } else
        {
            Console.WriteLine("splashes is missing oh no");
            Console.ReadLine();
            Environment.Exit(0);
        }
        game.hi();
        bool sf = game.gdm.savefileExists(game.factory);
        if (!sf && game.gdm.isDefualt())
        {
            game.factory.savefile = game.gdm.getDefualt();
            sf = game.gdm.savefileExists(game.factory);
        }
        bool alsf = true;
        if (sf || Directory.GetDirectories(FileManagement.worldFolder).Length > 0)
        {
            if (!sf)
            {
                game.factory.savefile = Directory.GetDirectories(FileManagement.worldFolder)[0].Split("\\").Last();
                sf = Directory.Exists(Path.Join(FileManagement.worldFolder, game.factory.savefile));
                if (!sf)
                {
                    Console.WriteLine("Oh no something REALLY went wrong with save " + game.factory.savefile + "\n\nalso Yeah no. I'm not making a edge case for this one. Go away.");
                    Console.ReadLine();
                    return;
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
                    string[] saves = Directory.GetDirectories(FileManagement.worldFolder);
                    Console.Clear();
                    for (int i=0;i<saves.Length;i++)
                    {
                        Console.WriteLine(saves[i].Split("\\").Last());
                    }
                    Console.WriteLine("(Press ENTER to exit)");
                    Console.ReadLine();
                    inp = null;
                }
                if (inp == "creative" || inp == "demo")
                {
                    Console.WriteLine("creative: All crafts in crafting menu are free");
                    Console.WriteLine("demo: No saving, there is a restart button instead of exiting.");
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
                alsf = Directory.Exists(Path.Join(FileManagement.worldFolder, game.factory.savefile));
            }
            if (alsf)
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
        try
        {
            Console.Title = "terminalfactory";
        }
        catch (PlatformNotSupportedException)
        {
            Console.Write("no");
        }
        if (game.factory.gd.state == "done")
        {
            if (game.specialMode == "demo")
            {
                while (true)
                {
                    game = new Game();
                    game.specialMode = "demo";
                    game.splashes = File.ReadAllText("data/splashes").Split("\n");
                    game.hi();
                    game.introduction();
                    game.initStuff();
                    if (game.gameThread != null)
                    {
                        game.gameThread.Start();
                        game.inputSutff();
                    }
                }
            } else
            {
                game.initStuff();
                if (game.gameThread != null)
                {
                    game.gameThread.Start();
                    game.inputSutff();
                }
            }
        } else
        {
            Console.WriteLine("\nAn error happened with GameData: " + game.factory.gd.state);
            Console.ReadLine();
        }
        game.bye();
    }
}
