
using System;
namespace terminalfactory;

// Inspired a little bit by https://www.youtu.be/cZYNADOHhVY :)
// https://github.com/ezyybin604/terminalfactory/

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

class TopBar
{
    public bool showTips = true;
    public string tip = "default tip";
    public int lastTipChange = DateTime.Now.Second;
    public Dictionary<string, string[]> tips = new Dictionary<string, string[]>();
}

class Game
{
    // 4 Scenes: game,end,(invintory/inv caus i dont know how to spell),pause,craft
    string scene = "game";
    Thread? gameThread;
    Point scroll = new Point();
    Point cursor = new Point();
    Factory factory = new Factory();
    List<ConsoleKeyInfo> readkeylog = new List<ConsoleKeyInfo>();
    DateTime time = DateTime.Now;
    TopBar topbar = new TopBar();
    HashSet<int> linesToUpdate = new HashSet<int>(); // i didnt renember what the data type was called so i had to google it
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
    void initStuff()
    {
        factory.initFactory();
        topbar.tips.Add("game", [
            "Use WASD to move",
            "Press P to pause"
        ]);
        topbar.tips.Add("pause", [
            "Use WS to change selection",
            "Press Z to select"
        ]);
        topbar.tips.Add("inv", [
            "Use WS to change selection",
            "Press Z to select",
            "Press A to enter crafts menu",
            "Press X to go back"
        ]);
        topbar.tips.Add("craft", [
            "Use WS to change selection",
            "Press Z to select",
            "Press X to go back"
        ]);
        topbar.tips.Add("end", ["now go away"]);

        gameThread = new Thread(runTheGameIg);
    }
    void menuDisplay()
    {
        // do this later
    }
    void updateBar()
    {
        Console.ResetColor();
        Console.SetCursorPosition(0, 0);
        Console.WriteLine(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, 0);
        Console.Write(" ");

        //Console.WriteLine("stdin:"+stdin);
        Console.WriteLine(topbar.tip);
        Console.WriteLine(new string('~', Console.WindowWidth));

        Console.SetCursorPosition(0, 0);
    }
    void displayStuff()
    {
        Console.Clear();
        updateBar();
        if (scene == "pause" || scene == "inv")
        {
            menuDisplay();
            return;
        }
        Console.SetCursorPosition(0, 2);
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            factory.displayLine(i, cursor);
        }
    }
    void updateScreen()
    {
        for (int i=0;i<Console.WindowHeight-2;i++)
        {
            if (linesToUpdate.Contains(i))
            {
                Console.SetCursorPosition(0, i+2);
                factory.displayLine(i, cursor);
            }
        }
        Console.SetCursorPosition(0, 1);
        linesToUpdate.Clear();
    }
    void adjustCamera()
    {
        // do that laerkjesnf
        // finish later
    }
    void useInput(ConsoleKeyInfo key)
    {
        char ch = key.KeyChar;
        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            return;
        }
        switch (scene)
        {
            case "game":
                switch (ch)
                {
                    case 'a':
                        cursor.x--;
                        linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 's':
                        cursor.y++;
                        linesToUpdate.Add(cursor.y);
                        linesToUpdate.Add(cursor.y-1);
                        adjustCamera();
                        break;
                    case 'w':
                        cursor.y--;
                        linesToUpdate.Add(cursor.y);
                        linesToUpdate.Add(cursor.y+1);
                        adjustCamera();
                        break;
                    case 'd':
                        cursor.x++;
                        linesToUpdate.Add(cursor.y);
                        adjustCamera();
                        break;
                    case 'p':
                        scene = "pause";
                        displayStuff();
                        break;
                    case 'i':
                        scene = "inv";
                        displayStuff();
                        break;
                }
                break;
        }
        cursor.x = Math.Max(cursor.x, 0);
    }
    void inputSutff() // dont try and merge this with the main function (runthegameig) it wont end well
    {
        ConsoleKeyInfo input;
        while (scene != "end")
        {
            input = Console.ReadKey();
            readkeylog.Add(input);
        }
    }
    void runTheGameIg()
    {
        Point windowSizePrevious = new Point(Console.WindowWidth, Console.WindowHeight);
        Point windowSize = new Point();
        generateNeeded();
        displayStuff();
        while (scene != "end")
        {
            time = DateTime.Now;
            if (time.Second-5 > topbar.lastTipChange)
            {
                int itip = (int)Math.Round(factory.generateRange(0, topbar.tips[scene].Length-1));
                topbar.tip = topbar.tips[scene][itip];
                topbar.lastTipChange = time.Second;
            }
            windowSize = new Point(Console.WindowWidth, Console.WindowHeight);
            if (!windowSize.Equals(windowSizePrevious))
            {
                windowSizePrevious = windowSize;
                generateNeeded();
                adjustCamera();
                displayStuff();
            }
            while (readkeylog.Count > 0)
            {
                useInput(readkeylog[0]);
                readkeylog.RemoveAt(0);
            }
            updateScreen();
            updateBar();
            Thread.Sleep(50);
            //topbar.tip = 
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
            Console.Title = "terminalfactory";
            game.initStuff();
            if (game.gameThread != null)
            {
                game.gameThread.Start();
                game.inputSutff();
            }
            Console.Clear();
            Console.WriteLine("bye");
            Thread.Sleep(1000);
        }
    }
}
