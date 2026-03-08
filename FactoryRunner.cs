
namespace terminalfactory;

/*
Tile types:
f: finite resource
i: infinite resource
b: bush
'`': grass/empty
m: Machine block (T1, T2, T3)
*: energy port
+: add/input
-: get/output
p: pipe (subtype determines direction)
~: cable
]: trough
@: world interactor
M: machine
h: handle this pipe, make it face towards any adjacent things
o: building stone
*/

// Chunk: Tile[x][y] data;

public class Machine
{
    public bool isFormed = false;
    public List<Point> inputs = new List<Point>();
    public Point? output = null;
    public Point? worldInteractor = null;
    public Point? energyPort = null;
    public bool runningRecipe = false;
    public string selectedRecipe = ""; // no recipe
    public long startedRecipe = DateTime.Now.Ticks;
}

class Factory // factory data / big verbose stuff related to factory
{
    public GameData gd = new GameData("gamedata");
    Dictionary<string, ConsoleColor> strColor = new Dictionary<string, ConsoleColor>();
    char[] natrualTiles = ['f', 'i', ']', 'b', 'o'];
    Random rng = new Random();
    public string savefile = "defualtfsave";
    public const int chunkSize = 16;
    public const int regionArea = 8;
    public Inventory inventory = new Inventory();
    // [x][y]
    public Dictionary<int, Dictionary<int, Tile[][]>> world = new Dictionary<int, Dictionary<int, Tile[][]>>();
    public Dictionary<Point, Machine> machines = new Dictionary<Point, Machine>();
    public HashSet<int> linesToUpdate = new HashSet<int>(); // i didnt renember what the data type was called so i had to google it
    Point[] machineArea = [
        new Point(1, -1),
        new Point(1, 1),
        new Point(-1, 1),
        new Point(-1, -1),
        new Point(-1, 0),
        new Point(1, 0),
        new Point(0, -1),
        new Point(0, 1)
    ];

