
namespace terminalfactory;

// type = # of !s
class GameData
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