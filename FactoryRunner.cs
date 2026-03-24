
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
S: splitter (outputs to machine output)
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
    public int startedRecipe = 0;
    public int number = 0;
}

class Factory // factory data / big verbose stuff related to factory
{
    public GameData gd = new GameData("data/gamedata");
    Dictionary<string, ConsoleColor> strColor = new Dictionary<string, ConsoleColor>();
    char[] natrualTiles = ['f', 'i', ']', 'b', 'o'];
    Random rng = new Random();
    public string savefile = "defualtfsave";
    public const int chunkSize = 16;
    public const int regionArea = 16;
    public const int defaultItemLimit = 1000000; // max items in output/input/pipe (default number)
    public int energyInNetwork = 0;
    public const int maxEnergy = int.MaxValue-2000; // in network
    public Inventory inventory = new Inventory();
    // [x][y]
    public Dictionary<int, Dictionary<int, Tile[][]>> world = new Dictionary<int, Dictionary<int, Tile[][]>>();
    public Dictionary<Point, Machine> machines = new Dictionary<Point, Machine>();
    public HashSet<int> linesToUpdate = new HashSet<int>(); // i didnt renember what the data type was called so i had to google it
    public HashSet<Point> nextUpdateTick = new HashSet<Point>();
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
    Point[] arrowOffset = [ // v^><
        new Point(0, 1),
        new Point(0, -1),
        new Point(1, 0),
        new Point(-1, 0),
    ];
    private int getWaterValue(string item)
    {
        if (item.Length == 4)
        {
            if (item.Substring(0, 3) == "wat")
            {
                // raw food
                return 4*((int)Math.Pow(4, JPI.parseInt(item.Substring(3))-1));
            }
        }
        return 0;
    }
    private int getFoodValue(string item)
    {
        if (item.Length == 3)
        {
            if (item.Substring(0, 2) == "fr")
            {
                // raw food
                return (int)Math.Pow(JPI.parseInt(item.Substring(2)), 3);
            } else if (item.Substring(0, 2) == "sf")
            {
                return getFoodValue("fr " + item.Substring(2))*2;
            }
        } else if (item.Length > 4 && item[0] == '@' && item.Substring(0, 4) == "@mfr")
        {
            string[] data = item.Substring(4).Split(",");
            if (data.Length == 2)
            {
                return getFoodValue(data[0]) * getFoodValue(data[1]);
            }
        }
        return 0; // That's not food.. probably
    }
    public string getItemName(string item)
    {
        if (item.Length > 0 && item[0] == '@')
        {
            string itm = item.Substring(1);
            if (itm.Length > 3 && itm.Substring(0, 3) == "mfr") // mixed fruit
            {
                string[] data = itm.Substring(3).Split(",");
                string[] result = new string[data.Length];
                for (int i=0;i<data.Length;i++)
                {
                    result[i] =  getItemName(data[i]);
                }
                return "Mixed Fruit: " + String.Join(", ", result);
            }
        } else
        {
            string trans = gd.getFromKey("itemNames", item);
            if (trans != "")
            {
                return trans;
            }
        }
        return item;
    }
    private Point getRegion(Point point)
    {
        Point reg = new Point(
            (int)Math.Floor((double)(point.x/regionArea)),
            (int)Math.Floor((double)(point.y/regionArea))
        );
        if (point.y < 0)
        {
            reg.y--;
        }
        return reg.getMultiply(regionArea);
    }
    public List<Point> getRegions()
    {
        HashSet<Point> regions = new HashSet<Point>();
        int[] yposb = new int[world.Count];
        int[] xposb;
        world.Keys.CopyTo(yposb, 0);
        for (int x=0;x<yposb.Length;x++)
        {
            xposb = new int[world[yposb[x]].Keys.Count];
            world[x].Keys.CopyTo(xposb, 0);
            for (int y=0;y<xposb.Length;y++)
            {
                regions.Add(getRegion(new Point(yposb[x], xposb[y])));
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
    public int generateIntRange(int min, int max)
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
                    copy.subtype = sbt[weightedRandom([2, 20, 20, 5, 25, 5, 10, 25, 25])];
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
            chunk.y--;
        }
        return world[chunk.x][chunk.y][index.x][index.y];
    }
    public Tile giveMeTheTile(Point point)
    {
        return giveMeTheTile(point.x, point.y);
    }
    public Tile giveMeTheTile(Point? point)
    {
        if (point == null) return new Tile();
        return giveMeTheTile((Point)point);
    }
    public void setTile(int x, int y, Tile tl)
    {
        Point chunk = new Point((int)Math.Floor((double)(x/chunkSize)), (int)Math.Floor((double)(y/chunkSize)));
        Point index = new Point(x%chunkSize, y%chunkSize);
        if (y < 0)
        {
            index.y+=chunkSize;
            index.y%=chunkSize;
            chunk.y--;
        }
        world[chunk.x][chunk.y][index.x][index.y] = tl;
    }
    public void setTile(Point pt, Tile tl)
    {
        setTile(pt.x, pt.y, tl);
    }
    public void setTile(Point? pt, Tile tl)
    {
        if (pt == null) return;
        setTile((Point)pt, tl);
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
    private int getPipeDir(Point p)
    {
        // 0,1,2,3,4
        int direction = 0;
        Tile ctl = giveMeTheTile(p);
        for (int i=4;i<machineArea.Length;i++)
        {
            Point pt = p.getTransform(machineArea[i]);
            Tile tile = giveMeTheTile(pt);
            if (gd.getFromKey("tags", "pipeT").Contains(tile.type))
            {
                // towards
                direction = getArrow(machineArea[i].getReverse())-1;
            } else if (gd.getFromKey("tags", "pipeB").Contains(tile.type))
            {
                // backwards
                direction = getArrow(machineArea[i])+3; // if dir > 3 its backwards
            } else if (gd.getFromKey("tags", "pipeD").Contains(tile.type))
            {
                // dynamic tile.prog == ctl.prog
                if (tile.prog > 3)
                {
                    direction = getArrow(machineArea[i])+3;
                    tile.prog = direction;
                    setTile(pt, tile);
                    linesToUpdate.Add(pt.y);
                } else
                {
                    direction = getArrow(machineArea[i].getReverse())-1;
                }
            }
            direction = Math.Max(0, direction);
        }
        return direction;
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
            if ("@+-*p".Contains(t.type))
            {
                if (continueText && !color)
                {
                    idx++;
                }
                currentColor = "cyan";
                color = true;
                colorNow = false;
                string arrowmap = "?v^><";
                if (t.type == 'p')
                {
                    arrowmap = arrowmap.Substring(1, arrowmap.Length-1);
                    currentColor = "white";
                }
                if (t.type == '+' || t.type == '-' || t.type == 'p')
                {
                    addChar = arrowmap[t.prog%arrowmap.Length];
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
                    topbar.changeTip("/gx" + inventory.getItemAmount(info).ToString() + " " + getItemName(info), 2, 4000);
                    return true;
                }
            }
        }
        return false;
    }
    public bool placeTile(int? usingItem, Point cursor)
    {
        if (usingItem != null && cursor.x > 0)
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
                if (tile.type == 'h')
                {
                    tile.type = 'p';
                    tile.prog = getPipeDir(cursor);
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
    private bool startMachine(Machine mac, string subt)
    {
        int totalEnergyConsumed = JPI.parseInt(gd.getFromKey("energyConsume", subt)) * JPI.parseInt(gd.getFromKey("machineTime", subt));
        if (totalEnergyConsumed < 1 || (mac.energyPort != null && giveMeTheTile(mac.energyPort).amount >= totalEnergyConsumed))
        {
            mac.runningRecipe = true;
            mac.startedRecipe = 0;
            Tile ep = giveMeTheTile(mac.energyPort);
            ep.amount -= totalEnergyConsumed;
            setTile(mac.energyPort, ep);
            return true;
        }
        return false;
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
        bool wasFormed = mach.isFormed;
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
                            mach.worldInteractor = mac.getTransform(machineArea[i].getMultiply(2));
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
        if (core.subtype != "niem" && (mach.output == null || mach.inputs.Count == 0))
        {
            mach.isFormed = false;
        }
        if (mach.isFormed != wasFormed)
        {
            linesToUpdate.Add(mac.y);
        }
        bool machRecipes = gd.getFromKey("tags", "macWrecipe").Split(",").Contains(core.subtype);
        string rid = core.subtype + "Recipes";
        if (!mach.isFormed && mach.runningRecipe)
        {
            mach.runningRecipe = false;
        } else if (mach.isFormed && !mach.runningRecipe && (mach.selectedRecipe != "" || !machRecipes))
        {
            if (machRecipes && core.subtype != "niem")
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
                        tile = giveMeTheTile(mach.output);
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
                    Slot[] recipe = inputInventory.getRecipe(rid, mach.selectedRecipe);
                    if (inputInventory.verifyRecipe(recipe))
                    {
                        if (!mach.runningRecipe && startMachine(mach, core.subtype)) // has enough energy?
                        {
                            inputInventory.removeItems(recipe);
                            // restore inventory to inputs
                            for (int i=0;i<mach.inputs.Count;i++)
                            {
                                Tile intile = giveMeTheTile(mach.inputs[i]);
                                intile.subtype = inputInventory.data[i].item;
                                intile.amount = inputInventory.data[i].num;
                                setTile(mach.inputs[i], intile);
                            }
                        }
                    }
                }
            } else
            {
                // list of non-recipe based machines: (* = done)
                // *cgen,ogen = generators
                // pump,mine = in-world extractors
                // niem,stor = storage
                // comp = dynamic input
                // mixr = dynamic output
                // *everything else (handled ^^^^ up there)
                switch (core.subtype)
                {
                    case "cgen": case "ogen":
                        string consumed = gd.getFromKey("generatorMaterial", core.subtype);
                        if (!mach.runningRecipe && mach.inputs.Count == 1 && mach.output != null)
                        {
                            Tile tile = giveMeTheTile(mach.inputs[0]);
                            Tile outputTile = giveMeTheTile(mach.output);
                            if (tile.amount > 0 && tile.subtype == consumed && outputTile.amount < 1)
                            {
                                startMachine(mach, core.subtype); // no energy requirement :)
                                tile.amount--;
                                setTile(mach.inputs[0], tile);
                            }
                        }
                        break;
                    case "pump": case "mine":
                        string cat = "fluidToItem";
                        if (core.subtype == "mine")
                        {
                            cat = "tileToItem";
                        }
                        if (mach.output != null && mach.worldInteractor != null && !mach.runningRecipe)
                        {
                            Tile wi = giveMeTheTile(mach.worldInteractor);
                            string witi = wi.type.ToString() + "." + wi.subtype;
                            if (gd.getKeys(cat).Contains(witi))
                            {
                                mach.selectedRecipe = gd.getFromKey(cat, witi);
                                startMachine(mach, core.subtype); // no items to remove
                            }
                        }
                        break;
                    case "niem":
                        int giveEnergy;
                        if (mach.output != null)
                        { // withdraw
                            Tile engout = giveMeTheTile(mach.output);
                            if ((engout.subtype == "energy" || engout.amount < 1) && engout.amount < 9999)
                            {
                                engout.subtype = "energy";
                                giveEnergy = Math.Min(JPI.parseInt(mach.selectedRecipe), energyInNetwork);
                                engout.amount += giveEnergy;
                                energyInNetwork -= giveEnergy;
                                setTile(mach.output, engout);
                            }
                        }
                        if (mach.energyPort != null)
                        { // deposit
                            Tile engpor = giveMeTheTile(mach.energyPort);
                            giveEnergy = Math.Min(maxEnergy-energyInNetwork, engpor.amount);
                            energyInNetwork += giveEnergy;
                            engpor.amount -= giveEnergy;
                            setTile(mach.energyPort, engpor);
                        }
                        break;
                    case "mixr":
                        if (mach.inputs.Count == 2)
                        {
                            bool validItems = true;
                            string[] ing = new string[2];
                            for (int i=0;i<2;i++)
                            {
                                Tile tile = giveMeTheTile(mach.inputs[i]);
                                // its less scary to say something is true than not
                                if (tile.amount > 0 && tile.subtype.Length == 3 && (tile.subtype.Substring(0, 2) == "fr" || tile.subtype.Substring(0, 2) == "sf"))
                                {
                                    ing[i] = tile.subtype;
                                } else
                                {
                                    validItems = false;
                                }
                            }
                            if (validItems && !mach.runningRecipe)
                            {
                                mach.selectedRecipe = "@mfr"+ing[0]+","+ing[1];
                                if (startMachine(mach, core.subtype))
                                {
                                    for (int i=0;i<2;i++)
                                    {
                                        Tile tile = giveMeTheTile(mach.inputs[i]);
                                        tile.amount--;
                                        setTile(mach.inputs[i], tile);
                                    }
                                }
                            }
                        }
                        break;
                    case "comp":
                        for (int i=0;i<mach.inputs.Count;i++)
                        {
                            Tile input = giveMeTheTile(mach.inputs[i]);
                            if (input.amount > 0)
                            {
                                mach.number += getFoodValue(input.subtype);
                                input.amount--;
                            }
                        }
                        if (mach.startedRecipe > 1000 && !mach.runningRecipe)
                        {
                            mach.number -= 1000;
                            mach.selectedRecipe = "compo";
                            startMachine(mach, core.subtype);
                        }
                        break;
                    case "stor":
                        for (int i=0;i<mach.inputs.Count;i++)
                        {
                            Tile tile = giveMeTheTile(mach.inputs[i]);
                            if (mach.selectedRecipe == tile.subtype || mach.number < 1)
                            {
                                mach.selectedRecipe = tile.subtype;
                                int giveItem = Math.Min(4000-mach.number, tile.amount);
                                if (giveItem > 0)
                                {
                                    mach.number += giveItem;
                                    tile.amount -= giveItem;
                                    setTile(mach.inputs[i], tile);
                                }
                            }
                        }
                        if (mach.output != null && mach.number > 0)
                        {
                            Tile tile = giveMeTheTile(mach.output);
                            int giveItem = Math.Min(mach.number, defaultItemLimit-tile.amount);
                            tile.amount += giveItem;
                            mach.number -= giveItem;
                            setTile(mach.output, tile);
                        }
                        break;
                }
            }
        }
        tickMachIO(mac);
        machines[mac] = mach; // is this redundant? (i see the changes in debugger without this)
    }
    private void tickMachIO(Point mac)
    {
        Machine mach = machines[mac];
        if (mach.isFormed)
        {
            if (mach.output != null) nextUpdateTick.Add((Point)mach.output);
            for (int i=0;i<mach.inputs.Count;i++)
            {
                nextUpdateTick.Add(mach.inputs[i]);
            }
        }
    }
    public void tickMachines()
    {
        Point[] macp = new Point[machines.Keys.Count];
        machines.Keys.CopyTo(macp, 0);
        for (int i=0;i<macp.Length;i++)
        {
            Machine mac = machines[macp[i]];
            Tile core = giveMeTheTile(macp[i]);
            if (mac.runningRecipe && JPI.parseInt(gd.getFromKey("machineTime", core.subtype)) < mac.startedRecipe)
            {
                // running recipe so it can start a chain
                updateMachine(macp[i]);
                if (mac.isFormed && mac.output != null)
                {
                    Tile output = giveMeTheTile(mac.output);
                    switch (core.subtype)
                    {
                        case "cgen": case "ogen":
                            mac.runningRecipe = false;
                            output.subtype = "energy";
                            output.amount += JPI.parseInt(gd.getFromKey("generatorOutput", core.subtype));
                            tickMachIO(macp[i]);
                            setTile(mac.output, output);
                            break;
                        default:
                            mac.runningRecipe = false;
                            if (output.subtype == mac.selectedRecipe || output.amount < 1)
                            {
                                output.subtype = mac.selectedRecipe;
                                output.amount++;
                                setTile(mac.output, output);
                            }
                            tickMachIO(macp[i]);
                            break;
                    }
                }
            } else if (mac.runningRecipe)
            {
                mac.startedRecipe++;
            }
        }
    }
    private HashSet<Point> copyHashPoint(HashSet<Point> inp)
    {
        HashSet<Point> hs = new HashSet<Point>();
        Point[] inpa = inp.ToArray();
        for (int i=0;i<inpa.Length;i++)
        {
            hs.Add(new Point(inpa[i]));
        }
        return hs;
    }
    public void tickStuff()
    {
        tickMachines();
        HashSet<Point> tickNow = copyHashPoint(nextUpdateTick);
        List<Point> tilesTick = nextUpdateTick.ToList();
        foreach (Point tp in tilesTick)
        {
            // did you know: i used the ~ symbol to seperate stuff for sublists in scratch
            Point[] li = tickTile(tp);
            foreach (Point p in li)
            {
                tickNow.Add(p);
            }
        }
        tilesTick = tickNow.ToList();
        while (tilesTick.Count > 0)
        {
            Point tp = tilesTick[0];
            Point[] li = tickTile(tp);
            foreach (Point p in li)
            {
                tickNow.Add(p);
            }
            tilesTick.RemoveAt(0);
        }
    }
    private Point[] tickTile(Point tp)
    {
        List<Point> tickLater = new List<Point>();
        Tile tct = giveMeTheTile(tp);
        // @ticktile
        switch (tct.type)
        {
            case '+': // input
                // map arrow to machine and update machine
                if (tct.prog > 0)
                {
                    Point macht = tp.getTransform(arrowOffset[tct.prog-1]);
                    Tile tile = giveMeTheTile(macht);
                    if (tile.type == 'M')
                    {
                        updateMachine(macht);
                    }
                }
                break;
            case '-': // output
                if (tct.prog > 0 && tct.amount > 0)
                {
                    Point pto = tp.getTransform(arrowOffset[tct.prog-1]); // dest
                    Tile tile = giveMeTheTile(pto);
                    if (tile.type == 'p' && tct.subtype != "energy")
                    {
                        // put pipe
                    } else if (tile.type == '~' && tct.subtype == "energy")
                    {
                        // put cable
                        int giveEnergy = Math.Min(tct.amount, defaultItemLimit-tile.amount);
                        if (giveEnergy > 0)
                        {
                            tct.amount -= giveEnergy;
                            tile.amount += giveEnergy;
                            setTile(pto, tile);
                            setTile(tp, tct);
                            tickLater.Add(pto); // tick destination
                        }
                    }
                }
                break;
        }
        return tickLater.ToArray();
    }
}