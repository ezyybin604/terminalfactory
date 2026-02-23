
namespace terminalfactory;

/*
Tile types:
f: finite resource
i: infinite resource
b: bush
'`': grass/empty
1: machine block tier 1
2: machine block tier 2
3: machine block tier 3
*: energy port
+: add/input
-: get/output
=: pipe
#: cable
]: trough
@: world interactor
+: machine frame
M: machine
*/

class Factory // factory data / big verbose stuff related to factory
{
    Dictionary<string, string> subtColor = new Dictionary<string, string>();
    Dictionary<string, ConsoleColor> strColor = new Dictionary<string, ConsoleColor>();
    char[] natrualTiles = ['f', 'i', ']', 'b'];
    Random rng = new Random();
    public string savefile = "defualt.tf";
    public const int chunkSize = 16;
    // [x][y]
    public Dictionary<int, Dictionary<int, Chunk>> world = new Dictionary<int, Dictionary<int, Chunk>>();

    public double generateRange(double min, double max)
    {
        return (rng.NextDouble()*(max-min))+min;
    }
    private int generateIntRange(int min, int max)
    {
        return (int)Math.Round(generateRange(min, max));
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
        Chunk chunk = new Chunk();
        chunk.x = x;
        chunk.y = y;
        // Generate chunk data herre
        chunk.data = new Tile[chunkSize][];
        for (int i=0;i<chunkSize;i++)
        {
            chunk.data[i] = new Tile[chunkSize];
            for (int o=0;o<chunkSize;o++)
            {
                chunk.data[i][o] = new Tile();
                chunk.data[i][o].type = '`';
                if (x == 0 && i == 0)
                {
                    chunk.data[i][o].type = ']';
                }
            }
        }
        for (int cnt=0;cnt<2;cnt++)
        {
            int fx = (int)(rng.NextDouble()*10);
            int fy = (int)(rng.NextDouble()*10);
            List<Point> shape;
            Tile copy = new Tile();
            switch ((int)Math.Floor(rng.NextSingle()*8))
            {
                case 0:
                    // Water springs (diamond shape, 2-4 radius determining tier of water 2=pond, 4=ocean 3=mountain spring)
                    int water = generateIntRange(2, 4);
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
                    shape = pointShapeGenerator(4, "scatter", generateIntRange(3, 8));
                    copy.type = 'b';
                    copy.subtype = "fr" + generateIntRange(1, 5).ToString();
                    break;
                case 3: case 4: case 2:
                    // Ore/Rock Cluster (scatter 5-12 randomly in 3x3 area)
                    shape = pointShapeGenerator(5, "scatter", generateIntRange(8, 20));
                    string[] sbt = ["diamond", "iron", "copper", "carbon", "stone", "bone", "oil", "sand"];
                    copy.subtype = sbt[generateIntRange(0, 7)];
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
                    if (chunk.data[pointGo.x][pointGo.y].type != ']')
                    {
                        chunk.data[pointGo.x][pointGo.y] = copy;
                    }
                }
            }
        }

        if (!world.Keys.Contains(x))
        {
            world.Add(x, new Dictionary<int, Chunk>());
        }
        if (!world[x].Keys.Contains(y))
        {
            world[x].Add(y, chunk);
        }
    }

    public void initFactory()
    {
        subtColor.Add("water1", "blue");
        subtColor.Add("water2", "blue");
        subtColor.Add("water3", "blue");
        subtColor.Add("oil", "darkgray");
        subtColor.Add("diamond", "cyan");
        subtColor.Add("iron", "white");
        subtColor.Add("copper", "darkyellow");
        subtColor.Add("carbon", "darkgray");
        subtColor.Add("stone", "gray");
        subtColor.Add("bone", "white");
        subtColor.Add("sand", "yellow");

        subtColor.Add("fr1", "red"); // strawberry
        subtColor.Add("fr2", "yellow"); // abiu
        subtColor.Add("fr3", "darkyellow"); // dates
        subtColor.Add("fr4", "magenta"); // dragonfruit
        subtColor.Add("fr5", "darkgreen"); // jackfruit

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
    }
    public void invertColors()
    { // hheheheeheh
        ConsoleColor bg = Console.ForegroundColor;
        ConsoleColor fg = Console.BackgroundColor;
        Console.BackgroundColor = bg;
        Console.ForegroundColor = fg;
    }
    Tile giveMeTheTile(int x, int y)
    {
        Point chunk = new Point((int)Math.Floor((double)(x/chunkSize)), (int)Math.Floor((double)(y/chunkSize)));
        Point index = new Point(x%chunkSize, y%chunkSize);
        if (y < 0)
        {
            index.y+=chunkSize;
            index.y%=chunkSize;
        }
        return world[chunk.x][chunk.y].data[index.x][index.y];
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
            Tile t = giveMeTheTile(x+scroll.x, y);
            string state = "";
            if (t.subtype == null)
            {
                t.subtype = "";
            }
            if (subtColor.ContainsKey(t.subtype) && natrualTiles.Contains(t.type)) // for natrually generating stuff only
            {
                if (continueText && !color)
                {
                    idx++;
                }
                currentColor = subtColor[t.subtype];
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
            char addChar = t.type;
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
                }
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
        //Console.WriteLine(String.Join(",", lineResult));
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
    // add world storage, uhhh figure that out later
    // add world tick function to tick the world
}