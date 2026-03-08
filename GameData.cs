
using System.Text.Json;

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
                            } else if (i == 3 && sect != null)
                            {
                                string[] keys = new string[data[after].Count];
                                data[after].Keys.CopyTo(keys, 0);
                                for (int x=0;x<keys.Length;x++)
                                {
                                    stuff[keys[x]] = data[after][keys[x]];
                                }
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
                            } else
                            {
                                state = "formatting error";
                                return;
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
    private void saveToFile(string fn, string savefile, object toSer)
    {
        string file = Path.Join(savefile, fn + ".json");
        File.WriteAllText(file, JsonSerializer.Serialize(toSer));
    }
    private object loadFromFile(Type type, string fn, string savefile, object nullDefault)
    {
        Console.WriteLine("Loading file " + fn);
        string file = Path.Join(savefile, fn + ".json");
        object? result = null;
        if (File.Exists(file))
        {
            result = JsonSerializer.Deserialize(File.ReadAllText(file), type);
        }
        if (result == null)
        {
            return nullDefault;
        }
        return result;
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
    private int parseInt(string inp)
    {
        int res;
        if (!int.TryParse(inp, out res))
        {
            return 0;
        }
        return res;
    }
    private string getCompressed(Tile tile)
    {
        return tile.type + tile.subtype + "=" + tile.prog.ToString() + "=" + tile.amount.ToString();
    }
    private Tile getTile(string tile)
    {
        string[] res = tile.Split("=");
        return new Tile(res[0][0], res[0].Substring(1), parseInt(res[1]), parseInt(res[2]));
    }
    private string[][] convertChunk(Tile[][] chunk)
    {
        string[][] res = new string[chunk.Length][];
        for (int x=0;x<res.Length;x++)
        {
            res[x] = new string[chunk[x].Length];
            for (int y=0;y<res[x].Length;y++)
            {
                res[x][y] = getCompressed(chunk[x][y]);
            }
        }
        return res;
    }
    private Tile[][] convertChunk(string[][] chunk)
    {
        Tile[][] res = new Tile[chunk.Length][];
        for (int x=0;x<res.Length;x++)
        {
            res[x] = new Tile[chunk[x].Length];
            for (int y=0;y<res[x].Length;y++)
            {
                res[x][y] = getTile(chunk[x][y]);
            }
        }
        return res;
    }
    private JsonSlot[] convertSlots(Slot[] slots)
    {
        JsonSlot[] res = new JsonSlot[slots.Length];
        for (int i=0;i<res.Length;i++)
        {
            res[i] = new JsonSlot(slots[i]);
        }
        return res;
    }
    private Slot[] convertSlots(JsonSlot[] slots)
    {
        Slot[] res = new Slot[slots.Length];
        for (int i=0;i<res.Length;i++)
        {
            res[i] = slots[i].getSlot();
        }
        return res;
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
        saveToFile("invdata", save, new InventoryData{data = convertSlots(fact.inventory.data)});
        MachineCursor machineCursor = new MachineCursor{
            macsk = Array.Empty<Point>(),
            macsv = Array.Empty<Machine>(),
            cursor = new JsonPoint(cursor),
            camera = new JsonPoint(camera)
        };
        machineCursor.applyDictionary(fact.machines);
        saveToFile("player", save, machineCursor);
        List<Point> regions = fact.getRegions(); // straightup stealing the concept of region files from minecraft
        for (int i=0;i<regions.Count;i++)
        {
            Region region = new Region{
                data = new string[regionLength][][],
                regionLocation = new JsonPoint(regions[i])
            };
            for (int p=0;p<regionLength;p++)
            {
                Point chunk = regions[i].getTransform(getPointIndex(p));
                region.data[p] = convertChunk(getChunk(fact, chunk));
            }
            saveToFile("region" + i.ToString(), save, region);
        }
    }
    public Slot[] LoadWorld(Factory fact)
    {
        int i=0;
        string save = fact.savefile;
        string fname = "region" + i.ToString();
        while (File.Exists(Path.Join(save, fname + ".json")))
        {
            Region region = (Region)loadFromFile(typeof(Region), fname, save, new Region
            {
                data = new string[regionLength][][],
                regionLocation = new JsonPoint()
            });
            for (int x=0;x<region.data.Length;x++)
            {
                if (region.data[x].Length > 0)
                {
                    Point chunkLoc = region.regionLocation.getPoint().getTransform(getPointIndex(x));
                    fact.placeChunk(chunkLoc, convertChunk(region.data[x]));
                }
            }
            i++;
            fname = "region" + i.ToString();
        }
        InventoryData id = (InventoryData)loadFromFile(typeof(InventoryData), "invdata", save, new InventoryData
        {
            data = new JsonSlot[Inventory.Length]
        });
        return convertSlots(id.data);
    }
    public Point[] LoadMachines(Factory fact)
    {
        MachineCursor deser = (MachineCursor)loadFromFile(typeof(MachineCursor), "player", fact.savefile, new MachineCursor
        {
            cursor = new JsonPoint(),
            camera = new JsonPoint(),
            macsk = Array.Empty<Point>(),
            macsv = Array.Empty<Machine>()
        });
        fact.machines = deser.returnMachines();
        return [deser.cursor.getPoint(), deser.camera.getPoint()];
    }
}

// 100% Ridiclous (looking) use of classes (or not depending on how judgy you feel like today)
public class JsonPoint
{
    public int x { get; set; }
    public int y { get; set; }
    public JsonPoint(Point point)
    {
        x = point.x;
        y = point.y;
    }
    public JsonPoint() {}
    public Point getPoint()
    {
        return new Point(x, y);
    }
}
public class JsonSlot
{
    public int num { get; set; }
    public string item { get; set; } = "";
    public JsonSlot(Slot slot)
    {
        item = slot.item;
        num = slot.num;
    }
    public JsonSlot() {}
    public Slot getSlot()
    {
        return new Slot(item, num);
    }
}

public class InventoryData
{ // for serization or however you spell it
    required public JsonSlot[] data { get; set; } = new JsonSlot[Inventory.Length];
}

public class Region
{ // jsonserialize REALLY wants me to use { get; set; } so im going to blindly paste it all over my data classes
    required public JsonPoint regionLocation { get; set; }
    required public string[][][] data { get; set; } // data = new string[FileManagement.regionLength][][]; // (0 length chunk=empty/not generated)
}
public class MachineCursor
{
    required public JsonPoint cursor { get; set; }
    required public JsonPoint camera { get; set; }
    required public Point[] macsk { get; set; }
    required public Machine[] macsv { get; set; }
    /*public MachineCursor(Dictionary<Point, Machine> macs, Point cursorp, Point camerap, Point ma) {
        cursor = cursorp;
        camera = camerap;
    }*/
    public void applyDictionary(Dictionary<Point, Machine> macs)
    {
        macsk = new Point[macs.Count];
        macsv = new Machine[macs.Count];
        macs.Keys.CopyTo(macsk, 0);
        for (int i=0;i<macsk.Length;i++)
        {
            macsv[i] = macs[macsk[i]];
        }
    }
    public MachineCursor() {
        macsk = Array.Empty<Point>();
        macsv = Array.Empty<Machine>();
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
}