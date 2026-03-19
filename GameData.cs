
using System.Text.Json;
using System.Text.Json.Serialization;

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
    JsonSerializerOptions jso = new JsonSerializerOptions(); // ignore, maybe use for something later
    public const string worldFolder = "worlds";
    public const int regionLength = Factory.regionArea * Factory.regionArea;
    private void saveToFile(string fn, string savefile, object toSer)
    {
        string file = Path.Join(worldFolder, savefile, fn + ".json");
        File.WriteAllText(file, JsonSerializer.Serialize(toSer, jso));
    }
    private object loadFromFile(Type type, string fn, string savefile, object nullDefault)
    {
        Console.WriteLine("Loading file " + fn);
        string file = Path.Join(worldFolder, savefile, fn + ".json");
        object? result = null;
        if (File.Exists(file))
        {
            string data = File.ReadAllText(file);
            result = JsonSerializer.Deserialize(data, type, jso);
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
    private string getCompressed(Tile tile)
    {
        if (tile.type == '`')
        {
            return "g"; // grass
        }
        if (tile.amount == 0 && tile.prog == 0)
        {
            return tile.type + tile.subtype;
        }
        return tile.type + tile.subtype + "=" + tile.prog.ToString() + "=" + tile.amount.ToString();
    }
    private Tile getTile(string tile, Factory fact)
    {
        if (tile == "g")
        {
            return new Tile('`', "", 0, 0);
        }
        string[] res = tile.Split("=");
        if (res.Length == 1)
        {
            return new Tile(res[0][0], res[0].Substring(1), 0, 0);
        }
        return new Tile(res[0][0], res[0].Substring(1), fact.parseInt(res[1]), fact.parseInt(res[2]));
    }
    private string[][] convertChunk(Tile[][] chunk)
    {
        string[][] res = new string[chunk.Length][];
        for (int x=0;x<res.Length;x++)
        {
            List<string> xto = new List<string>();
            int grassChain = 0;
            for (int y=0;y<chunk[x].Length;y++)
            {
                string ct = getCompressed(chunk[x][y]);
                if (ct == "g")
                {
                    grassChain++;
                } else
                {
                    if (grassChain == 1)
                    {
                        xto.Add("g");
                    } else if (grassChain > 1)
                    {
                        xto.Add("g" + grassChain.ToString());
                        
                    }
                    xto.Add(ct);
                    grassChain = 0;
                }
            }
            if (grassChain == 1)
            {
                xto.Add("g");
            } else if (grassChain > 1)
            {
                xto.Add("g" + grassChain.ToString());
            }
            res[x] = xto.ToArray();
        }
        return res;
    }
    private Tile[][] convertChunk(string[][] chunk, Factory fact)
    {
        Tile[][] res = new Tile[chunk.Length][];
        for (int x=0;x<res.Length;x++)
        {
            res[x] = new Tile[Factory.chunkSize];
            int cidx = 0;
            for (int y=0;y<res[x].Length;y++)
            {
                if (chunk[x][cidx][0] == 'g' && chunk[x][cidx].Length > 1)
                {
                    int num = fact.parseInt(chunk[x][cidx].Substring(1));
                    for (int i=0;i<num;i++)
                    {
                        res[x][y] = getTile("g", fact);
                        y++;
                    }
                    y--;
                } else
                {
                    res[x][y] = getTile(chunk[x][cidx], fact);
                }
                cidx++;
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
            macsd = new Dictionary<string, string>(),
            cursor = JPI.getJs(cursor),
            camera = JPI.getJs(camera),
            energyInNetwork = fact.energyInNetwork
        };
        machineCursor.applyDictionary(fact.machines);
        saveToFile("player", save, machineCursor);
        List<Point> regions = fact.getRegions(); // straightup stealing the concept of region files from minecraft
        for (int i=0;i<regions.Count;i++)
        {
            Region region = new Region{
                data = new string[regionLength][][],
                regionLocation = JPI.getJs(regions[i])
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
        while (File.Exists(Path.Join(worldFolder, save, fname + ".json")))
        {
            Region region = (Region)loadFromFile(typeof(Region), fname, save, new Region
            {
                data = new string[regionLength][][],
                regionLocation = JPI.getJs()
            });
            for (int x=0;x<region.data.Length;x++)
            {
                if (region.data[x].Length > 0)
                {
                    Point chunkLoc = JPI.getPoint(region.regionLocation).getTransform(getPointIndex(x));
                    fact.placeChunk(chunkLoc, convertChunk(region.data[x], fact));
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
            cursor = JPI.getJs(),
            camera = JPI.getJs(),
            macsd = new Dictionary<string, string>()
        });
        fact.machines = deser.returnMachines();
        fact.energyInNetwork = deser.energyInNetwork;
        return [JPI.getPoint(deser.cursor), JPI.getPoint(deser.camera)];
    }
}

// 100% Ridiclous (looking) use of classes (or not depending on how judgy you feel like today)
class JPI // JsonPointInterface / other things because i felt like it
{
    public static int parseInt(string inp)
    { // Yoink
        int res;
        if (!int.TryParse(inp, out res))
        {
            return 0;
        }
        return res;
    }
    public static string getJs(Point p)
    {
        return p.x.ToString() + "," + p.y.ToString();
    }
    public static string getJs()
    {
        return getJs(new Point());
    }
    public static string getJs(Point? p)
    {
        if (p == null)
        {
            return getJs();
        }
        return getJs((Point)p);
    }
    public static Point getPoint(string p)
    {
        string[] dec = p.Split(",");
        if (dec.Length < 2)
        {
            return new Point();
        }
        return new Point(parseInt(dec[0]), parseInt(dec[1]));
    }
    public static Point? getPointNull(string p, bool nil)
    {
        if (nil)
        {
            return null;
        }
        return getPoint(p);
    }
    public static string stringCharSubt(string s, int i, char c)
    {
        return s.Substring(0, i) + c + s.Substring(Math.Min(i+1, s.Length-1), Math.Max(s.Length-i-1, 0));
    }
    public static string convertMachine(Machine mac)
    {
        List<string> outs = new List<string>();
        outs.Add((mac.isFormed ? 1 : 0).ToString());
        string[] inputs = new string[mac.inputs.Count];
        for (int i=0;i<inputs.Length;i++)
        {
            inputs[i] = JPI.getJs(mac.inputs[i]);
        }
        outs.Add(String.Join('.', inputs)); // this will go well
        outs.Add(JPI.getJs(mac.output));
        outs.Add(JPI.getJs(mac.worldInteractor));
        outs.Add(JPI.getJs(mac.energyPort));
        string isnull = "000";
        if (mac.energyPort == null) isnull = JPI.stringCharSubt(isnull, 2, '1');
        if (mac.worldInteractor == null) isnull = JPI.stringCharSubt(isnull, 1, '1');
        if (mac.output == null) isnull = JPI.stringCharSubt(isnull, 0, '1');
        outs.Add(isnull);
        outs.Add((mac.runningRecipe ? 1 : 0).ToString());
        outs.Add(mac.selectedRecipe);
        outs.Add(mac.startedRecipe.ToString());
        outs.Add(mac.number.ToString());
        return String.Join('=', outs);
    }
    public static Machine convertMachine(string mach)
    {
        string[] mac = mach.Split('=');
        Machine macr = new Machine(); // mac Real
        /*
            0 public int formed { get; set; } = 0; // boolean
            1 public string[] inputs { get; set; } = [];
            2 public string output { get; set; } = "0,0";
            3 public string worldinteractor { get; set; } = "0,0";
            4 public string energyPort { get; set; } = "0,0";
            5 public string isnull { get; set; } = "111"; // output, wi, ep (000, 010 format)
            6 public int runningrecipe { get; set; } = 0; // boolean
            7 public string selectedrecipe { get; set; } = "";
            8 public int startedrecipe { get; set; } = 0;
            9 public int number { get; set; } = 0;
        */
        macr.isFormed = parseInt(mac[0]) == 1;
        macr.inputs = new List<Point>();
        foreach (string p in mac[1].Split("."))
        {
            macr.inputs.Add(getPoint(p));
        }
        string isnull = mac[5];
        macr.output = getPointNull(mac[2], isnull[0] == '1');
        macr.worldInteractor = JPI.getPointNull(mac[3], isnull[1] == '1');
        macr.energyPort = JPI.getPointNull(mac[4], isnull[2] == '1');
        macr.runningRecipe = parseInt(mac[6]) == 1; // Generally Perposturus Tenitis (Desicion)
        macr.selectedRecipe = mac[7];
        macr.startedRecipe = parseInt(mac[8]);
        macr.number = parseInt(mac[9]);
        return macr;
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
    required public string regionLocation { get; set; }
    required public string[][][] data { get; set; } // data = new string[FileManagement.regionLength][][]; // (0 length chunk=empty/not generated)
}
public class MachineCursor
{
    required public string cursor { get; set; }
    required public string camera { get; set; }
    public Dictionary<string, string> macsd { get; set; }
    public int energyInNetwork { get; set; }
    public void applyDictionary(Dictionary<Point, Machine> macs)
    {
        macsd = new Dictionary<string, string>();
        foreach (KeyValuePair<Point, Machine> kv in macs)
        {
            macsd[JPI.getJs(kv.Key)] = JPI.convertMachine(kv.Value);
        }
    }
    public MachineCursor() {
        macsd = new Dictionary<string, string>();
    }
    public Dictionary<Point, Machine> returnMachines()
    {
        Dictionary<Point, Machine> res = new Dictionary<Point, Machine>();
        foreach (KeyValuePair<string, string> kv in macsd)
        {
            res[JPI.getPoint(kv.Key)] = JPI.convertMachine(kv.Value);
        }
        return res;
    }
}