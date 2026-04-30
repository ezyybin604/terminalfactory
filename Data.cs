
using MessagePack;

namespace E604terminalfactory;

// data structuring classes

public struct Tile
{
    public char type = ' ';
    public string subtype = ""; ///machine type/resource in tile/fruit type
    public int prog; // Amount of tile in tile/machine progress/stored amount/energy amount
    //public string item; // machine output/storage type
    public int amount; // amount of item for those tiles that need it
    public Tile() {}
    public Tile(char t, string subt="", int prg=0, int amt=0)
    {
        type = t;
        subtype = subt;
        prog = prg;
        amount = amt;
    }
    public Tile(Tile ct)
    {
        type = ct.type;
        subtype = ct.subtype;
        prog = ct.prog;
        amount = ct.amount;
    }
    public Tile(string full)
    {
        string[] ip = full.Split(".");
        if (ip.Length > 0 && ip[0].Length == 1)
        {
            type = ip[0][0];
        }
        if (ip.Length > 1)
        {
            subtype = ip[1];
        }
        if (ip.Length > 2)
        {
            prog = JPI.parseInt(ip[2]);
        }
    }
}

public struct Point // Wait wdm theres a Point data structure in system.drawing (this comment probably came after the next comment)
{ // just wondering, is there anything like this in SYSTEM c#
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
    public Point(Point point)
    {
        x = point.x;
        y = point.y;
    }
    public void transform(Point point)
    {
        x += point.x;
        y += point.y;
    }
    public Point getTransform(Point point)
    {
        return new Point(x+point.x, y+point.y);
    }
    public Point getTransform(int tx, int ty)
    {
        return new Point(x+tx, y+ty);
    }
    public Point getTransform(int tp)
    {
        return new Point(x+tp, y+tp);
    }
    public Point getReverse()
    {
        return new Point(-x, -y);
    }
    public Point getMultiply(int m)
    {
        return new Point(x*m, y*m);
    }
    public Point getDivide(int m)
    {
        return new Point(x/m, y/m);
    }
    private static int neutralize(int n)
    {
        if (n == 0)
        {
            return 0;
        }
        return n/Math.Abs(n);
    }
    public Point getNeutralized()
    {
        return new Point(neutralize(x), neutralize(y));
    }
}

public class TopBar
{
    //couldnt bother to go through all tip variable refs so i renamed it
    public string tipt = "Have you tried waiting?";
    public long lastTipChange = DateTime.MinValue.Ticks;
    public Dictionary<string, string[]> tips = new Dictionary<string, string[]>();
    public int menuSelection = 0;
    public int menuScroll = 0;
    public bool manualTip;
    public int tipPriority;
    public string returnScene = "";
    public int areyousure = 0;
    public void changeTip(string tipi, int priority, int extrams=0, bool forced=false)
    {
        if (priority >= tipPriority || forced)
        {
            lastTipChange = DateTime.Now.Ticks - (extrams * TimeSpan.TicksPerMillisecond);
            tipPriority = priority;
            tipt = tipi;
        }
    }
    public void changeTip(int priority, string tipi, bool forced=false)
    {
        changeTip(tipi, priority, 0, forced);
    }
}

// in-ven-tory
public class Slot
{
    public int num = 0;
    public string item = "";
    public Slot Copy()
    {
        Slot slot = new Slot();
        slot.item = item;
        slot.num = num;
        return slot;
    }
    public Slot(string ite, int nu)
    {
        item = ite;
        num = nu;
    }
    public Slot(string ite)
    {
        item = ite;
        num = 1;
    }
    public Slot() {}
}

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
    public int tier = 1;
}

