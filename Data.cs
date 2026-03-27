
namespace terminalfactory;

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
}

class TopBar
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

class Inventory
{
    public bool hasData = false;
    public GameData gd = new GameData();
    public const int Length = 150;
    public const int MaxPerSlot = 999;
    public Slot[] data = new Slot[Length];
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
    public string[] getMenu(Factory fact)
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
        return menuout;
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
                if (slot.num >= itm[slot.item])
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
                return true; // sry if this happens your items get voided
            }
        }
        return true;
    }
}

class FTutorial
{ // tutorial controller
    public Point boxpos = new Point(64, 64);
    public Point size = new Point(60, 15);
    public FTutorial()
    {
        // add stuff here later
    }
}