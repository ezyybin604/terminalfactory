
namespace terminalfactory;

class Factory // factory data
{
    public string savefile = "defualt.tf";
    // add world storage, uhhh figure that out later
    // also make a worldgen function
    // add world tick function to tick the world
}
class Game
{
    void AskForWindowChange(int width, int height)
    {
        Console.Clear();
        try
        {
            Console.SetWindowSize(width, height);
        }
        catch
        {
            while ((Console.WindowWidth != width) || (Console.WindowHeight != height))
            {
                Console.WriteLine(String.Format("Window size tried to change, but an error occured.\nCan you change it to {0}x{1}?", width, height));
                Console.WriteLine(String.Format("Current size: {0}x{1}", Console.WindowWidth, Console.WindowHeight));
                Console.WriteLine("Press key when ready to try again.");
                Console.ReadKey();
            }
        }
    }
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
            game.AskForWindowChange(120,40);
        }
    }
}
