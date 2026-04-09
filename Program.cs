
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)
// https://github.com/ezyybin604/terminalfactory/

// todo:
/*
    - splitter core machine
    - passthrough pipes
    - make adjustCamera not a disaster (extra low priority) (dont make it use weird while loops)
    - hope that all the machines are functional
    - super scale collection facility
    - dragon shedding
    - add machine loose forming
    - make worldgen features more datadriven (at least for main worldgen)
    - make laser purifer lens consume chance
    - renember that changeProg exists (fit some functions that dont use it where it should be used)
    - conceider serializing nextUpdateTick
    - change populatedChunks to unpopulatedChunks (and hope it works now that its probably finished)
    - finish dragon.putscale
*/

class Game
{
    // Scenes: game,end,inv,pause,craft
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
    string[] splashes = Array.Empty<string>(); // yoinking yet another concept from minecraft
    int? usingItem = null;
    int timer = 0;
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
    public bool choice(string prompt, string extra="")
    {
        char res = '\0';
        while (res == '\0')
        {
            Console.Clear();
            if (extra == "hi")
            {
                hi();
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
    public void introduction()
    {
        Console.Write("Skip intro? (Please read i beg) (Press key: y/n):");
        if (!(Console.ReadKey().KeyChar == 'y'))
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
        string extrat = "";
        if (specialMode == "demo") extrat = "(if noone sees this ever im actually gonna crash out im 5 seconds away from loosing my marbles and throwing a microwave at them)";
        extrat += " \n(y/n):";
        if (choice("\nDo you want a tutorial? (warning: important)" + extrat))
        {
            // do tutorial stuff
            Game tutr = new Game
            { // Why does c# want me to do this isnt "simplified" what
                specialMode = "tutorial"
            };
            tutr.initStuff();
            tutr.startGame();
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
        topbar.tips.Add("end", ["now go away"]);

        menus.Add("pause_info", [
            "Placeholder Information"
        ]);
        menus.Add("pause", [
            "Resume Game|resume",
            "Save|save",
            "Delete Savefile (DANGER)|delete",
            "Quit (go away)|quit"
        ]);
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
            factory.tutorial = new FTutorial(factory);
            cursor = factory.tutorial.center;
        }
        menus.Add("end", ["Why are you reading this exactly?"]);
        menus.Add("inv", new string[Inventory.Length]); // dynamic menu, based off inventory variable

        menus.Add("craft_raw", []);
        menus.Add("craft", []);
        menus.Add("craft_desc", []);

        gameThread = new Thread(runTheGameIg)
        {
            Name = "Game Logic"
        };

        for (int i=0;i<Inventory.Length && !inventory.hasData;i++)
        {
            inventory.data[i] = new Slot();
        }
        inventory.gd = factory.gd;
        factory.inventory = inventory;
    }
    void displayMenuLine(int i)
    {
        bool lowerScreen = false;
        int lowerIndex = 0;
        if (menus.ContainsKey(scene + "_info"))
        {
            lowerIndex = Console.WindowHeight-i-3;
            lowerScreen = Console.WindowHeight-menus[scene].Length < i+1 && Console.WindowHeight > menus[scene].Length + 1 + menus[scene + "_info"].Length;
        }
        if (i > menus[scene].Length-1 && !lowerScreen)
        {
            return;
        }
        string[] si;
        if (lowerScreen)
        {
            si = menus[scene + "_info"][lowerIndex].Split("|");
        } else
        {
            si = menus[scene][i].Split("|");
        }
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
        Console.Write(si[0].Substring(0, Math.Min(si[0].Length, Console.WindowWidth)));
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
        int menuLength = Math.Min(Console.WindowHeight-2, menus[scene].Length);
        if (menus.ContainsKey(scene + "_info"))
        {
            menuLength = Console.WindowHeight-2;
        }
        for (int i=0;i<menuLength;i++)
        {
            displayMenuLine(i+topbar.menuScroll);
        }
    }
    void updateMenu()
    {
        if (scene == "inv") menus["inv"] = inventory.invmenud;
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
    void forceUpdateAll()
    { // only for game
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            factory.linesToUpdate.Add(i+scroll.y);
        }
    }
    void adjustCamera()
    {
        if (specialMode == "tutorial" && factory.tutorial != null)
        {
            scroll = factory.tutorial.boxpos.getTransform(factory.tutorial.size.getDivide(2)).getTransform(Point.getWindowSize().getTransform(0, -4).getDivide(-2));
            generateNeeded();
            return;
        }
        if (scene != "game")
        {
            int prevScroll = topbar.menuScroll;
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
                        if (factory.breakTile(cursor, topbar))
                        {
                            sendAction("breaktile-" + factory.inventory.latestGiven);
                            if (tic.type != 'i')
                            {
                                factory.linesToUpdate.Add(cursor.y);
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
            int itip = (int)Math.Round(Factory.generateRange(0, topbar.tips[scene].Length-1));
            string ftip = topbar.tips[scene][itip]; // final tip
            if (factory.tutorial != null)
            {
                ftip = factory.tutorial.updateTip();
            }
            topbar.changeTip(0, ftip, true);
            topbar.lastTipChange = time.Ticks;
        }
    }
    void inputSutff() // dont try and merge this with the main function (runthegameig) it wont end well
    {
        string[] instantExitModes = ["demo", "tutorial"];
        ConsoleKeyInfo input;
        while (scene != "end")
        {
            input = Console.ReadKey(true);
            readkeylog.Add(input);
            if (instantExitModes.Contains(specialMode) && scene == "pause" && input.KeyChar == 'z' && menus["pause"][topbar.menuSelection].Split("|")[1] == "quit")
            {
                while (gameThread != null && gameThread.IsAlive)
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
        adjustCamera();
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
            if (specialMode == "tutorial" && factory.tutorial != null && (factory.tutorial.size.x >= Console.WindowWidth || factory.tutorial.size.y+3 > Console.WindowHeight))
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
            Thread.Sleep(50);
        }
        if (specialMode == "")
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
    static void hi(string[] splashes)
    {
        Console.ResetColor();
        Console.Clear();
        Console.WriteLine("TERMINALFACTORY");
        string splash = splashes[Factory.generateIntRange(1, splashes.Length)-1].Replace('\r', '\0');
        Console.Write("\"" + splash + "\"");
        Console.SetCursorPosition(0, Console.WindowHeight-1);
        Console.Write("v0.1");
        Console.SetCursorPosition(0, 3);
    }
    void hi()
    {
        hi(splashes);
    }
    public void startGame()
    {
        if (gameThread != null)
        {
            gameThread.Start();
            inputSutff();
        }
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
        if (game.gdm.isDefualt())
        {
            game.factory.savefile = game.gdm.getDefualt();
            sf = game.gdm.savefileExists(game.factory);
        }
        bool alsf = true;
        if (sf || Directory.GetDirectories(FileManagement.worldFolder).Length > 0)
        {
            if (!sf)
            {
                game.factory.savefile = JPI.getFilename(Directory.GetDirectories(FileManagement.worldFolder)[0]);
                sf = game.gdm.savefileExists(game.factory);
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
                alsf = Directory.Exists(Path.Join(FileManagement.worldFolder, game.factory.savefile));
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
                    game = new Game
                    { // again, why
                        specialMode = "demo",
                        splashes = File.ReadAllText("data/splashes").Split("\n")
                    };
                    game.hi();
                    if (game.choice("Would you like to play on a savefile? (y/n)", "hi"))
                    {
                        string? savef = null;
                        while (savef == null)
                        {
                            Console.Clear();
                            Console.WriteLine("What is the savefile name?");
                            savef = Console.ReadLine();
                            if (savef == "" || (savef != null && (savef.Contains("/") || savef.Contains("\\"))))
                            {
                                savef = null;
                            }
                        }
                        game.factory.savefile = "demosave-" + savef;
                        if (!game.gdm.savefileExists(game.factory))
                        {
                            game.introduction();
                        }
                    } else
                    {
                        Console.WriteLine();
                        game.factory.savefile = "";
                        game.introduction();
                    }
                    game.initStuff();
                    game.startGame();
                }
            } else
            {
                if (game.specialMode == "tutorial") game.factory = new Factory();
                game.initStuff();
                game.startGame();
            }
        } else
        {
            Console.WriteLine("\nAn error happened with GameData: " + game.factory.gd.state);
            Console.ReadLine();
        }
        game.bye();
    }
}
