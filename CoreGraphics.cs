
using System.Security.Cryptography.X509Certificates;
using E604terminalfactory;
using SDL3;

namespace gameRunner;

// sdl stuff mainly instead of corewrite handling terminal

public class WindowHandler
{
    static Dictionary<string, nint> fonts = new Dictionary<string, nint>();
    public required TileConsole tc;
    public static void initFont(string font, string file, float size)
    {
        string id = font + "_" + ((int)size).ToString();
        fonts.Add(id, TTF.OpenFont(Path.Join("data/fonts", file), size));
        //Console.WriteLine(String.Format("Initalized font {0} in size {1} as {2}", font, size, id));
        if (fonts[id] == 0)
        {
            SDL.LogError(SDL.LogCategory.System, $"Font could not initalize: {SDL.GetError()}");
        }
    }
    public static void initFonts(string font, string file, float[] sizes)
    {
        foreach (float size in sizes)
        {
            initFont(font, file, size);
        }
    }
    // deg range: 0-360
    public const float toRadians = MathF.PI/180;
    public const int circleDetail = 40;
    public SDL.FPoint[] generateCircle(int radius, int degs, int degf, SDL.FPoint offset=new SDL.FPoint(), int extraPoint=0, int points=circleDetail)
    {
        if (degf < degs) return [];
        int afterd = degf-degs;
        SDL.FPoint[] res = new SDL.FPoint[points+extraPoint];
        for (int i=0;i<points+extraPoint;i++)
        {
            double deg = degs+(i*afterd/points);
            deg *= toRadians;
            res[i] = new SDL.FPoint
            {
                X = (float)(Math.Cos(deg) * radius) + offset.X,
                Y = (float)(Math.Sin(deg) * radius) + offset.Y
            };
        }
        return res;
    }
    public static SDL.FPoint getCursorPoint()
    {
        float x, y;
        SDL.GetMouseState(out x, out y);
        return new SDL.FPoint{X = x, Y = y};
    }
    public struct Line
    {
        public float xmin;
        public float xmax;
        public Line expandLine(float x)
        {
            xmax = Math.Max(xmax, x);
            xmin = Math.Min(xmin, x);
            return this;
        }
    }
    GameData gd;
    SDL.Point spdm; // spritesheet dimensions
    nint spritesheet;
    public void drawTile(Tile tile, int x, int y, int spro=0) // sprite offset = 1 when machine on
    {
        string keyt = tile.type + "." + tile.subtype;
        string val = gd.getFromKey("tileTileset", keyt);
        if (val == "")
        {
            keyt = tile.type.ToString();
            val = gd.getFromKey("tileTileset", keyt);
        }
        if (val == "")
        {
            val = "23";
        }
        string[] sprs = val.Split(","); // n,n < sprites
        SDL.FRect dest = createRectF(x, y, tileSize, tileSize);
        foreach (string s in sprs)
        {
            // UNFINISHED
            int sp = JPI.parseInt(s)-1;
            Point stpos = new Point(sp%spdm.X, (int)Math.Floor((double)(sp/spdm.Y)));
            stpos.multiply(shTileS);
            SDL.FRect clip = createRectF(stpos, shTileS, shTileS);
            SDL.RenderTexture(renderer, spritesheet, clip, dest);
        }
    }
    public void fillCircle(SDL.FPoint[] circle, SDL.Color col)
    {
        int ymax = int.MinValue;
        int ymin = int.MaxValue;
        Dictionary<int, Line> lines = new Dictionary<int, Line>();
        for (int i=0;i<circle.Length;i++)
        {
            int y = (int)Math.Round(circle[i].Y);
            ymax = Math.Max(y, ymax);
            ymin = Math.Min(y, ymin);
            if (lines.ContainsKey(y))
            {
                lines[y] = lines[y].expandLine(circle[i].X);
            } else
            {
                lines.Add(y, new Line
                {
                    xmin = circle[i].X,
                    xmax = circle[i].X
                });
            }
        }
        int ydif = ymax - ymin;
        SDL.SetRenderDrawColor(renderer, col.R, col.G, col.B, col.A);
        int currentline = 0;
        int[] keys = lines.Keys.ToArray();
        keys.Sort();
        for (int i=0;i<ydif;i++)
        {
            int truey = ymin+i;
            Line line = lines[keys[currentline]];
            SDL.RenderLine(renderer, line.xmin, truey, line.xmax, truey);
            if (lines.ContainsKey(truey))
            {
                currentline++;
            }
        }
    }
    public void SetRenderDrawColor(SDL.Color col)
    {
        SDL.SetRenderDrawColor(renderer, col.R, col.G, col.B, col.A);
    }
    public void SetRenderDrawColor(SDL.FColor col)
    {
        SDL.SetRenderDrawColor(renderer, (byte)col.R, (byte)col.G, (byte)col.B, (byte)col.A);
    }
    public SDL.Point getStringLength(string font, string text)
    {
        int x, y;
        TTF.GetStringSize(fonts[font], text, (nuint)text.Length, out x, out y);
        return new SDL.Point{X=x, Y=y};
    }
    SDL.PixelFormat defaultFormat = SDL.PixelFormat.Unknown;
    public void drawRect(SDL.FRect rect, SDL.Color col, SDL.Color? edgecol=null, int linecurve=0, int lineScale=1, nint? copytexture=null)
    {
        if (copytexture != null)
        {
            if (edgecol == null)
            {
                // no alpha
                SDL.FillSurfaceRect((nint)copytexture, SDLTools.Cast(rect), SDL.MapSurfaceRGBA((nint)copytexture, col.R, col.G, col.B, col.A));
            }
            return;
        }
        SDL.SetRenderScale(renderer, lineScale, lineScale);
        SDL.FPoint[] outline = [];
        if (linecurve > 0)
        {
            // outline not converted
            List<SDL.FPoint> outlinenc = SDLTools.DividePoints([
                .. generateCircle(linecurve, 0, 90, new SDL.FPoint{X = rect.X+rect.W-linecurve, Y=rect.Y+rect.H-linecurve}),
                .. generateCircle(linecurve, 90, 180, new SDL.FPoint{X = rect.X+linecurve, Y=rect.Y+rect.H-linecurve}, 1),
                .. generateCircle(linecurve, 180, 270, new SDL.FPoint{X = rect.X+linecurve, Y=rect.Y+linecurve}),
                .. generateCircle(linecurve, 270, 360, new SDL.FPoint{X = rect.X+rect.W-linecurve, Y=rect.Y+linecurve}, 1),
            ], lineScale).ToList();
            outlinenc.Add(outlinenc[0]);
            outline = outlinenc.ToArray();
            fillCircle(outline, col);
        } else
        {
            SetRenderDrawColor(col);
            SDL.RenderFillRect(renderer, SDLTools.DivideRect(rect, lineScale));
        }
        if (edgecol != null)
        {
            SDL.Color ce = (SDL.Color)edgecol;
            SDL.SetRenderDrawColor(renderer, ce.R, ce.G, ce.B, ce.A);
            if (linecurve > 0)
            { // maybe add whatever anti aliasing is to outline
                SDL.RenderLines(renderer, outline, outline.Length);
            } else
            {
                SDL.RenderRect(renderer, SDLTools.DivideRect(rect, lineScale));
            }
        }
        SDL.SetRenderScale(renderer, 1, 1);
    }
    public static SDL.Color createColor(byte r, byte g, byte b, byte a=(byte)SDL.AlphaOpaque) {
        return new SDL.Color { R = r, G = g, B = b, A = a };
    }
    public static SDL.Color createColor(byte un, byte a=(byte)SDL.AlphaOpaque) // uno/un value
    {
        return createColor(un, un, un, a);
    }
    public static SDL.FRect createRectF(float x, float y, float w, float h)
    {
        return new SDL.FRect { X = x, Y = y, W = w, H = h };
    }
    public static SDL.Rect createRect(int x, int y, int w, int h)
    {
        return new SDL.Rect { X = x, Y = y, W = w, H = h };
    }
    public static SDL.FRect createRectF(Point pt, int w, int h)
    {
        return new SDL.FRect { X = pt.x, Y = pt.y, W = w, H = h };
    }
    public static SDL.FPoint createPoint(float x, float y)
    {
        return new SDL.FPoint { X = x, Y = y };
    }
    public const nint NULL = 0;
    public nint renderer;
    nint window;
    SDL.FRect textRect;
    public Point windowSize;
    Dictionary<int, UIElement> ui = new Dictionary<int, UIElement>();
    public static SDL.FPoint cursor;
    public double deltaTime = 0;
    public static int? selected = null;
    private bool acceptingInput = false;
    public int lastkeyp = 0;
    public const int shTileS = 32;
    public const int tileSize = 64;
    private void changeInputAcceptance(bool newstat)
    {
        if (newstat != acceptingInput)
        {
            acceptingInput = newstat;
            if (acceptingInput)
            {
                SDL.StartTextInput(window);
            } else
            {
                SDL.StopTextInput(window);
            }
        }
    }
    public static Point getWindowSize(nint window)
    {
        int w, h;
        SDL.GetWindowSize(window, out w, out h);
        return new Point(w, h);
    }
    public static float align(int algn, float p, int size)
    {
        return p-(size/2*algn);
    }
    public void writeText(string c, float x, float y, string font, SDL.Color fg, Algn alignment=Algn.leftupper, SDL.FRect? src=null, nint? copytexture=null) {
        if (c.Length == 0) return;
        nint surface = TTF.RenderTextBlended(fonts[font], c, (uint)c.Length, fg);
        if (surface == NULL)
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("Font Surface could not display: {0}", SDL.GetError()));
            return;
        }
        SDL.Surface surf = PointerTools.GetSurface(surface);
        textRect.W = surf.Width;
        textRect.H = surf.Height;
        if (src != null)
        {
            SDL.FRect rsrc = (SDL.FRect)src;
            textRect.W = MathF.Min(textRect.W, MathF.Max(0, MathF.Min(rsrc.W, textRect.W-rsrc.X)));
        }
        int[] arl = SDLTools.Get(alignment);
        textRect.X = align(arl[0], x, surf.Width);
        textRect.Y = align(arl[1], y, surf.Height);
        if (copytexture != null)
        {
            nint surfblit = (nint)copytexture;
             if (src == null)
            {
                SDL.BlitSurface(surface, NULL, surfblit, SDLTools.Cast(textRect));
            } else
            {
                SDL.FRect rsrc = (SDL.FRect)src;
                SDL.BlitSurface(surface, new SDL.Rect
                {
                    X = (int)rsrc.X, W = (int)Math.Min(rsrc.W, textRect.W),
                    Y = 0, H = (int)textRect.H
                }, surfblit, SDLTools.Cast(textRect));
            }
        } else
        {
            nint texture = SDL.CreateTextureFromSurface(renderer, surface);
            if (src == null)
            {
                SDL.RenderTexture(renderer, texture, NULL, textRect);
            } else
            {
                SDL.FRect rsrc = (SDL.FRect)src;
                rsrc = new SDL.FRect {
                   X = (int)rsrc.X, W = Math.Min(rsrc.W, textRect.W),
                   Y = 0, H = (int)textRect.H
                };
                SDL.RenderTexture(renderer, texture, rsrc, textRect);
            }
        }
        SDL.DestroySurface(surface);
    }
    public static ConsoleKey GetConsoleKey(char c)
    {
        ConsoleKey ck;
        Enum.TryParse(c.ToString(), out ck);
        return ck;
    }
    private void sendKeyEvent(string[] s)
    {
        if (tc.theGame == null) return;
        string evt = tc.theGame.scene + "-" + string.Join('-', s);
        string sc = tc.theGame.factory.gd.getFromKey("keyEvents", evt);
        if (sc != "")
        {
            char c = sc[0];
            ConsoleKeyInfo key = new ConsoleKeyInfo(c, GetConsoleKey(c), false, false, false);
            if (sc.Length > 1)
            {
                switch (sc)
                {
                    case "enter":
                        key = new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false);
                        break;
                    default:
                        break;
                }
            }
            Game.readkeylog.Add(key);
        }
    }
    private void sendKeyEvent(string s)
    {
        sendKeyEvent([s]);
    }
    /*private void sendKeyEvent(List<string>[] s)
    {
        // try every combo
    }*/
    public static SDL.Color black = createColor(0);
    public static SDL.Color white = createColor(255);
    public static bool sceneUpdated = false;
    Dictionary<int, string> clickmaps = new Dictionary<int, string>();
    Dictionary<string, SDL.Color> colors = new Dictionary<string, SDL.Color>();
    public WindowHandler()
    {
        gd = new GameData();
        clickmaps.Add(1, "lc");
        clickmaps.Add(2, "rc");
        clickmaps.Add(3, "mc");
        colors.Add("titleColor", createColor(255, 128, 0));
        colors.Add("blackTransparent", createColor(0, SDL.AlphaTransparent));
        colors.Add("red", createColor(255, 0, 0));
        colors.Add("green", createColor(0, 255, 0));
    }
    private void drawHeader()
    {
        if (tc.theGame == null) return;
        Game game = tc.theGame;
        for (int i=0;i<game.topbar.header.Length;i++)
        {
            if (game.topbar.header[i] == "TERMINALFACTORY")
            {
                writeText("TERMINAL", 15, 15+(30*i), "consbold_20", colors["titleColor"], Algn.leftupper);
                writeText("FACTORY", 25+getStringLength("consbold_20", "TERMINAL").X, 15+(30*i), "consbold_20", SDLTools.Invert(colors["titleColor"]), Algn.leftupper);
            } else
            {
                writeText(game.topbar.header[i], 15, 15+(30*i), "consbold_20", black, Algn.leftupper);
            }
        }
    }
    // Yoink start from https://discourse.libsdl.org/t/sdl2-color-gradient/25408/4 (modified)
    void drawHorizontalGradientBox(int x, int y, int w, int h, float steps, SDL.Color c1, SDL.Color c2)
    {
        float yt = y;
        float rt = c1.R;
        float gt = c1.G;
        float bt = c1.B;
        float at = c1.A;
        
        // Changes in each attribute
        float ys = h/steps;
        float rs = (c2.R - c1.R)/steps;
        float gs = (c2.G - c1.G)/steps;
        float bs = (c2.B - c1.B)/steps;
        float asv = (c2.A - c1.A)/steps;

        for (int i=0;i<steps;i++)
        {
            // Create an horizontal rectangle sliced by the number of steps
            SDL.FRect rect = createRectF(x, (int)yt, w, Math.Max(ys, 1));

            // Sets the rectangle color based on iteration
            SDL.SetRenderDrawColor(renderer, (byte)rt, (byte)gt, (byte)bt, (byte)at);
            SDL.RenderFillRect(renderer, rect);

            // Update colors and positions
            yt += ys;
            rt += rs;
            gt += gs;
            bt += bs;
            at += asv;
        }
    }
    public SDL.FPoint getTextureSize(nint texture)
    {
        if (!SDL.GetTextureSize(texture, out float x, out float y)) SDL.LogError(SDL.LogCategory.Video, SDL.GetError());
        return createPoint(x, y);
    }
    string curlayout = "none";
    private void changeUILayout(string name, UIElement[] elements)
    {
        if (name == curlayout) return;
        curlayout = name;
        ui.Clear();
        for (int i=0;i<elements.Length;i++)
        {
            ui[elements[i].id] = elements[i];
        }
    } // yoink end
    [STAThread]
    public void Loop()
    {
        if (tc.theGame != null)
        {
            gd = tc.theGame.factory.gd;
        }
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("SDL could not initialize: {0}", SDL.GetError()));
            return;
        }
        if (!TTF.Init())
        {
            SDL.LogError(SDL.LogCategory.System, $"SDL_TTF could not initialize: {SDL.GetError()}");
            return;
        }
        if (!SDL.CreateWindowAndRenderer("terminalfactory", 1200, 600, 0, out window, out renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"Error creating window and rendering: {SDL.GetError()}");
            return;
        }
        if (!SDL.SetHint(SDL.Hints.FramebufferAcceleration, "0"))
        {
            SDL.LogError(SDL.LogCategory.System, SDL.GetError());
        }
        SDL.SetWindowResizable(window, true);
        if (!SDL.SetWindowIcon(window, Image.Load("data/textures/icon.png")))
        {
            SDL.LogError(SDL.LogCategory.Render, $"Error setting icon: {SDL.GetError()}");
        }
        SDL.SetWindowMinimumSize(window, 800, 400);
        
        spritesheet = SDL.CreateTextureFromSurface(renderer, Image.Load("data/textures/tileset.png"));
        if (spritesheet == NULL) SDL.LogError(SDL.LogCategory.Video, SDL.GetError());
        spdm = SDLTools.Cast(SDLTools.DividePoint(getTextureSize(spritesheet), shTileS));
        SDL.SetTextureScaleMode(spritesheet, SDL.ScaleMode.PixelArt);

        windowSize = getWindowSize(window);
        initFonts("consbold", "consbold.ttf", [20, 30]); // consbold_30
        initFonts("sans", "opensans.ttf", [20, 8, 15, 25, 40]); // sans_ 20,8,15
        SDL.Color grey = createColor(205);
        SDL.Color darkergrey = createColor(150);
        SDL.Color darkgrey = createColor(180);

        // highlight=high
        SDL.FRect prevhigh = createRectF(0, 0, 0, 0);
        SDL.FRect newhigh = createRectF(0, 0, 0, 0);
        const float timehigh = 0.3f;
        float proghigh = timehigh;
        /*ui.Add(0, new UIElement
        {
            id = 0,
            window = this,
            type = "input",
            contents = "",
            rect = createRect((windowSize.x/2)-100, 150, 200, 50),
            // button color, outline color, text color, highlight tint, selected tint, selecting tint
            color = [createColor(66, 135, 245), black, black, grey, darkergrey, darkgrey],
            font = "sans_15",
            transition_time = 0.12f
        });*/
        SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Blend);
        ulong lastTick;
        int nsDelay = 1000/30;
        int nearestSleep = 0;
        bool loop = true;
        bool validclick = false;
        bool menu = true;

        //defaultFormat = SDL.GetWindowPixelFormat(window);
        defaultFormat = SDL.PixelFormat.RGBA8888;
        SDL.FRect lowerRect = createRectF(0, 0, 0, 0);
        ulong NOW = SDL.GetPerformanceCounter();
        ulong LAST;
        while (loop)
        {
            LAST = NOW;
            NOW = SDL.GetPerformanceCounter();
            deltaTime = (NOW - LAST) / (double)SDL.GetPerformanceFrequency();

            lastTick = SDL.GetTicks();
            bool clicked = false;
            bool inpacc = acceptingInput;
            while (SDL.PollEvent(out SDL.Event e))
            {
                switch ((SDL.EventType)e.Type)
                {
                    case SDL.EventType.Quit:
                        loop = false;
                        break;
                    case SDL.EventType.WindowResized:
                        windowSize = getWindowSize(window);
                        break;
                    case SDL.EventType.MouseButtonDown:
                        if (e.Button.Button == 1)
                        {
                            clicked = true;
                            selected = null;
                            inpacc = false;
                        }
                        string clickb = clickmaps[e.Button.Button];
                        if (validclick)
                        {
                            sendKeyEvent([clickb, "valid"]);
                            sendKeyEvent(["ac", "valid"]);
                        }
                        sendKeyEvent(clickb);
                        sendKeyEvent("ac");
                        break;
                    case SDL.EventType.KeyDown:
                        switch (e.Key.Scancode)
                        {
                            case SDL.Scancode.Backspace:
                                if (selected != null && ui[(int)selected].cursorpos > 0)
                                {
                                    int seld = (int)selected;
                                    ui[seld].contents = SDLTools.RemoveChars(ui[seld].contents, ui[seld].cursorpos-1);
                                    ui[seld].cursorpos--;
                                    ui[seld].lastInput = "backspace";
                                }
                                break;
                            case SDL.Scancode.Left:
                                if (selected != null)
                                {
                                    int seld = (int)selected;
                                    ui[seld].cursorpos--;
                                    ui[seld].cursorpos = Math.Max(0, ui[seld].cursorpos);
                                    ui[seld].lastInput = "left";
                                }
                                break;
                            case SDL.Scancode.Right:
                                if (selected != null)
                                {
                                    int seld = (int)selected;
                                    ui[seld].cursorpos++;
                                    ui[seld].cursorpos = Math.Min(ui[seld].contents.Length, ui[seld].cursorpos);
                                    ui[seld].lastInput = "right";
                                }
                                break;
                            case SDL.Scancode.Return: case SDL.Scancode.KpEnter:
                                if (tc.theGame != null)
                                {
                                    Game game = tc.theGame;
                                    sendKeyEvent("return");
                                    if (selected != null) sendKeyEvent(["return", "valid"]);
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case SDL.EventType.TextInput:
                        if (acceptingInput && selected != null)
                        {
                            int seld = (int)selected;
                            char yes = PointerTools.GetChar(e.Edit.Text);
                            ui[seld].contents = ui[seld].contents.Insert(ui[seld].cursorpos, yes.ToString());
                            ui[seld].cursorpos++;
                            ui[seld].lastInput = yes.ToString();
                        }
                        break;
                    default:
                        break;
                }
            }
            cursor = getCursorPoint();
            SDL.SetRenderDrawColor(renderer, 255, 255, 255, 0);
            SDL.RenderClear(renderer);
            if (tc.theGame != null) writeText(tc.theGame.topbar.tipPriority.ToString() + ", " + nearestSleep.ToString(), 0, 0, "sans_8", black);
            foreach (int eidx in ui.Keys.ToArray())
            {
                UIElement element = ui[eidx];
                element.Draw();
                if (element.col_idx != 4 || element.time == element.transition_time || clicked)
                {
                    int finalc = 0;
                    if (element.id == selected)
                    {
                        finalc = 5;
                    }
                    if (element.hovering)
                    {
                        finalc = 3;
                        if (clicked)
                        {
                            finalc = 4;
                            selected = element.id;
                            if (element.type == "input")
                            {
                                if (!acceptingInput) element.cursorpos = element.contents.Length;
                                inpacc = true;
                            }
                        }
                    }
                    element.changeColor(finalc);
                }
            }
            changeInputAcceptance(inpacc);
            if (tc.theGame != null) tc.changeMode(tc.theGame.factory.gd.getFromKey("modeMaps", tc.theGame.scene));
            if (tc.misctext.ContainsKey("vers") && menu)
            {
                writeText(tc.misctext["vers"], 10, windowSize.y-10, "sans_20", black, Algn.leftlower);
            }
            if (tc.theGame != null)
            {
                // also what does clickkeys do or mean i forgor
                //List<string> clickkeys = new List<string>(); // should ai be renanamed to ahc (artifical halluciantor creator)
                Game game = tc.theGame;
                validclick = true;
                switch (tc.mode)
                {
                    case "prompt":
                        changeUILayout("prompt", [
                            new UIElement{
                                id = 0,
                                window = this,
                                type = "input",
                                contents = "",
                                rect = createRectF(20, 45+(30*game.topbar.header.Length), 200, 50),
                                // button color, outline color, text color, highlight tint, selected tint, selecting tint
                                color = [createColor(255, 249, 135), black, black, grey, darkergrey, darkgrey],
                                font = "sans_15"
                            }
                        ]);
                        drawHeader();
                        game.menus["prompt"] = [ui[0].contents];
                        break;
                    case "menu":
                        lowerRect = createRectF(10, 45+(30*game.topbar.header.Length), windowSize.x-20, windowSize.y-55);
                        lowerRect.H -= lowerRect.Y;
                        drawHeader();
                        drawRect(lowerRect, grey, black);
                        nint menusurf = SDL.CreateSurface((int)lowerRect.W, (int)lowerRect.H, defaultFormat); // SDL.Surface
                        // menusurf start
                        if (!game.menus["nohighlight"].Contains(game.scene))
                        {
                            if (timehigh == proghigh)
                            {
                                drawRect(newhigh, darkergrey, null, 0, 1, menusurf);
                            } else
                            {
                                drawRect(SDLTools.Lerp(prevhigh, newhigh, proghigh/timehigh), darkergrey, null, 0, 1, menusurf);
                                proghigh += (float)deltaTime;
                                if (proghigh > timehigh)
                                {
                                    proghigh = timehigh;
                                }
                            }
                        }
                        SDL.FRect colliderect;
                        for (int i=0;i<game.menus[game.scene].Length;i++)
                        {
                            string itm = game.menus[game.scene][i];
                            colliderect = createRectF(6, 11+(i*25), getStringLength("sans_15", itm).X+12, 20);
                            if (SDL.PointInRectFloat(cursor, SDLTools.Transform(colliderect, createPoint(lowerRect.X, lowerRect.Y))))
                            {
                                // set topbar.menuselection here
                                if (game.topbar.menuSelection != i) proghigh = 0;
                                game.topbar.menuSelection = i;
                                validclick = true;
                            }
                            if (game.topbar.menuSelection == i)
                            {
                                if (!prevhigh.Equals(colliderect))
                                {
                                    prevhigh = SDLTools.Lerp(prevhigh, newhigh, proghigh/timehigh);
                                    newhigh = SDLTools.Copy(colliderect);
                                }
                            }
                            writeText(itm, 10, 10+(i*25), "sans_15", black, copytexture:menusurf);
                        }
                        // menusurf end
                        SDL.RenderTexture(renderer, SDL.CreateTextureFromSurface(renderer, menusurf), NULL, lowerRect);
                        break;
                    case "world":
                        menu = false;
                        drawTile(new Tile("i.diamond"), 10, 10);
                        SDL.FRect tilex = createRectF(tileSize, tileSize, tileSize, tileSize);
                        break;
                }
                if (tc.theGame.topbar.tipPriority > 1)
                {
                    string flavortext = tc.theGame.topbar.tipt;
                    char modifer = '\0';
                    if (flavortext[0] == '/')
                    {
                        modifer = flavortext[1];
                        flavortext = flavortext.Substring(2);
                    }
                    SDL.Color color = white;
                    switch (modifer)
                    {
                        case 'd':
                            color = colors["red"];
                            break;
                        case 'g':
                            color = colors["green"];
                            break;
                        default:
                            break;
                    }
                    drawHorizontalGradientBox(0, 0, windowSize.x, 50, 25, black, colors["blackTransparent"]);
                    writeText(flavortext, 5, 5, "sans_25", color);
                }
            }
            SDL.RenderPresent(renderer);
            nearestSleep = (int)(SDL.GetTicks()-lastTick);
            Thread.Sleep(Math.Max(1, nsDelay-nearestSleep));
        }
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        TTF.Quit();
        SDL.Quit();
    }
}