
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

class Factory // factory data
{
    Random rng = new Random();
    public string savefile = "defualt.tf";
    public int chunkSize = 16;
    // [x][y]
    public Dictionary<int, Dictionary<int, Chunk>> world = new Dictionary<int, Dictionary<int, Chunk>>();

    private double generateRange(double min, double max)
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
    // add world storage, uhhh figure that out later
    // add world tick function to tick the world
}