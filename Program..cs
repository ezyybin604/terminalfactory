
namespace terminalfactory;

class Factory // factory data
{
    public string savefile = "defualt.tf";
}
class Game
{
    public static void Main()
    {
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
            Console.SetWindowSize(40,30);
            Console.ReadKey();
        }
    }
}