    private Point getRegion(Point point)
    {
        return new Point(
            ((int)Math.Floor((double)(point.x/regionArea)))*3,
            ((int)Math.Floor((double)(point.y/regionArea)))*3
        );
    }
    public int parseInt(string inp)
    { // i didnt want to copy it and have the same thing twice
        int res;
        if (!int.TryParse(inp, out res))
        {
            return 0;
        }
        return res;
    }
    public List<Point> getRegions()
    {
        HashSet<Point> regions = new HashSet<Point>();
        int[] yposb = new int[world.Count];
        int[] xposb;
        world.Keys.CopyTo(yposb, 0);
        for (int x=0;x<yposb.Length;x++)
        {
            xposb = new int[world[x].Keys.Count];
            world[x].Keys.CopyTo(xposb, 0);
            for (int y=0;y<yposb.Length;y++)
            {
                regions.Add(getRegion(new Point(x, y)));
            }
        }
        return regions.ToList();
    }
    public double generateRange(double min, double max)
    {
        return (rng.NextDouble()*(max-min))+min;
    }
    private double floorRound(double d, double max)
    {
        double dd = Math.Floor(d);
        if (max == dd)
        {
            return dd-1;
        }
        return dd;
    }
    private int generateIntRange(int min, int max)
    {
        return (int)floorRound(generateRange(min, max), max);
    }
    private int weightedRandom(int[] weights)
    {
        if (weights.Length < 1)
        {
            return 0;
        }
        int sum = weights.AsParallel().Sum();
        int[] results = new int[sum];
        int amount = 0;
        int x = 0;
        for (int i=0;i<sum;i++)
        {
            results[i] = x;
            amount++;
            if (weights[x] < amount+1)
            {
                amount = 0;
                x++;
            }
        }
        int rnd = generateIntRange(0, sum-1);
        return results[rnd];
    }
    private List<Point> pointShapeGenerator(int size, string shape, int amount=0) // look idk what this actually looks like
    {
        List<Point> result = new List<Point>();
        switch (shape) {
            case "diamond":
                int sizetotal = size+1+size;
                for (int x=0;x<sizetotal;x++)
                {
                    int mid = Math.Abs(x-size); // closer to middle=bigger
                    result.Add(new Point(x, size));
                    for (int y=mid;y<size;y++)
                    {
                        result.Add(new Point(x, y));
                        result.Add(new Point(x, sizetotal-y-1));
                    }
                }
                break;
            case "scatter":
                for (int i=0;i<amount;i++)
                {
                    result.Add(new Point(generateIntRange(0, size), generateIntRange(0, size)));
                }
                break;
        }
        return result;
    }
    public void generateChunk(int x, int y)
    {
        Tile[][] chunk = new Tile[chunkSize][];
        // Generate chunk data herre
        for (int i=0;i<chunkSize;i++)
        {
            chunk[i] = new Tile[chunkSize];
            for (int o=0;o<chunkSize;o++)
            {
                chunk[i][o] = new Tile();
                chunk[i][o].type = '`';
                if (x == 0 && i == 0)
                {
                    chunk[i][o].type = ']';
                }
            }
        }
        for (int cnt=0;cnt<2;cnt++)
        {
            int fx = (int)(rng.NextDouble()*10);
            int fy = (int)(rng.NextDouble()*10);
            List<Point> shape;
            Tile copy = new Tile();
            switch (weightedRandom([10, 20, 15, 30]))
            {
                case 0:
                    // Water springs (diamond shape, 2-4 radius determining tier of water 2=pond, 4=ocean 3=mountain spring)
                    int water = weightedRandom([40, 10, 125])+2;
                    shape = pointShapeGenerator(water, "diamond");
                    copy.type = 'i';
                    copy.prog = 0;
                    if (water == 2)
                    {
                        copy.subtype = "water2";
                    } else if (water == 3)
                    {
                        copy.subtype = "water3";
                    } else if (water == 4)
                    {
                        copy.subtype = "water1";
                    }
                    break;
                case 1:
                    // Bush Group (scatter 3-8 randomly in 4x4 area)
                    shape = pointShapeGenerator(4, "scatter", generateIntRange(3, 5));
                    copy.type = 'b';
                    copy.prog = 10; // regen over time
                    copy.subtype = "fr" + (weightedRandom([256, 128, 32, 16, 8])+1).ToString();
                    break;
                case 2:
                    // Ore/Rock Cluster (scatter 5-12 randomly in 3x3 area)
                    shape = pointShapeGenerator(5, "scatter", generateIntRange(8, 20));
                    string[] sbt = ["diamond", "iron", "copper", "carbon", "stone", "bone", "oil", "sand", "coal"];
                    copy.subtype = sbt[weightedRandom([5, 20, 20, 5, 25, 5, 10, 25, 25])];
                    copy.type = 'i';
                    if (copy.subtype == "stone" || copy.subtype == "bone" ||  copy.subtype == "sand")
                    {
                        copy.type = 'f';
                        copy.prog = generateIntRange(4, 128);
                        if (copy.subtype == "sand")
                        {
                            copy.prog = 4096;
                        }
                    }
                    if (copy.subtype == "sand")
                    {
                        shape = pointShapeGenerator(1, "diamond");
                    }
                    if (copy.subtype == "oil")
                    {
                        shape = pointShapeGenerator(3, "diamond");
                    }
                    break;
                default:
                    copy.type = ' ';
                    shape = [new Point(0, 0)];
                    break;
            }
            for (int i=0;i<shape.Count;i++)
            {
                Point pointGo = new Point(shape[i].x + fx, shape[i].y + fy);
                if (pointGo.x < chunkSize && pointGo.y < chunkSize)
                {
                    if (chunk[pointGo.x][pointGo.y].type != ']')
                    {
                        chunk[pointGo.x][pointGo.y] = copy;
                    }
                }
            }
        }

        if (!world.Keys.Contains(x))
        {
            world.Add(x, new Dictionary<int, Tile[][]>());
        }
        if (!world[x].Keys.Contains(y))
        {
            world[x].Add(y, chunk);
        }
    }
    public void placeChunk(Point position, Tile[][] data)
    {
        if (!world.Keys.Contains(position.x))
        {
            world[position.x] = new Dictionary<int, Tile[][]>();
        }
        if (world[position.x].Keys.Contains(position.y))
        { // i dont know if these do the same thing someone tell me
            world[position.x][position.y] = data;
        } else
        {
            world[position.x].Add(position.y, data);
        }
    }
    public void initFactory()
    {
        strColor.Add("blue", ConsoleColor.Cyan);
        strColor.Add("darkgray", ConsoleColor.DarkGray);
        strColor.Add("cyan", ConsoleColor.Cyan);
        strColor.Add("darkyellow", ConsoleColor.DarkYellow);
        strColor.Add("gray", ConsoleColor.Gray);
        strColor.Add("yellow", ConsoleColor.Yellow);
        strColor.Add("green", ConsoleColor.Green);
        strColor.Add("white", ConsoleColor.White);
        strColor.Add("red", ConsoleColor.Red);
        strColor.Add("magenta", ConsoleColor.Magenta);
        strColor.Add("darkgreen", ConsoleColor.DarkGreen);
        strColor.Add("darkcyan", ConsoleColor.DarkCyan);
    }
    public void invertColors()
    { // hheheheeheh
        ConsoleColor bg = Console.ForegroundColor;
        ConsoleColor fg = Console.BackgroundColor;
        Console.BackgroundColor = bg;
        Console.ForegroundColor = fg;
    }
    public Tile giveMeTheTile(int x, int y)
    {
        Point chunk = new Point((int)Math.Floor((double)(x/chunkSize)), (int)Math.Floor((double)(y/chunkSize)));
        Point index = new Point(x%chunkSize, y%chunkSize);
        if (y < 0)
        {
            index.y+=chunkSize;
            index.y%=chunkSize;
        }
        return world[chunk.x][chunk.y][index.x][index.y];
    }
    public Tile giveMeTheTile(Point point)
    {
        return giveMeTheTile(point.x, point.y);
    }
    public void setTile(int x, int y, Tile tl)
    {
        Point chunk = new Point((int)Math.Floor((double)(x/chunkSize)), (int)Math.Floor((double)(y/chunkSize)));
        Point index = new Point(x%chunkSize, y%chunkSize);
        if (y < 0)
        {
            index.y+=chunkSize;
            index.y%=chunkSize;
        }
        world[chunk.x][chunk.y][index.x][index.y] = tl;
    }
    public void setTile(Point pt, Tile tl)
    {
        setTile(pt.x, pt.y, tl);
    }
    private int getArrow(Point dir) // ^v<>
    {
        if (dir.x == 0)
        { // vertical
            if (dir.y == -1)
            {
                return 1;
            } else if (dir.y == 1)
            {
                return 2;
            }
        } else if (dir.y == 0)
        { // horizontal
            if (dir.x == -1)
            {
                return 3;
            } else if (dir.x == 1)
            {
                return 4;
            }
        }
        return 0;
    }
    public void displayLine(int y, Point cursor, Point scroll)
    {
        string[] lineResult;
        int idx = 0;
        bool continueText = false;
        bool color = false;
        bool colorNow;
        string currentColor = "";
        string prevColor;
        int invertedColor = 0;
        lineResult = new string[(Console.WindowWidth*2)+2];
        for (int x=0;x<Console.WindowWidth;x++)
        {
            colorNow = color;
            prevColor = currentColor;
            Point cur = new Point(x+scroll.x, y);
            Tile t = giveMeTheTile(cur);
            char addChar = t.type;
            string state = "";
            if (t.subtype == null)
            {
                t.subtype = "";
            }
            string subtc = gd.autoTilePick(t, 0, "tileRecolors");// add gd.autotilepick
            if (subtc != "" && natrualTiles.Contains(t.type)) // for natrually generating stuff only
            {
                if (continueText && !color)
                {
                    idx++;
                }
                currentColor = subtc;
                state = "natrualColor";
                color = true;
                colorNow = false;
            }
            if (t.type == ']')
            {
                if (continueText && !color)
                {
                    idx++;
                }
                currentColor = "gray";
                color = true;
                colorNow = false;
            }
            if ("@+-*".Contains(t.type))
            {
                if (continueText && !color)
                {
                    idx++;
                }
                currentColor = "cyan";
                color = true;
                colorNow = false;
                if (t.type == '+' || t.type == '-')
                {
                    addChar = "?v^><"[t.prog];
                }
            }
            if (t.type == 'M')
            {
                if (!machines[cur].isFormed)
                {
                    if (continueText && !color)
                    {
                        idx++;
                    }
                    currentColor = "red";
                    color = true;
                    colorNow = false;
                }
                addChar = t.subtype.ToUpper()[0];
            }
            if (t.type == 'm')
            {
                string macs = "+|-";
                currentColor = gd.getFromKey("machineColor", t.subtype);
                color = true;
                colorNow = false;
                addChar = macs[t.prog];
            }
            bool colorLoop = false;
            if (color && !colorNow && (prevColor == "" || prevColor != currentColor))
            {
                if (lineResult[idx] != null)
                {
                    idx++;
                }
                colorLoop = true;
                lineResult[idx] = "/" + currentColor;
                idx++;
            }
            if (y == cursor.y && x+scroll.x == cursor.x)
            {
                if (lineResult[idx] != null)
                {
                    idx++;
                }
                if (!colorLoop && currentColor != "" && color && !colorNow)
                {
                    lineResult[idx] = "/" + currentColor;
                    idx++;
                }
                invertedColor = 2;
                lineResult[idx] = "/invert";
                //idx++;
            }
            if (state == "natrualColor")
            {
                if (t.subtype.Contains("water"))
                {
                    addChar = '░';
                } else if (t.subtype == "stone")
                {
                    addChar = 's';
                } else if (t.subtype == "bone")
                {
                    addChar = '3';
                } else if (t.subtype == "oil")
                {
                    addChar = 'o';
                } else if (t.subtype == "coal")
                {
                    addChar = 'c';
                }
            }
            if (t.type == 'o')
            {
                addChar = 'b';
            }
            if (colorNow && color)
            {
                color = false;
                currentColor = "";
                idx++;
            }
            if (lineResult[idx] == null || invertedColor > 0)
            {
                if (lineResult[idx] != null)
                {
                    idx++;
                }
                if (invertedColor > 0)
                {
                    invertedColor--;
                }
                lineResult[idx] = addChar.ToString();
            } else
            {
                lineResult[idx] += addChar.ToString();
            }
            continueText = true;
            invertedColor = Math.Max(invertedColor, 0);
            if (invertedColor > 0)
            {
                currentColor = "";
            }
        }
        if (lineResult[idx] != null)
        {
            idx++;
        }
        lineResult[idx] = "/end";
        Console.ResetColor();
        //Console.WriteLine(String.Join(",", lineResult)); // displayLine:debug
        Console.ForegroundColor = ConsoleColor.Green;
        for (int o=0;lineResult[o] != "/end";o++)
        {
            string yes = lineResult[o];
            if (yes[0] == '/' && yes.Length > 1)
            {
                yes = yes.Substring(1);
                if (yes == "invert")
                {
                    invertColors();
                } else
                {
                    Console.ForegroundColor = strColor[yes];
                }
            } else
            {
                Console.Write(lineResult[o]);
                //Thread.Sleep(100);
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
            }
        }
    }
    public bool breakTile(Point cursor, TopBar topbar)
    {
        Tile curs = giveMeTheTile(cursor);
        string info = gd.autoTilePick(curs, 1, "blockinfo");
        if (info != "")
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
                if (inventory.addItem(new Slot(info)))
                {
                    setTile(cursor, curs);
                    topbar.changeTip("/gx" + inventory.getItemAmount(info).ToString() + " " + gd.getFromKey("itemNames", info), 2, 4000);
                    return true;
                }
            }
        }
        return false;
    }
    public bool placeTile(int? usingItem, Point cursor)
    {
        if (usingItem != null && cursor.x > 1)
        {
            int invlen = inventory.fix();
            Tile tile = new Tile();
            Slot slot = inventory.data[(int)usingItem];
            string info = gd.getFromKey("itemToBlock", slot.item);
            if (slot.num > 0 && info != "")
            {
                string[] infol = info.Split(".");
                tile.type = infol[0][0];
                if (infol.Length == 2)
                {
                    tile.subtype = infol[1];
                }
                inventory.data[(int)usingItem].num--;
                setTile(cursor, tile);
                if (tile.type == 'M')
                {
                    machines.Add(cursor, new Machine());
                    if (gd.getFromKey("tags", "chooseADefault").Split(",").Contains(tile.subtype))
                    {
                        machines[cursor].selectedRecipe = gd.getKeys(tile.subtype + "Recipes")[0];
                    }
                }
                return true;
            }
        }
        return false;
    }
    void changeProg(Point pt, int nprg)
    {
        Tile tile = giveMeTheTile(pt);
        int prevp = tile.prog;
        tile.prog = nprg;
        if (tile.prog != prevp)
        {
            linesToUpdate.Add(pt.y);
            setTile(pt, tile);
        }
    }
    public void updateMachines()
    {
        Dictionary<Point, Machine>.KeyCollection why = machines.Keys;
        Point[] macp = new Point[why.Count];
        why.CopyTo(macp, 0);
        for (int i=0;i<macp.Length;i++)
        {
            updateMachine(macp[i]);
        }
    }
    private void startMachine(Machine mac, string subt)
    {// just found out about utcnow but i decided i dont care about that
        int totalEnergyConsumed = parseInt(gd.getFromKey("generatorOutput", subt));
        if (mac.energyPort != null && giveMeTheTile((Point)mac.energyPort).amount > totalEnergyConsumed)
        mac.runningRecipe = true;
        mac.startedRecipe = DateTime.Now.Ticks;
    }
    void updateMachine(Point mac)
    {
        /*
            Stuff to 𝓱𝓪𝓷𝓭𝓵𝓮
            bool isFormed = false;
            List<Point> inputs = new List<Point>();
            Point? output = null;
            Point? worldInteractor = null;
            Point? energyPort = null;
            bool runningRecipe = false
        */
        Machine mach = machines[mac];
        Tile core = giveMeTheTile(mac);
        if (core.type != 'M')
        {
            mach.isFormed = false;
            machines.Remove(mac);
            return;
        }
        mach.isFormed = true;
        mach.inputs.Clear();
        mach.output = null;
        mach.worldInteractor = null;
        mach.energyPort = null;
        for (int i=0;i<machineArea.Length;i++)
        {
            Point pt = new Point(mac);
            pt.transform(machineArea[i]);
            Tile tile = giveMeTheTile(pt);
            if (i < 4)
            {
                changeProg(pt, 0);
                if (tile.type != 'm')
                {
                    mach.isFormed = false;
                }
            } else if (i > 3)
            {
                switch (tile.type)
                {
                    case '+':
                        mach.inputs.Add(pt);
                        changeProg(pt, getArrow(machineArea[i]));
                        break;
                    case '-':
                        if (mach.output == null)
                        {
                            mach.output = pt;
                        } else
                        {
                            mach.isFormed = false;
                        }
                        changeProg(pt, getArrow(machineArea[i].getReverse()));
                        break;
                    case '@':
                        if (mach.worldInteractor == null)
                        {
                            mach.worldInteractor = pt;
                        } else
                        {
                            mach.isFormed = false;
                        }
                        break;
                    case '*':
                        if (mach.energyPort == null)
                        {
                            mach.energyPort = pt;
                        } else
                        {
                            mach.isFormed = false;
                        }
                        break;
                    case 'm':
                        if (i > 5)
                        {
                            changeProg(pt, 2);
                        } else
                        {
                            changeProg(pt, 1);
                        }
                        break;
                    default:
                        mach.isFormed = false;
                        break;
                }
            }
        }
        if (mach.output == null || mach.inputs.Count == 0)
        {
            mach.isFormed = false;
        }
        bool machRecipes = gd.getFromKey("tags", "macWrecipe").Split(",").Contains(core.subtype);
        string rid = core.subtype + "Recipes";
        if (!mach.isFormed && mach.runningRecipe)
        {
            mach.runningRecipe = false;
        } else if (mach.isFormed && !mach.runningRecipe && (mach.selectedRecipe != "" || !machRecipes))
        {
            if (machRecipes)
            {
                Slot[] inputSlots = new Slot[mach.inputs.Count];
                bool slotsFull = true; // all inputs have to be full to run recipe
                for (int i=0;i<inputSlots.Length;i++)
                {
                    Tile tile = giveMeTheTile(mach.inputs[i]);
                    if (tile.amount > 0)
                    {
                        inputSlots[i].num = tile.amount;
                        inputSlots[i].item = tile.subtype;
                    } else
                    {
                        slotsFull = false;
                    }
                    if (mach.output == null)
                    {
                        slotsFull = false;
                    } else
                    {
                        tile = giveMeTheTile((Point)mach.output);
                        if (tile.amount > 0) // has nothing in output or no run recipe
                        {
                            slotsFull = false;
                        }
                    }
                }
                if (slotsFull)
                {
                    Inventory inputInventory = new Inventory();
                    inputInventory.data = inputSlots;
                    // verify and turn on recipe
                    if (inputInventory.verifyRecipe(rid, mach.selectedRecipe))
                    {
                        startMachine(mach, core.subtype);
                    }
                }
            } else
            {
                // list of non-recipe based machines:
                // cgen,ogen = generators
                // pump,mine = in-world extractors
                // niem,stor = storage
                // comp = dynamic input
                // mixr = dynamic output
                switch (core.subtype)
                {
                    case "cgen": case "ogen":
                        string consumed = gd.getFromKey("generatorMaterial", core.subtype);
                        if (mach.inputs.Count == 1 && mach.output != null)
                        {
                            Tile tile = giveMeTheTile(mach.inputs[0]);
                            Tile outputTile = giveMeTheTile((Point)mach.output);
                            if (tile.amount > 0 && tile.subtype == consumed && outputTile.amount < 1)
                            {
                                startMachine(mach, core.subtype);
                            }
                        }
                        break;
                }
            }
        }
        machines[mac] = mach; // is this redundant? (i see the changes without this)
    }
    public void tickMachines()
    {
        Point[] macp = new Point[machines.Keys.Count];
        machines.Keys.CopyTo(macp, 0);
        for (int i=0;i<macp.Length;i++)
        {
            Machine mac = machines[macp[i]];
            if (mac.runningRecipe)
            {
                // running recipe so it can start a chain
            }
        }
    }
}

