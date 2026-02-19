
using System;
using System.IO.Compression;
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)

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

class Game // impliment cursor/scrolling tomorrowwwwww
{
    Point scroll = new Point();
    Factory factory = new Factory();
    Dictionary<string, string> subtColor = new Dictionary<string, string>();
    Dictionary<string, ConsoleColor> strColor = new Dictionary<string, ConsoleColor>();
    char[] natrualTiles = ['f', 'i', ']', 'b'];
    void generateNeeded()
    {
        int w = (int)Math.Ceiling((double)(Console.WindowWidth/factory.chunkSize));
        int h = (int)Math.Ceiling((double)((Console.WindowHeight-2)/factory.chunkSize));
        w++;
        h++;
        for (int x=0;x<w;x++)
        {
            for (int y=0;y<h;y++)
            {
                factory.generateChunk(x, y);
            }
        }
    }
    Tile giveMeTheTile(int x, int y)
    {
        Point chunk = new Point((int)Math.Floor((double)(x/factory.chunkSize)), (int)Math.Floor((double)(y/factory.chunkSize)));
        Point index = new Point(x-(chunk.x*factory.chunkSize), y-(chunk.y*factory.chunkSize));
        return factory.world[chunk.x][chunk.y].data[index.x][index.y];
    }
    void initalizeColorThingysProbably()
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
    void displayStuff()
    {
        Console.Clear();
        Console.WriteLine("Insert tips/controls/tutorial");
        Console.WriteLine(new string('~', Console.WindowWidth));
        string[] lineResult = new string[(Console.WindowWidth*2)+1];
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            int idx = 0;
            for (int x=0;x<Console.WindowWidth;x++)
            {
                Tile t = giveMeTheTile(x, i);
                string state = "";
                if (t.subtype == null)
                {
                    t.subtype = "";
                }
                if (subtColor.ContainsKey(t.subtype) && natrualTiles.Contains(t.type)) // for natrually generating stuff only
                {
                    lineResult[idx] = "/" + subtColor[t.subtype];
                    idx++;
                    state = "natrualColor";
                }
                if (t.type == ']')
                {
                    lineResult[idx] = "/gray";
                    idx++;
                }
                lineResult[idx] = t.type.ToString();
                if (state == "natrualColor")
                {
                    if (t.subtype.Contains("water"))
                    {
                        lineResult[idx] = "░";
                    } else if (t.subtype == "stone")
                    {
                        lineResult[idx] = "s";
                    } else if (t.subtype == "bone")
                    {
                        lineResult[idx] = "3";
                    }
                     else if (t.subtype == "oil")
                    {
                        lineResult[idx] = "o";
                    }
                }
                idx++;
            }
            lineResult[idx] = "/end";
            Console.ForegroundColor = ConsoleColor.Green;
            for (int o=0;lineResult[o] != "/end";o++)
            {
                string yes = lineResult[o];
                if (yes[0] == '/' && yes.Length > 1)
                {
                    yes = yes.Substring(1);
                    Console.ForegroundColor = strColor[yes];
                } else
                {
                    Console.Write(lineResult[o]);
                    Console.ForegroundColor = ConsoleColor.Green;
                }
            }
            if (i+1 < Console.WindowHeight-2)
            {
                Console.WriteLine();
            }
        }
    }
    public static void Main()
    {
        Game game = new Game();
        game.factory = new Factory();
        if (!File.Exists(game.factory.savefile))
        {
            Console.WriteLine("No savefile found.");
            Console.Write("Skip intro? (y/n):");
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
            game.initalizeColorThingysProbably();
            game.generateNeeded();
            game.displayStuff();
            Console.ReadLine();
        }
    }
}
