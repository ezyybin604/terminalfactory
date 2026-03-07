
/*using System.Xml;
using System.Xml.Serialization;*/
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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
    private void saveToFile(string fn, string savefile, object toSer)
    {
        string file = Path.Join(savefile, fn + ".json");
        File.WriteAllText(file, JsonSerializer.Serialize(toSer));
    }
    private object loadFromFile(Type type, string fn, string savefile, object nullDefault)
    {
        string file = Path.Join(savefile, fn + ".json");
        //object? result =  JsonSerializer.Deserialize(File.ReadAllText(file), JsonTypeInfo.CreateJsonTypeInfo(type, JsonSerializerOptions.Default));
        object? result = JsonSerializer.Deserialize(File.ReadAllText(file), type);
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
    private Point getPointIndex(int inp)
    {
        return new Point(inp%Factory.regionArea, (int)Math.Floor((double)(inp/Factory.regionArea)));
    }
    // for stuff
    public void SaveStuff(Factory fact, Point cursor, Point camera)
    {
        fact.inventory.fix();
        string save = fact.savefile;
        saveToFile("invdata", save, new InventoryData{data = fact.inventory.data});
        MachineCursor machineCursor = new MachineCursor{
            macsk = Array.Empty<Point>(),
            macsv = Array.Empty<Machine>(),
            cursor = cursor,
            camera = camera
        };
        machineCursor.applyDictionary(fact.machines);
        saveToFile("player", save, machineCursor);
        List<Point> regions = fact.getRegions(); // straightup stealing the concept of region files from minecraft
        for (int i=0;i<regions.Count;i++)
        {
            Region region = new Region{
                data = new string[regionLength][][]
            };
            region.regionLocation = regions[i];
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
        while (File.Exists(Path.Join(save, fname)))
        {
            Region region = (Region)loadFromFile(typeof(Region), fname, save, new Region
            {
                data = new string[regionLength][][]
            });
            for (int x=0;x<region.data.Length;x++)
            {
                if (region.data[x].Length > 0)
                {
                    Point chunkLoc = region.regionLocation.getTransform(getPointIndex(x));
                    fact.placeChunk(chunkLoc, convertChunk(region.data[x]));
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
        MachineCursor deser = (MachineCursor)loadFromFile(typeof(MachineCursor), "player", fact.savefile, new MachineCursor
        {
            macsk = Array.Empty<Point>(),
            macsv = Array.Empty<Machine>()
        });
        fact.machines = deser.returnMachines();
        return [deser.cursor, deser.camera];
    }
}

// 100% Ridiclous (looking) use of classes (or not depending on how judgy you feel like today)
public class InventoryData
{ // for serization or however you spell it
    required public Slot[] data { get; set; }
}

public class Region
{ // jsonserialize REALLY wants me to use { get; set; } so im going to blindly paste it all over my data classes
    public Point regionLocation { get; set; }
    required public string[][][] data { get; set; } // data = new string[FileManagement.regionLength][][]; // (0 length chunk=empty/not generated)
}
public class MachineCursor
{
    public Point cursor { get; set; }
    public Point camera { get; set; }
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