public class Inventory
{
    public bool hasData = false;
    public GameData gd = new GameData();
    public const int Length = 150;
    public const int MaxPerSlot = 999;
    public Slot[] data = new Slot[Length];
    List<string> invlist = new List<string>();
    public int getItemAmount(string item)
    {
        fix();
        int amount = 0;
        for (int i=0;i<data.Length;i++)
        {
            if (data[i].num > 0)
            {
                if (data[i].item == item)
                {
                    amount += data[i].num;
                }
            } else
            {
                return amount;
            }
        }
        return amount;
    }
    public int fix()
    {
        Slot[] invcopy = data;
        int x=0;
        for (int i=0;i<data.Length;i++)
        {
            if (invcopy[i].num > 0)
            {
                if (x != i) { data[x] = invcopy[i].Copy(); }
                if (i > x)
                {
                    data[i].num = 0;
                }
                x++;
            }
        }
        return x; // returns "length" of inventory
    }
    public string[] getMenu()
    {
        invlist.Clear();
        for (int i=0;i<data.Length;i++)
        {
            Slot slot = data[i];
            if (slot.num > 0)
            {
                invlist.Add(String.Format("x{0}, {1}", slot.num, gd.getFromKey("itemNames", slot.item)));
            }
        }
        string[] menuout = new string[invlist.Count];
        for (int i=0;i<invlist.Count;i++)
        {
            menuout[i] = invlist[i];
        }
        return menuout;
    }
    public Slot[] getRecipe(string catg, string result)
    {
        List<Slot> recipe = new List<Slot>();
        string[] ing = gd.getFromKey(catg, result).Split(",");
        int numitem = 0;
        for (int i=0;i<ing.Length;i++)
        {
            string cur = ing[i];
            if (cur[0] == 'x')
            {
                cur = cur[1..];
                int.TryParse(cur, out numitem);
            } else
            {
                recipe.Add(new Slot(cur, numitem));
            }
        }
        Slot[] copied = new Slot[recipe.Count];
        recipe.CopyTo(copied);
        return copied;
    }
    public bool verifyRecipe(string catg, string result)
    {
        return verifyRecipe(getRecipe(catg, result));
    }
    public bool verifyRecipe(Slot[] recipe)
    {
        Dictionary<string, int> itemNumbers = new Dictionary<string, int>();
        foreach (Slot slot in recipe) // slot slot slot slot slot
        {
            itemNumbers.Add(slot.item, 0);
        }
        foreach (Slot slot in data)
        {
            if (itemNumbers.Keys.Contains(slot.item))
            {
                itemNumbers[slot.item] += slot.num;
            }
        }
        foreach (Slot slot in recipe)
        {
            if (slot.num > itemNumbers[slot.item])
            {
                return false;
            }
        }
        return true;
    }
    public void removeItems(Slot[] items)
    {
        Dictionary<string, int> itm = new Dictionary<string, int>();
        for (int i=0;i<items.Length;i++)
        {
            itm.Add(items[i].item, items[i].num);
        }
        foreach (Slot slot in data)
        {
            string item = slot.item;
            if (itm.Keys.Contains(item))
            {
                if (slot.num >= itm[slot.item])
                {
                    int ridNumItem = Math.Min(slot.num, itm[slot.item]);
                    slot.num -= ridNumItem; // doing this w/o the help of i/for is uncomfterbole
                    itm[slot.item] -= ridNumItem;
                }
            }
        }
    }
    int getFreeSlot(Slot item)
    {
        int i=0;
        while (!(data[i].item == "" || data[i].item == item.item)
            || data[i].num >= MaxPerSlot
            && i < Inventory.Length)
        {
            i++;
        }
        return i;
    }
    public bool addItem(Slot item)
    {
        int owedItem = item.num;
        while (owedItem > 0)
        {
            int slot = getFreeSlot(item);
            if (slot < Inventory.Length)
            { // in range of array
                int ridNumItem = Math.Min(MaxPerSlot-data[slot].num, owedItem);
                owedItem -= ridNumItem;
                data[slot].num += ridNumItem;
                if (data[slot].item == "")
                {
                    data[slot].item = item.item;
                }
            } else if (owedItem == item.num)
            {
                return false;
            } else
            {
                return true; // sry if this happens your items get voided
            }
        }
        return true;
    }
}