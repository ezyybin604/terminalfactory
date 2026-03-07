
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
    public const int regionLength = Factory.regionArea * Factory.regionArea;
    private void saveToFile(Type type, string fn, string savefile, object toSer)
    {
        string file = Path.Join(savefile, fn);
        File.WriteAllText(file, ""); // clears my anxiety

        XmlSerializer xml = new XmlSerializer(type);
        TextWriter writer = new StreamWriter(file);
        xml.Serialize(writer, toSer);
        writer.Close();
    }
    private object loadFromFile(Type type, string fn, string savefile, object nullDefault)
    {
        Console.WriteLine("Started loading " + fn);
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

    private Tile[][] getChunk(Factory fact, Point ch)
    {
        if (fact.world.Keys.Contains(ch.x))
        {
            if (fact.world[ch.x].Keys.Contains(ch.y))
            {
                return fact.world[ch.x][ch.y];
            }
        }
        return new Tile[0][]; // the array.empty fix was ugly
    }
    private Point getPointIndex(int inp)
    {
        return new Point(inp%Factory.regionArea, (int)Math.Floor((double)(inp/Factory.regionArea)));
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
            Region region = new Region();
            region.regionLocation = regions[i];
            for (int p=0;p<regionLength;p++)
            {
                Point chunk = regions[i].getTransform(getPointIndex(p));
                region.data[p] = getChunk(fact, chunk);
            }
            saveToFile(typeof(Region), "region" + i.ToString(), save, region);
        }
    }
    public Slot[] LoadWorld(Factory fact)
    {
        int i=0;
        string save = fact.savefile;
        string fname = "region" + i.ToString();
        while (File.Exists(Path.Join(save, fname)))
        {
            Region region = (Region)loadFromFile(typeof(Region), fname, save, new Region());
            for (int x=0;x<region.data.Length;x++)
            {
                if (region.data[x].Length > 0)
                {
                    Point chunkLoc = region.regionLocation.getTransform(getPointIndex(x));
                    fact.placeChunk(chunkLoc, region.data[x]);
                }
            }
            i++;
            fname = "region" + i.ToString();
        }
        InventoryData id = (InventoryData)loadFromFile(typeof(InventoryData), "invdata", save, new Slot[0]);
        return id.data;
    }
    public Point[] LoadMachines(Factory fact)
    {
        MachineCursor deser = (MachineCursor)loadFromFile(typeof(MachineCursor), "player", fact.savefile, new MachineCursor());
        fact.machines = deser.returnMachines();
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
    public Tile[][][] data = new Tile[FileManagement.regionLength][][]; // (0 length chunk=empty/not generated)
}

public class MachineCursor
{
    public Point cursor = new Point();
    public Point camera = new Point();
    public Point[] macsk = Array.Empty<Point>();
    public Machine[] macsv = Array.Empty<Machine>();
    public MachineCursor(Dictionary<Point, Machine> macs, Point cursorp, Point camerap) {
        cursor = cursorp;
        macsk = new Point[macs.Count];
        macsv = new Machine[macs.Count];
        macs.Keys.CopyTo(macsk, 0);
        for (int i=0;i<macsk.Length;i++)
        {
            macsv[i] = macs[macsk[i]];
        }
        camera = camerap;
    }
    public Dictionary<Point, Machine> returnMachines()
    {
        Dictionary<Point, Machine> res = new Dictionary<Point, Machine>();
        for (int i=0;i<macsk.Length;i++)
        {
            res[macsk[i]] = macsv[i];
        }
        return res;
    }
    public MachineCursor() {}
}