[MessagePackObject]
public class Dragon
{ // 1000% not done
    [Key(0)]
    public long fedFood = 0;
    [Key(1)]
    public long fedDrink = 0;
    [Key(2)]
    public long dragonThirst = 1000; // 1000^(1.04^x) val
    [Key(3)]
    public long dragonHunger = 1000; // 1000^(1.03^x) val
    [Key(4)]
    public int age = 0; // maxes out at 128
    [Key(5)]
    public int scalesShed = 0;
    private void dragonTick()
    {
        if (fedFood >= dragonHunger && fedDrink >= dragonThirst)
        {
            age++;
            age = Math.Min(age, 128);
            scalesShed += (int)Math.Floor(age*1.2);
            fedDrink -= dragonThirst;
            fedFood -= dragonHunger;
            dragonThirst = 1000 * (long)Math.Pow(1.04, age); // 1000^(1.04^x) val
            dragonHunger = 1000 * (long)Math.Pow(1.03, age);
        }
    }
    public bool Feed(Slot slt)
    {
        // stuff
        int value = Factory.getFoodValue(slt.item);
        if (value > 0)
        {
            fedFood += slt.num * value;
            if (fedFood < -1000) fedFood = long.MaxValue;
            dragonTick();
            return true;
        }
        value = Factory.getWaterValue(slt.item);
        if (value > 0)
        {
            fedDrink += slt.num * value;
            if (fedDrink < -1000) fedDrink = long.MaxValue;
            dragonTick();
            return true;
        }
        return false;
    }
    public static string ShortenNumber(long num)
    { // yoinked from https://stackoverflow.com/questions/1555397/formatting-large-numbers-with-net
        int mag = (int)(Math.Floor(Math.Log10(num))/3);
        double divisor = Math.Pow(10, mag*3);
        double shortNumber = num / divisor;
        string suffix = "";
        switch(mag)
        {
            case 0:
                suffix = string.Empty;
                break;
            case 1:
                suffix = "k";
                break;
            case 2:
                suffix = "m";
                break;
            case 3:
                suffix = "b";
                break;
        }
        return shortNumber.ToString("N1") + suffix; // 4.3m
    }
    public string[] getInfo()
    {
        return [
            "Dragon Hunger: " + ShortenNumber(dragonHunger-fedFood),
            "Dragon Thirst: " + ShortenNumber(dragonThirst-fedDrink)
        ];
    }
}

public class Inventory
{
    public string latestGiven = "";
    public bool hasData = false;
    public GameData gd = new GameData();
    public const int Length = 150;
    public const int MaxPerSlot = 999;
    public Slot[] data = new Slot[Length];
    public string[] invmenud = new string[Length];
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
    public void updateMenu(Factory fact)
    {
        invlist.Clear();
        for (int i=0;i<data.Length;i++)
        {
            Slot slot = data[i];
            if (slot.num > 0)
            {
                invlist.Add(String.Format("x{0}, {1}", slot.num, fact.getItemName(slot.item)));
            }
        }
        string[] menuout = new string[invlist.Count];
        for (int i=0;i<invlist.Count;i++)
        {
            menuout[i] = invlist[i];
        }
        invmenud = menuout;
    }
    public Slot[] getRecipe(string catg, string result)
    {
        List<Slot> recipe = new List<Slot>();
        string[] ing = gd.getSplit(catg, result);
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
                if (slot.num >= Math.Min(itm[slot.item], MaxPerSlot))
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
                latestGiven = item.item;
                return true; // sry if this happens your items get voided
            }
        }
        latestGiven = item.item;
        return true;
    }
}

