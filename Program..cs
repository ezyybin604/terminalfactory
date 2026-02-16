
using System;
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)

/*
Tile types:
f: finite resource
i: infinite resource
' ': grass/empty
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
M: machine
b: bush
*/
public struct Tile
{
    public char type;
    public string subtype; ///machine type/resource in tile/fruit type
    public int prog; // Amount of tile in tile/machine progress/stored amount/energy amount
    public string item; // machine output/storage type
}
public struct Chunk
{
    public int x;
    public int y;
    public Tile[][] data;
}
class Factory // factory data
{
    public string savefile = "defualt.tf";
    int chunkSize = 16;
    // [x][y]
    Dictionary<int, Dictionary<int, Chunk>> world = new Dictionary<int, Dictionary<int, Chunk>>();
    void generateChunk(int x, int y)
    {
        Chunk chunk = new Chunk();
        chunk.x = x;
        chunk.y = y;
        // Generate chunk data herre
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
    // also make a worldgen function
    // add world tick function to tick the world
}
class Game
{
    public static void Main()
    {
        Game game = new Game();
        Factory factory = new Factory();
        if (!File.Exists(factory.savefile))
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
            }
            Console.ReadLine();
            
        }
    }
}
