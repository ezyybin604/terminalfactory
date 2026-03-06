
using System.Reflection.Metadata.Ecma335;
using System.Xml.Serialization;

namespace terminalfactory;

// type = # of !s
public class GameData
{
    public string state = "prep";
    private Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();
    public string getindex(string[] strings, int idx)
    {
        if (idx > strings.Length-1)
        {
            return "";
        }
        return strings[idx];
    }
    public string[] autoTilePick(Tile tile, string infokey="blockinfo")
    {
        string info = getFromKey(infokey, tile.type.ToString() + "." + tile.subtype);
        if (info == "")
        {
            info = getFromKey(infokey, tile.type.ToString());
        }
        return info.Split(",");
    }
    public string[] getKeys(string catg)
    {
        // c# overcomplicating things for no reason :skull:
        Dictionary<string, string>.KeyCollection why = data[catg].Keys;
        string[] outa = new string[why.Count];
        why.CopyTo(outa, 0);
        return outa;
    }
    public string autoTilePick(Tile tile, int idx=0, string infokey="blockinfo")
    {
        return getindex(autoTilePick(tile, infokey), idx);
    }
    public string getFromKey(string catg, string key)
    {
        if (data[catg].ContainsKey(key))
        {
            return data[catg][key];
        }
        return "";
    }
    public GameData()
    {
        state = "done";
    }
    public GameData(string filename)
    {
        if (File.Exists(filename))
        {
            using (StreamReader sr = File.OpenText(filename))
            {
                string? s;
                string? sect = null;
                Dictionary<string,string> stuff = new Dictionary<string, string>();
                while ((s = sr.ReadLine()) != null)
                {
                    if (s != "" && s[0] != '#')
                    {
                        if (s[0] == '!')
                        {
                            int i=0;
                            while (s[i] == '!' && s.Length-1 > i) { i++; }
                            string after = s.Substring(i);
                            if (i == 1)
                            {
                                stuff = new Dictionary<string, string>();
                                sect = after;
                            } else if (i == 2 && sect != null)
                            {
                                data[sect] = stuff;
                                sect = null;
                            } else
                            {
                                Console.Error.WriteLine("gamedata ohno");
                            }
                        } else if (sect != null)
                        {
                            string[] ss = s.Split("=");
                            if (ss.Length == 2)
                            {
                                stuff[ss[0]] = ss[1];
                            }
                        }
                    }
                }
                state = "done";
            }
        } else
        {
            state = "nofile";
        }
    }
}

class FileManagement
{
    // copying from docs
    private void saveToFile(Type type, string fn, string savefile, object toSer)
    {
        XmlSerializer xml = new XmlSerializer(type);
        TextWriter writer = new StreamWriter(Path.Join(savefile, fn));
        xml.Serialize(writer, toSer);
        writer.Close();
    }
    private object loadFromFile(Type type, string fn, string savefile, object nullDefault)
    {
        XmlSerializer serializer = new XmlSerializer(type);
        string fname = Path.Join(savefile, fn);
        if (File.Exists(fname))
        {
            FileStream fs = new FileStream(fname, FileMode.Open);
            object? o = serializer.Deserialize(fs);
            if (o != null)
            {
                return o;
            }
        }
        return nullDefault;
    }

    // for stuff
    public void SaveStuff(Factory fact, Point cursor, Point camera)
    {
        fact.inventory.fix();
        string save = fact.savefile;
        saveToFile(typeof(InventoryData), "invdata", save, new InventoryData(fact.inventory));
        saveToFile(typeof(MachineCursor), "player", save, new MachineCursor(fact.machines, cursor, camera));
        List<Point> regions = fact.getRegions(); // straightup stealing the concept of region files from minecraft
        for (int i=0;i<regions.Count;i++)
        {
            // stuff here
        }
    }
    public Slot[] LoadInventory(Factory fact)
    {
        return (Slot[])loadFromFile(typeof(InventoryData), "invdata", fact.savefile, new Slot[0]);
    }
    public Point[] LoadMachines(Factory fact)
    {
        MachineCursor deser = (MachineCursor)loadFromFile(typeof(MachineCursor), "player", fact.savefile, new MachineCursor());
        fact.machines = deser.machines;
        return [deser.cursor, deser.camera];
    }
}

// 100% Ridiclous (looking) use of classes (or not depending on how judgy you feel like today)
public class InventoryData
{ // for serization or however you spell it
    public Slot[] data = new Slot[Inventory.Length];
    public InventoryData(Inventory inv) {
        data = inv.data;
    }
    public InventoryData() {}
}

public class Region
{ // 3x3 area of chunks
    public Point regionLocation = new Point();
    public Slot[][][] data = new Slot[9][][]; // (0 length chunk=empty/not generated)
}

public class MachineCursor
{
    public Point cursor = new Point();
    public Point camera = new Point();
    public Dictionary<Point, Machine> machines = new Dictionary<Point, Machine>();
    public MachineCursor(Dictionary<Point, Machine> macs, Point cursorp, Point camerap) {
        cursor = cursorp;
        machines = macs;
        camera = camerap;
    }
    public MachineCursor() {}
}