
using E604terminalfactory;
using SDL3;

namespace gameRunner;

public class GameGraphics
{
    public GameGraphics(Game igame)
    {
        game = igame;
    }
    public void init()
    {
        spritesheet = SDL.CreateTextureFromSurface(WindowHandler.renderer, Image.Load("data/textures/tileset.png"));
        if (spritesheet == WindowHandler.NULL) SDL.LogError(SDL.LogCategory.Video, SDL.GetError());
        spdm = SDLTools.Cast(SDLTools.DividePoint(WindowHandler.getTextureSize(spritesheet), shTileS));
        SDL.SetTextureScaleMode(spritesheet, SDL.ScaleMode.PixelArt);
    }
    nint spritesheet;
    const float camspeedc = 10;
    public const int shTileS = 32; // size in spritesheet
    public const int tileSize = 32; // size in pixels
    public SDL.FPoint worldscroll = WindowHandler.createPoint(0, 0);
    public Game game;
    public SDL.Point spdm; // spritesheet dimensions
    public Dictionary<int, Tile[]> worldisplay = new Dictionary<int, Tile[]>();
    Point startpn = new Point();
    public void sendTiles(Point startp, Tile[] tiles)
    {
        if (!startpn.Equals(startp)) startpn = startp;
        if (worldisplay.ContainsKey(startp.y))
        {
            worldisplay.Remove(startp.y);
        }
        worldisplay.Add(startp.y, tiles);
    }
    public void drawHeader()
    { // add splash
        for (int i=0;i<game.topbar.header.Length;i++)
        {
            string[] intp = game.topbar.header[i].Split("|");
            string mode = intp[0];

            bool alternate = true;
            string font = "consbold_20";
            if (intp.Length > 1)
            {
                switch (mode)
                {
                    case "logo":
                        WindowHandler.writeText(intp[1], 15, 15+(30*i), font, WindowHandler.colors["titleColor"], Algn.leftupper);
                        WindowHandler.writeText(intp[2],
                            25+WindowHandler.getStringLength(font, intp[1]).X, 15+(30*i),
                            font, SDLTools.Invert(WindowHandler.colors["titleColor"]), Algn.leftupper
                        );
                        break;
                    default:
                        alternate = false;
                        if (WindowHandler.fonts.Keys.Contains(mode))
                        {
                            font = mode;
                        }
                        break;
                }
            }
            if (!alternate)
            {
                WindowHandler.writeText(TopBar.CleanHeader(game.topbar.header[i]), 15, 15+(30*i), font, WindowHandler.black, Algn.leftupper);
            }
        }
    }
    public void drawTile(Tile tile, int x, int y, int spro=0) // sprite offset = 1 when machine on
    {
        string keyt = tile.type + "." + tile.subtype;
        string val = game.factory.gd.getFromKey("tileTileset", keyt);
        if (val == "")
        {
            keyt = tile.type.ToString();
            val = game.factory.gd.getFromKey("tileTileset", keyt);
        }
        if (val == "")
        {
            val = "23";
        }
        string[] sprs = val.Split(","); // n,n < sprites
        SDL.FRect dest = WindowHandler.createRectF(x, y, tileSize, tileSize);
        foreach (string s in sprs)
        {
            int sp = JPI.parseInt(s)-1;
            sp += spro;
            Point stpos = new Point(sp%spdm.X, sp/spdm.X);
            stpos.multiply(shTileS);
            SDL.FRect clip = WindowHandler.createRectF(stpos, shTileS, shTileS);
            SDL.RenderTexture(WindowHandler.renderer, spritesheet, clip, dest);
        }
    }
    public void cameraController()
    {
        SDL.FPoint camspeed = WindowHandler.createPoint(0, 0);
        if (WindowHandler.getKeyPressed(SDL.Keycode.A))
        {
            camspeed.X -= 1;
        }
        if (WindowHandler.getKeyPressed(SDL.Keycode.W))
        {
            camspeed.Y -= 1;
        }
        if (WindowHandler.getKeyPressed(SDL.Keycode.D))
        {
            camspeed.X += 1;
        }
        if (WindowHandler.getKeyPressed(SDL.Keycode.S))
        {
            camspeed.Y += 1;
        }
        float speed = camspeedc;
        if (camspeed.X != 0 && camspeed.Y != 0)
        {
            speed = (float)WindowHandler.diagSpeed(camspeedc);
        }
        camspeed.X *= speed;
        camspeed.Y *= speed;
        worldscroll.X += camspeed.X;
        worldscroll.Y += camspeed.Y;
        bool changed = false;
        if (Math.Abs(worldscroll.Y) > tileSize)
        {
            int d = Point.neutralize((int)worldscroll.Y);
            worldscroll.Y -= d*tileSize;
            game.scroll.y += d;
            changed = true;
        }
        if (Math.Abs(worldscroll.X) > tileSize)
        {
            int d = Point.neutralize((int)worldscroll.X);
            worldscroll.X -= d*tileSize;
            game.scroll.x += d;
            changed = true;
        }
        if (game.scroll.x == 0) worldscroll.X = Math.Max(0, worldscroll.X);
        if (changed)
        {
            game.generateNeeded();
            game.displayStuff();
        }
    }
    
    public void drawWorld()
    {
        int brh = game.cusc.getWindowSize(WindowSizes.BOARD).y;
        int dooffset = 0;
        if (game.scroll.x > 0) dooffset++;
        for (int i=-1;i<brh;i++)
        {
            if (worldisplay.ContainsKey(i+game.scroll.y))
            {
                Tile[] tiles = worldisplay[i+game.scroll.y];
                for (int x=0;x<tiles.Length;x++)
                { // add offsets for these so scroll
                    SDL.Point pt = SDLTools.Cast(WindowHandler.createPoint((x*tileSize)-(int)worldscroll.X-(tileSize*dooffset), (i*tileSize)-(int)worldscroll.Y));
                    drawTile(tiles[x], pt.X, pt.Y);
                    if (tiles[x].type == 'M')
                    {
                        Point yes = new Point(x+game.scroll.x-1, i+game.scroll.y);
                        bool uhh = game.factory.machines[yes].isFormed;
                        SDL.FRect iss = WindowHandler.createRectF(new Point(pt), 5, 5);
                        if (uhh)
                        {
                            WindowHandler.drawRect(iss, WindowHandler.colors["green"]);
                        } else
                        {
                            WindowHandler.drawRect(iss, WindowHandler.colors["red"]);
                        }
                    }
                }
            }
        }
    }
    public Point cursorLimiter(Point cursor)
    {
        Point newCursor = cursor;
        Point inbetween = new Point(newCursor.x, game.cursor.y);
        List<Point> check = FTutorial.getPathList([game.cursor, inbetween]);
        int prevx = game.cursor.x;
        bool setNewX = false;
        foreach (Point pt in check)
        {
            char ttype = game.factory.giveMeTheTile(pt).type;
            if (ttype == 't')
            {
                setNewX = true;
            } else if (!setNewX)
            {
                prevx = pt.x;
            }
        }
        if (setNewX)
        {
            newCursor.x = prevx;
        }
        bool setNewY = false;
        check = FTutorial.getPathList([new Point(newCursor.x, game.cursor.y), newCursor]);
        int prevy = game.cursor.y;
        foreach (Point pt in check)
        {
            char ttype = game.factory.giveMeTheTile(pt).type;
            if (ttype == 't')
            {
                setNewY = true;
            } else if (!setNewY)
            {
                prevy = pt.y;
            }
        }
        if (setNewY)
        {
            newCursor.y = prevy;
        }
        return newCursor;
    }
}