public class FTutorial
{ // tutorial controller
    public Point boxpos = new Point(256, 256); // JUST PUT THIS AS FAR AWAY FROM 0,0 IF IT ERRORS OUT ON HIGH WINDOWSIZES
    public Point size = new Point(60, 15);
    public Point center = new Point();
    string[] messageprog = [];
    int mpgs = 0; // number in tutorial (21 is pipe)
    private int action = 0; // how many times action happened
    required public Factory fact;
    public List<string> acts = new List<string>();
    public int ticksSinceLast = 0;
    public string curact = "";
    int numact = 0;
    string animcur = "";
    int animf = 0;
    public bool canDelete = true;
    public bool worldModify = true;
    List<Point> path = [];
    public void getPath(List<Point> dots)
    {
        path = [];
        for (int i=0;i<dots.Count-1;i++)
        { // start dot + line dots
            Point pt = dots[i];
            Point df = dots[i+1].getTransform(pt.getMultiply(-1)).getNeutralized();
            while (!pt.Equals(dots[i+1]))
            {
                path.Add(pt);
                pt.transform(df);
            }
        }
        path.Add(dots.Last()); // last dot
    }
    public void updateAction()
    {
        messageprog = fact.gd.getKeys("tutorialMsg");
        center = boxpos.getTransform(size.getDivide(2));
        string[] curd = fact.gd.getSplit("tutorialMsg", messageprog[mpgs]);
        numact = JPI.parseInt(curd[0]);
        curact = curd[1];
        beforeAction();
    }
    public FTutorial() {}
    public bool tickTutorial()
    {
        for (string goin;acts.Count>0;acts.RemoveAt(0)) // chat is this a cool way to use a for loop
        {
            goin = acts[0];
            if (curact == "tick") action++;
            if (goin == curact) action++;
        }
        if (action >= numact && ticksSinceLast > 2 && animcur == "")
        {
            mpgs++;
            action = 0;
            ticksSinceLast = 0;
            updateAction();
            return true;
        }
        if (animcur != "")
        {
            doAnimation();
        }
        ticksSinceLast++;
        return false;
    }
    public string updateTip()
    {
        return messageprog[mpgs];
    }
    private void beforeAction()
    {
        switch (GameData.getindex(fact.gd.getSplit("tutorialMsg", messageprog[mpgs]), 2))
        {
            case "spawnsand":
                fact.placeFeatureUpdate(Factory.pointShapeGenerator(1, "diamond"), new Tile("f.sand.16"), center.getTransform(-1));
                break;
            case "giveitem":
                fact.inventory.addItem(new Slot("copper", Factory.generateIntRange(30, 120)));
                fact.inventory.addItem(new Slot("iron", Factory.generateIntRange(30, 120)));
                break;
            case "sneakysand":
                if (fact.inventory.getItemAmount("sand") < 16)
                {
                    fact.inventory.addItem(new Slot("sand", 32));
                }
                break;
            case "placestone":
                animcur = "breakplacestone";
                animf = 0;
                break;
            case "clearplacecgen":
                worldModify = false;
                animcur = "placecgen";
                animf = 0;
                break;
            case "placeinput":
                fact.setTileUpdate(center.getTransform(-1, 0), new Tile('+'));
                break;
            case "placeoutput":
                fact.setTileUpdate(center.getTransform(1, 0), new Tile('-'));
                break;
            case "fillmachine":
                animcur = "fillmachine";
                animf = 0;
                break;
            case "highlightcorner": // if you feel midly annoyed by the word swap, thats okay and also thank you
                animcur = "cornerhighlight";
                animf = 0;
                break;
            case "stopdelete":
                canDelete = false;
                break;
            case "placeport":
                fact.setTileUpdate(center.getTransform(0, -1), new Tile('*'));
                break;
            case "placewi":
                fact.setTileUpdate(center.getTransform(0, 1), new Tile('@'));
                break;
            case "placepipes":
                getPath([
                    center.getTransform(2, 0),
                    center.getTransform(8, 0),
                    center.getTransform(8, 4),
                    center.getTransform(-8, 4),
                    center.getTransform(-8, 0),
                    center.getTransform(-2, 0)
                ]);
                animcur = "dopipepath";
                animf = 0;
                break;
            case "getcomposter":
                animcur = "pipegone";
                animf = 0;
                break;
            case "placecable":
                getPath([
                    center.getTransform(2, 0),
                    center.getTransform(8, 0),
                    center.getTransform(8, -4),
                    center.getTransform(0, -4),
                    center.getTransform(0, -2),
                ]);
                animcur = "placecable";
                animf = 0;
                break;
            case "removecable":
                animcur = "cablegone";
                animf = 0;
                break;
            case "changeToAssembler":
                fact.setTileUpdate(center, new Tile("M.asmb"));
                fact.machines[center].selectedRecipe = "";
                break;
            case "coalgenreplace":
                fact.setTileUpdate(center, new Tile("M.cgen"));
                Point inp = center.getTransform(-1, 0);
                Tile inti = fact.giveMeTheTile(inp);
                inti.subtype = "coal";
                inti.amount = int.MaxValue;
                fact.setTile(inp, inti);
                animcur = "ridenergy";
                break;
            case "givefood":
                fact.inventory.addItem(new Slot("fr1", 999));
                break;
            default:
                break;
        }
    }
    private void doAnimation()
    {
        switch (animcur)
        {
            case "breakplacestone":
                if (animf == 1)
                {
                    fact.placeFeatureUpdate(Factory.pointShapeGenerator(1, "diamond"), fact.emptyTile, center.getTransform(-1));
                } else if (animf == 3)
                {
                    fact.placeFeatureUpdate(Factory.pointShapeGenerator(6, "scatter", 10), new Tile("f.stone.24"), center.getTransform(-3));
                } else if (animf > 4)
                {
                    animcur = "";
                }
                break;
            case "placecgen":
                if (animf == 0)
                { // I WANT MY INLINE CURLY BRACKETS I WILL NOT STAND FOR THIS BLOAT ANY LONGER AAAAAAA
                    fact.placeFeatureUpdate(Factory.pointShapeGenerator(size.getTransform(-1), "rectangle"), fact.emptyTile, boxpos.getTransform(1));
                } else if (animf == 4)
                {
                    fact.setTileUpdate(center, new Tile("M.cgen"));
                    fact.machines.Add(center, new Machine());
                    animcur = "";
                }
                break;
            case "fillmachine":
                if (animf%2 == 0 && animf < 16)
                {
                    Point block = center.getTransform(fact.machineArea[animf/2]);
                    if (fact.giveMeTheTile(block).type == ' ') fact.setTileUpdate(block, new Tile("m.1"));
                } else if (animf > 20)
                {
                    animcur = "";
                }
                break;
            case "cornerhighlight":
                if (animf%8 == 3)
                {
                    Tile goc = new Tile("m.3");
                    if (animf > 8) goc = new Tile("m.1");
                    for (int i=0;i<4;i++)
                    {
                        fact.setTileUpdate(center.getTransform(fact.machineArea[i]), goc);
                    }
                } else if (animf > 18)
                {
                    animcur = "";
                }
                break;
            case "dopipepath": case "placecable":
                if (animf < path.Count)
                {
                    Tile dothat;
                    if (animcur == "placecable")
                    {
                        dothat = new Tile('~');
                    } else
                    {
                        dothat = new Tile('p', "", fact.getPipeDir(path[animf]));
                    }
                    fact.setTileUpdate(path[animf], dothat);
                } else
                {
                    animcur = "";
                }
                break;
            case "pipegone": case "cablegone":
                if (animf < path.Count)
                {
                    fact.setTileUpdate(path[animf], new Tile(' '));
                } else
                {
                    if (animcur == "pipegone")
                    {
                        fact.setTileUpdate(center, new Tile("M.comp"));
                        fact.setTileUpdate(center.getTransform(0, 1), new Tile("m.1"));
                        fact.machines[center].selectedRecipe = "compo";
                    }
                    animcur = "";
                }
                break;
            case "ridenergy":
                Point inp = center.getTransform(1, 0);
                Tile inti = fact.giveMeTheTile(inp);
                inti.amount = 0;
                fact.setTile(inp, inti);
                fact.updateMachines();
                if (action >= numact)
                {
                    animcur = "";
                }
                break;
            default: // Intentionally crash the game also THIS IS THE ANIMATION CONTROLLER THIS IS A REMINDER
                mpgs = int.MaxValue;
                break;
        }
        animf++;
    }
}

public struct TileBroken
{
    public TileBroken(Slot? slot, Point point, Tile tile)
    {
        location = point;
        newTile = tile;
        if (slot == null)
        {
            hasItem = false;
            item = new Slot();
        } else
        {
            hasItem = true;
            item = slot;
        }
    }
    public bool hasItem = false;
    public Slot item;
    public Point location;
    public Tile newTile;
}