
using System;
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
}

class Game
{
    Point scroll = new Point();
    Factory factory = new Factory();
    void generateNeeded()
    {
        int w = (int)Math.Ceiling((double)(Console.WindowWidth/factory.chunkSize));
        int h = (int)Math.Ceiling((double)((Console.WindowHeight-2)/factory.chunkSize));
        for (int x=0;x<w;x++)
        {
            for (int y=0;y<h;y++)
            {
                factory.generateChunk(x, y);
            }
        }
    }
    void displayStuff()
    {
        Console.Clear();
        Console.WriteLine("Insert tips/controls/tutorial");
        Console.WriteLine(new string('~', Console.WindowWidth));
        // uhhh finish world render stuff later
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
            }
            Console.ReadLine();
            game.generateNeeded();
            game.displayStuff();
            Console.ReadLine();
        }
    }
}
