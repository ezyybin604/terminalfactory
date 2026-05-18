
using gameRunner;

namespace E604terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)
// https://github.com/ezyybin604/terminalfactory/

// todo:
/*
    - splitter core - machine
    - passthrough pipes (max 5 inbetween)
    - make adjustCamera not a disaster (extra low priority) (dont make it use weird while loops)
    - hope that all the machines are functional
    - super scale collection facility
    - dragon shedding
    - add machine loose forming
    - make worldgen features more datadriven (at least for main worldgen)
    - make laser purifer lens consume chance
    - renember that changeProg exists (fit some functions that dont use it where it should be used)
    - conceider serializing nextUpdateTick
    - finish dragon.putscale
    - add cursor to prompt scene
    - add sfx to certain actions in graphics
    - add back button to prompt screen
    - also add smooth scrolling with world rendering
    - once smooth scrolling is done, add connected grass
    - Move machine logic into its own file
    - add recipes to splitter (round robin, split, forced round robin, etc, default: round robin)
    - add a "backbuffer" to sending tiles to CoreGraphics
    - give world renderer more info of tile position
    - make a "manual" with help topics and stuff
*/

public class Game
{
    // Scenes: game,end,inv,pause,craft,custom,intro,prompt
    public string scene = "custom";
    public Point scroll = new Point();
    public Point cursor = new Point(2,2);
    public Factory factory = new Factory
    {
        gd = new GameData("data/gamedata") // add data path to gdm main path too maybe
    };
    public static List<ConsoleKeyInfo> readkeylog = new List<ConsoleKeyInfo>();
    DateTime time = DateTime.Now;
    public TopBar topbar = new TopBar();
    public Dictionary<string, string[]> menus = new Dictionary<string, string[]>();
    Inventory inventory = new Inventory();
    public FileManagement gdm = new FileManagement("terminalfactory");
    string currentTipText = "";
    public string specialMode = "";
    string[] splashes = Array.Empty<string>(); // yoinking yet another concept from minecraft
    int? usingItem = null;
    int timer = 0;
    public required TileConsole cusc;
    public required WindowHandler windowHandler;
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
    public void sendAction(string act)
    {
        if (factory.tutorial != null)
        {
            factory.tutorial.acts.Add(act);
        }
    }
    void generateNeeded()
    {
        Point boardSize = cusc.getWindowSize(WindowSizes.BOARD);
        int w = (int)Math.Ceiling((double)(boardSize.x/Factory.chunkSize));
        int h = (int)Math.Ceiling((double)(boardSize.y/Factory.chunkSize));
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
        string text = "Skip introduction?";
        if (cusc.runnerType == "sdl")
        {
            cusc.changeMode("prompt");
            cusc.writeText(text);
        } else
        {
            text += " (Please read i beg) (Press key: y/n):";
            Console.Write(text);
        }
        Console.WriteLine("This introduction is being DELETED. go away and pretend it doesnt exist");
        Console.ReadLine();
    }
    void initStuff()
    {
        if (cusc.runnerType == "sdl")
        {
            factory.exposeDisplay = true;
        }
        topbar.tips.Add("game", [
            "Use WASD to move",
            "Press P to pause",
            "Press K to break/collect",
            "Press O to place",
            "Press I to open inventory",
            "Press L to view tile contents/view recipe",
            "Press J to exhange contents with tile",
            "Also press J to select machine recipe",
            "Press I to show machine progress"
        ]);
        topbar.tips.Add("pause", [
            "Use WS to change selection",
            "Press Z to select"
        ]);
        topbar.tips.Add("inv", [
            "Use WS to change selection",
            "Press Z to select (use item)",
            "Press A to enter crafts menu",
            "Press X to go back",
            "Press H to delete item",
            "If your inventory is empty, you will exit.",
            "When an item is deleted, selection will be too."
        ]);
        topbar.tips.Add("craft", [
            "Use WS to change selection",
            "Press Z to select",
            "Press X to go back"
        ]);
        topbar.tips.Add("custom", topbar.tips["pause"]);
        topbar.tips.Add("prompt", ["Press ENTER to continue"]);
        topbar.tips.Add("end", ["now go away"]);

        menus.Add("pause_info", [
            "Placeholder Information"
        ]);
        menus.Add("pause", [
            "Resume Game|resume",
            "Save|save",
            "Delete Savefile (DANGER)|delete",
            "Quit|quit"
        ]);
        menus.Add("custom", []);
        menus.Add("customopt", []);
        menus.Add("nohighlight", ["prompt", "intro"]);
        
        menus.Add("intro", []);
        topbar.tips.Add("intro", [" "]);
        if (specialMode == "demo")
        {
            if (factory.savefile == "")
            {
                menus["pause"] = [
                    "Resume Game|resume",
                    "Restart|quit"
                ];
            } else
            {
                menus["pause"] = [
                    "Resume Game|resume",
                    "Save Game|save",
                    "Delete Savefile|delete",
                    "Restart|quit"
                ];
            }
        } else if (specialMode == "tutorial")
        {
            menus["pause"] = [
                "Resume Game|resume",
                "Exit tutorial|quit"
            ];
            factory.emptyTile = new Tile(" ");
            factory.tutorial = new FTutorial{fact = factory};
            if (cusc.runnerType == "sdl")
            {
                factory.tutorial.tutorialkey = "SDLTutorialMsg";
                factory.tutorial.size = new Point(30, 15);
            }
            factory.tutorial.updateAction();
            cursor = factory.tutorial.center;
        }
        menus.Add("end", ["Why are you reading this exactly?"]);
        menus.Add("inv", new string[Inventory.Length]); // dynamic menu, based off inventory variable

        menus.Add("craft_raw", []);
        menus.Add("craft", []);
        menus.Add("craft_desc", []);

        for (int i=0;i<Inventory.Length && !inventory.hasData;i++)
        {
            inventory.data[i] = new Slot();
        }
        inventory.gd = factory.gd;
        factory.inventory = inventory;
    }
    string menuprefix = "- ";
    void displayMenuLine(int i)
    {
        bool hasHeader = topbar.header.Length > 0;
        int headeroff = 0;
        if (hasHeader)
        {
            headeroff = topbar.header.Length+1;
        }
        int headeridx = i+headeroff;
        // header end
        bool lowerScreen = false;
        int lowerIndex = 0;
        if (menus.ContainsKey(scene + "_info"))
        { // what does this even do?????????????????????
            lowerIndex = Console.WindowHeight-i-3;
            lowerScreen = Console.WindowHeight-menus[scene].Length < i+1 && Console.WindowHeight > menus[scene].Length + 1 + menus[scene + "_info"].Length;
        }
        if ((i > menus[scene].Length-1 && !lowerScreen) || (!hasHeader && i < 0))
        {
            return;
        }
        string si = "";
        if (lowerScreen)
        {
            si = menus[scene + "_info"][lowerIndex].Split("|")[0];
        } else if (headeridx < topbar.header.Length && hasHeader)
        {
            si = topbar.header[headeridx];
        } else if (headeridx == topbar.header.Length && hasHeader) {} else
        {
            si = menus[scene][i].Split("|")[0];
        }
        // print header content here
        int gi = i+2-topbar.menuScroll+headeroff;
        Console.ResetColor();
        if (gi > Console.WindowHeight-1 || gi < 2)
        {
            return;
        }
        Console.SetCursorPosition(0, gi);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.ForegroundColor = ConsoleColor.DarkRed;
        bool nohighlight = menus["nohighlight"].Contains(scene);
        if (nohighlight)
        {
            menuprefix = "";
        } else
        {
            menuprefix = "- ";
        }
        if (i == topbar.menuSelection && !nohighlight)
        {
            factory.invertColors();
            si = menuprefix + si;
        }
        Console.SetCursorPosition(0, gi);
        Console.Write(si.Substring(0, Math.Min(si.Length, Console.WindowWidth)));
    }
    void menuDisplay()
    {
        if (scene == "inv")
        {
            inventory.fix();
            inventory.updateMenu(factory);
        } // prev for loop menus[scene].Length-topbar.menuScroll
        if (scene == "pause")
        {
            menus["pause_info"] = factory.dragon.getInfo();
        }
        if (cusc.runnerType == "sdl") return;
        int menuLength = Math.Min(Console.WindowHeight-2, menus[scene].Length);
        if (menus.ContainsKey(scene + "_info"))
        {
            menuLength = Console.WindowHeight-2;
        }
        for (int i=-topbar.header.Length-1;i<menuLength;i++)
        {
            displayMenuLine(i+topbar.menuScroll);
        }
    }
    void updateMenu()
    {
        if (scene == "inv") menus["inv"] = inventory.invmenud;
        if (cusc.runnerType == "sdl") return;
        for (int i=-topbar.header.Length-1;i<menus[scene].Length;i++)
        {
            if (factory.linesToUpdate.Contains(i))
            {
                displayMenuLine(i);
            }
        }
        factory.linesToUpdate.Clear();
    }
    void selectItemMenuCustom(string opt)
    {
        switch (opt)
        {
            case "nameprompt":
                if (gdm.savefileExists("defualtfsave"))
                { // new
                    scene = "prompt";
                    menus["prompt"] = [""];
                    topbar.returnScene = "intro";
                    displayStuff();
                } else
                { // default (only if have def)
                    factory.savefile = "defualtfsave";
                    scene = "intro";
                }
                break;
            case "listworld":
                TileConsole.startSceneSelect(this, "worldlist"); // BACKWARDSSS
                break;
            case "opt":
                TileConsole.startSceneSelect(this, "options");
                break;
            case "back":
                TileConsole.startSceneSelect(this, "title");
                break;
            case "selectworl":
                factory.savefile = menus["custom"][topbar.menuSelection];
                TileConsole.startSceneSelect(this, "title");
                break;
            case "start":
                loadData();
                topbar.header = [];
                scene = "game";
                displayStuff();
                break;
            case "quit":
                scene = "end";
                break;
        }
    }
    void selectItemMenu()
    {
        string[] st = menus[scene][topbar.menuSelection].Split("|");
        string world = Path.Join(gdm.worldFolder, factory.savefile);
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
            if (cusc.runnerType == "sdl") return;
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
            if (tipTextCopy.Length > cusc.getWindowSize(WindowSizes.TEXT).x && cusc.runnerType == "console")
            {
                Console.Write(tipTextCopy.Substring(0, cusc.getWindowSize(WindowSizes.TEXT).x));
            } else
            {
                Console.Write(tipTextCopy);
            }
            currentTipText = tipText;
        }
    }
    void lookThisOneIsJustToDrawTheBigLine()
    {
        if (cusc.runnerType == "sdl") return;
        Console.ResetColor();
        if (Console.WindowHeight > 1)
        {
            Console.SetCursorPosition(0, 1);
            if (factory.tutorial != null && factory.tutorial.curact == "continue" && Console.WindowWidth > 3)
            {
                Console.WriteLine("~(C)" + new string('~', Console.WindowWidth-4));
            } else
            {
                Console.WriteLine(new string('~', Console.WindowWidth));
            }
        }
    }
    public void displayStuff()
    {
        factory.linesToUpdate.Clear();
        Console.ResetColor();
        cusc.Clear();
        updateBar(true);
        lookThisOneIsJustToDrawTheBigLine();
        if (scene != "game")
        {
            menuDisplay();
            lookThisOneIsJustToDrawTheBigLine();
            return;
        }
        for (int i=0;i<cusc.getWindowSize(WindowSizes.BOARD).y;i++)
        {
            factory.displayLine(i+scroll.y, cursor, scroll, cusc);
        }
    }
    void updateScreen()
    {
        int grh = cusc.getWindowSize(WindowSizes.BOARD).y;
        for (int i=0;i<grh;i++)
        {
            if (factory.linesToUpdate.Contains(i+scroll.y))
            {
                if (cusc.runnerType == "sdl") Console.SetCursorPosition(0, i+2);
                factory.displayLine(i+scroll.y, cursor, scroll, cusc);
            }
        }
        factory.linesToUpdate.Clear();
    }
    void forceUpdateAll()
    { // only for game
        if (cusc.runnerType == "sdl") return;
        int scrollnum = scroll.y;
        if (scene != "game")
        {
            scrollnum = topbar.menuScroll;
            if (topbar.header.Length > 0) scrollnum-=topbar.header.Length+1;
        }
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            factory.linesToUpdate.Add(i+scrollnum);
        }
    }
    void adjustCamera()
    {
        if (specialMode == "tutorial" && factory.tutorial != null && scene == "game")
        {
            scroll = factory.tutorial.boxpos.getTransform(factory.tutorial.size.getDivide(2)).getTransform(cusc.getWindowSize(WindowSizes.BOARD).getTransform(0, -4).getDivide(-2));
            generateNeeded();
            return;
        }
        if (scene != "game")
        {
            int prevScroll = topbar.menuScroll;
            if (cusc.runnerType != "sdl")
            {
                while (!(topbar.menuScroll <= topbar.menuSelection))
                {
                    topbar.menuScroll--;
                }
                while (!(topbar.menuSelection <= topbar.menuScroll+Console.WindowHeight-3))
                {
                    topbar.menuScroll++;
                }
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
            string[] ing = factory.gd.getSplit(catg, ent[i]);
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
        if (cusc.runnerType == "console" && key.Modifiers.HasFlag(ConsoleModifiers.Control))
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
                        sendAction("movepress");
                        if (specialMode == "tutorial" && factory.giveMeTheTile(cursor).type == 't') cursor.x++;
                        factory.linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 's':
                        cursor.y++;
                        sendAction("movepress");
                        if (specialMode == "tutorial" && factory.giveMeTheTile(cursor).type == 't') cursor.y--;
                        factory.linesToUpdate.Add(cursor.y);
                        factory.linesToUpdate.Add(cursor.y-1);
                        adjustCamera();
                        break;
                    case 'w':
                        cursor.y--;
                        sendAction("movepress");
                        if (specialMode == "tutorial" && factory.giveMeTheTile(cursor).type == 't') cursor.y++;
                        factory.linesToUpdate.Add(cursor.y);
                        factory.linesToUpdate.Add(cursor.y+1);
                        adjustCamera();
                        break;
                    case 'd':
                        cursor.x++;
                        sendAction("movepress");
                        if (specialMode == "tutorial" && factory.giveMeTheTile(cursor).type == 't') cursor.x--;
                        factory.linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 'p':
                        scene = "pause";
                        topbar.menuSelection = 0;
                        forceDisplay = true;
                        break;
                    case 'i':
                        if (tic.type == 'M')
                        {
                            // show machine progress
                            sendAction("showmacprog");
                            Machine mac = factory.machines[cursor];
                            string ntip = "/dNo recipe running";
                            if (mac.runningRecipe) ntip = mac.startedRecipe.ToString() + " / " + factory.gd.getFromKey("machineTime", tic.subtype);
                            topbar.changeTip(ntip, 2, 3000);
                        } else
                        {
                            topbar.menuSelection = 0;
                            inventory.fix();
                            inventory.updateMenu(factory);
                            if (inventory.data[0].num > 0)
                            {
                                sendAction("inventoryopen");
                                scene = "inv";
                                forceDisplay = true;
                            } else
                            {
                                topbar.changeTip("/dInventory empty.", 1, 3000);
                            }
                        }
                        break;
                    case 'k':
                        if (factory.tutorial != null && !factory.tutorial.worldModify)
                        {
                            return;
                        }
                        TileBroken br = factory.breakTile(cursor);
                        if (br.hasItem)
                        {
                            if (inventory.addItem(br.item))
                            {
                                topbar.changeTip("/gx" + inventory.getItemAmount(br.item.item).ToString() + " " + factory.getItemName(br.item.item), 2, 4000);
                                sendAction("breaktile-" + factory.inventory.latestGiven);
                                if (tic.type != 'i')
                                {
                                    factory.linesToUpdate.Add(cursor.y);
                                }
                            }
                        }
                        break;
                    case 'o':
                        // place
                        if (factory.tutorial != null && !factory.tutorial.worldModify)
                        {
                            return;
                        }
                        if (factory.placeTile(usingItem, cursor))
                        {
                            sendAction("placetile");
                            factory.linesToUpdate.Add(cursor.y);
                        }
                        break;
                    case 'l': // view contents
                        if (tic.amount > 0 && factory.gd.getFromKey("tags", "containerTile").Contains(tic.type))
                        {
                            sendAction("viewContents");
                            string tip = "x" + tic.amount.ToString() + " " + factory.getItemName(tic.subtype);
                            topbar.changeTip(tip, 2, 3000);
                        } else if (tic.type == 'M')
                        {
                            sendAction("viewRecipe");
                            string tip = "Recipe: " + factory.getItemName(factory.machines[cursor].selectedRecipe);
                            topbar.changeTip(tip, 2, 3000);
                        } else if (factory.gd.getFromKey("tags", "engrec").Contains(tic.type))
                        {
                            topbar.changeTip("Energy: " + tic.amount, 2, 1000);
                        }
                        break;
                    case 'j': // exchange / select recipe for machine
                        if (factory.gd.getFromKey("tags", "containerTile").Contains(tic.type))
                        {
                            if (tic.amount > 0)
                            {
                                // extract
                                if (!factory.gd.getSplit("tags", "fluids").Contains(tic.subtype) && inventory.addItem(new Slot(tic.subtype, tic.amount)))
                                {
                                    tic.amount = 0;
                                    factory.setTile(cursor, tic);
                                }
                            } else if (tic.type == '+' && usingItem != null)
                            {
                                sendAction("jdeposit");
                                // deposit
                                tic.amount = inventory.data[(int)usingItem].num;
                                tic.subtype = inventory.data[(int)usingItem].item;
                                factory.setTile(cursor, tic);
                                inventory.data[(int)usingItem].num = 0;
                                inventory.fix();
                                usingItem = null;
                            }
                        } else if (tic.type == 'M' && factory.gd.getSplit("tags", "macWrecipe").Contains(tic.subtype))
                        {
                            scene = "craft";
                            topbar.returnScene = "game";
                            topbar.menuSelection = 0;
                            updateRecipeMenu(tic.subtype + "Recipes");
                            forceDisplay = true;
                        }
                        break;
                    case 'c':
                        if (factory.tutorial != null && factory.tutorial.ticksSinceLast > 10)
                        {
                            sendAction("continue");
                        }
                        break;
                }
                break;
            case "pause": case "custom":
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
                        if (scene == "pause")
                        {
                            selectItemMenu();
                        } else
                        {
                            selectItemMenuCustom(menus["customopt"][topbar.menuSelection]);
                        }
                        break;
                    default:
                        break;
                }
                break;
            case "inv":
                switch (ch)
                {
                    case 'w':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection--;
                        sendAction("inventorymove");
                        break;
                    case 's':
                        factory.linesToUpdate.Add(topbar.menuSelection);
                        topbar.menuSelection++;
                        sendAction("inventorymove");
                        break;
                    case 'z':
                        sendAction("invselect-" + inventory.data[topbar.menuSelection].item);
                        usingItem = topbar.menuSelection;
                        scene = "game";
                        forceDisplay = true;
                        break;
                    case 'a':
                        sendAction("invcraft");
                        scene = "craft";
                        topbar.returnScene = "inv";
                        topbar.menuSelection = 0;
                        updateRecipeMenu();
                        forceDisplay = true;
                        break;
                    case 'x':
                        scene = "game";
                        forceDisplay = true;
                        sendAction("backgen");
                        break;
                    case 'h':
                        if (factory.tutorial != null && !factory.tutorial.canDelete)
                        {
                            return;
                        }
                        sendAction("invdel");
                        inventory.data[topbar.menuSelection].num = 0;
                        inventory.fix();
                        inventory.updateMenu(factory);
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
                        inventory.updateMenu(factory);
                        sendAction("backgen");
                        break;
                    case 'z':
                        // craft process goes here (doit)
                        string result = menus["craft_raw"][topbar.menuSelection];
                        if (topbar.returnScene == "game")
                        {
                            factory.machines[cursor].selectedRecipe = result;
                            scene = topbar.returnScene;
                            forceDisplay = true;
                            sendAction("craftdoselect");
                        } else if (topbar.returnScene == "inv")
                        {
                            Slot[] recipe = inventory.getRecipe("craftingRecipe", result);
                            if (inventory.verifyRecipe(recipe) || specialMode == "creative")
                            {
                                if (inventory.addItem(new Slot(result)))
                                {
                                    sendAction("craftdo");
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
            case "prompt":
                string s = menus["prompt"][0];
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (s.Length > 0) menus["prompt"][0] = SDLTools.RemoveChars(s, s.Length-1);
                } else if (key.Key == ConsoleKey.Enter)
                {
                    if (gdm.savefileExists(s) || s.Length == 0)
                    {
                        topbar.changeTip("/dSavefile already exists", 2, 4000);
                    } else
                    {
                        factory.savefile = menus["prompt"][0];
                        scene = topbar.returnScene;
                        if (scene == "intro")
                        {
                            TileConsole.startSceneSelect(this, "intro");
                        }
                    }
                } else
                {
                    menus["prompt"][0] += ch;
                }
                break;
            case "intro":
                Game tutr = new Game
                { // Why does c# want me to do this isnt "simplified" what
                    specialMode = "tutorial",
                    cusc = cusc,
                    windowHandler = windowHandler,
                    splashes = splashes,
                    scene = "game"
                };
                tutr.initStuff();
                readkeylog.RemoveAt(0);
                cusc.theGame = tutr;
                tutr.runTheGameIg();
                cusc.theGame = this;
                readkeylog.Add(key);
                scene = "game";
                displayStuff();
                break;
        }
        if (scene != "game")
        {
            topbar.menuSelection = Math.Clamp(topbar.menuSelection, 0, Math.Max(0, menus[scene].Length-1));
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
                if (!(factory.tutorial != null && factory.tutorial.curact == "backgen"))
                {
                    topbar.changeTip(1, tpr);
                }
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
            int itip = (int)Math.Round(Factory.generateRange(0, topbar.tips[scene].Length-1));
            string ftip = topbar.tips[scene][itip]; // final tip
            int overpri = 0;
            if (factory.tutorial != null)
            {
                ftip = factory.tutorial.updateTip();
            }
            topbar.changeTip(overpri, ftip, true);
            topbar.lastTipChange = time.Ticks;
        }
    }
    void printToMenu(string s)
    {
        string[] spl = s.Split("\n");
        List<string> from = menus[scene].ToList();
        foreach (string tp in spl)
        {
            from.Add(tp.Replace("\r", ""));
        }
        menus[scene] = from.ToArray();
    }
    void updateScene()
    {
        // gets called when scene switch
        if (cusc.runnerType == "sdl")
        {
            WindowHandler.sceneUpdated = true;
        }
        switch (scene)
        {
            case "intro":
                topbar.header = [];
                displayStuff();
                Thread.Sleep(1000);
                printToMenu(@"Your town was taken by a DRAGON.
A group survived, (that includes you)
But it is hungry.
None of you know how to feed a creature of such scale,
But you have the knowledge of a empty field nearby
that could be the perfect spot for a factory to
pump out continous food and water for the dragon.
");
                displayStuff();
                Thread.Sleep(5000);
                string press = "Press any key";
                if (cusc.runnerType == "sdl")
                {
                    press = "Click";
                }
                printToMenu(String.Format(@"
Nobody follows, so to keep secrecy while you travel.

({0} to start)", press));
                displayStuff();
                Thread.Sleep(200);
                readkeylog.Clear();
                break;
            case "prompt":
                menus["prompt"] = [""];
                if (topbar.returnScene == "intro")
                {
                    topbar.header = ["Name the world"];
                }
                forceUpdateAll();
                break;
            default:
                break;
        }
    }
    public bool acceptFrame = true;
    public void runTheGameIg()
    {
        if (specialMode != "tutorial")
        {
            hi();
            TileConsole.startSceneSelect(this, "title");
        }
        Point windowSizePrevious = cusc.getWindowSize(WindowSizes.WINDOW);
        Point windowSize;
        Point previousCamera = new Point(-1, 0);
        string previousScene = scene;
        generateNeeded();
        adjustCamera();
        generateNeeded();
        displayStuff();
        while (scene != "end")
        {
            while (!acceptFrame)
            {
                Thread.Sleep(10);
            }
            if (previousScene != scene)
            {
                previousScene = scene;
                topbar.lastTipChange = DateTime.MinValue.Ticks;
                topbar.menuScroll = 0;
                updateScene();
            }
            if (!previousCamera.Equals(scroll))
            {
                generateNeeded();
                forceUpdateAll();
                previousCamera = scroll;
            }
            time = DateTime.Now;
            unnessaryFunctionForDecidingTips();
            windowSize = cusc.getWindowSize(WindowSizes.WINDOW);
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
            } else
            {
                updateMenu();
            }
            if (cusc.runnerType != "sdl" && specialMode == "tutorial" && factory.tutorial != null && (factory.tutorial.size.x >= Console.WindowWidth || factory.tutorial.size.y+3 > Console.WindowHeight))
            {
                topbar.changeTip("/dWindow size too small!", 10, 4500, true);
            }
            updateBar();
            while (readkeylog.Count > 0)
            {
                useInput(readkeylog[0]);
                readkeylog.RemoveAt(0);
            }
            timer++;
            timer = Math.Max(0, timer);
            if (factory.tutorial != null)
            {
                string prev = factory.tutorial.updateTip();
                if (factory.tutorial.tickTutorial())
                {
                    lookThisOneIsJustToDrawTheBigLine();
                }
                if (prev != factory.tutorial.updateTip())
                {
                    topbar.lastTipChange = DateTime.MinValue.Ticks;
                }
            }
            if (cusc.runnerType == "sdl") acceptFrame = false;
            Thread.Sleep(50);
        }
        if (specialMode == "")
        {
            bye();
        }
    }
    void bye()
    {
        if (gdm.savefileExists(factory.savefile))
        {
            gdm.saveOption("defaultsave", factory.savefile);
        }
        if (cusc.runnerType != "sdl")
        {
            Console.ResetColor();
            cusc.Clear();
            Console.WriteLine("bye");
            Thread.Sleep(1000);
        }
        Environment.Exit(0);
    }
    public void hi()
    {
        string splash = splashes[Factory.generateIntRange(1, splashes.Length)-1].Replace('\r', '\0');
        cusc.setSplash(splash, "0.1");
    }
    public void Start()
    {
        Directory.CreateDirectory(gdm.worldFolder);
        if (File.Exists("data/splashes"))
        {
            splashes = File.ReadAllText("data/splashes").Split("\n");
        } else
        {
            TileConsole.Error("splashes is missing oh no");
        }
        try
        {
            if (cusc.runnerType != "sdl")
            {
                Console.Title = "terminalfactory";
            }
        }
        catch (PlatformNotSupportedException)
        {
            TileConsole.Log("no");
        }
        if (factory.gd.state == "done")
        {
            if (specialMode == "demo" && cusc.runnerType != "sdl")
            {
                while (true)
                {
                    Game game = new Game
                    { // again, why
                        specialMode = "demo",
                        splashes = File.ReadAllText("data/splashes").Split("\n"),
                        cusc = cusc,
                        windowHandler = windowHandler
                    };
                    game.hi();
                    if (cusc.choice("Would you like to play on a savefile? (y/n)", this, "hi"))
                    {
                        string? savef = null;
                        while (savef == null)
                        {
                            cusc.Clear(); // change to ask tileconsole for savefile prompt
                            TileConsole.Log("What is the savefile name?");
                            savef = Console.ReadLine();
                            if (savef == "" || (savef != null && (savef.Contains("/") || savef.Contains("\\"))))
                            {
                                savef = null;
                            }
                        }
                        game.factory.savefile = "demosave-" + savef;
                        if (!game.gdm.savefileExists(game.factory.savefile))
                        {
                            game.introduction();
                        }
                    } else
                    {
                        if (cusc.runnerType == "console") TileConsole.Log();
                        game.factory.savefile = "";
                        game.introduction();
                    }
                    game.initStuff();
                    cusc.startGame(this);
                }
            } else
            {
                initStuff();
                cusc.startGame(this);
            }
        } else
        {
            TileConsole.Error("\nAn error happened with GameData: " + factory.gd.state);
        }
        bye();
    }
}
