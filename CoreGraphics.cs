
using E604terminalfactory;
using SDL3;

namespace gameRunner;

// sdl stuff mainly instead of corewrite handling terminal
// also rule: ONLY file w/unsafe functions

public class WindowHandler
{
    static Dictionary<string, nint> fonts = new Dictionary<string, nint>();
    public required TileConsole tc;
    public static void initFont(string font, string file, float size)
    {
        string id = font + "_" + ((int)size).ToString();
        fonts.Add(id, TTF.OpenFont("data/" + file, size));
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
    public void drawRect(SDL.FRect rect, SDL.Color col, SDL.Color? edgecol=null, int linecurve=0, int lineScale=1)
    {
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
    public static SDL.Color createColor(byte un) // uno/un value
    {
        return createColor(un, un, un);
    }
    public static SDL.FRect createRect(float x, float y, float w, float h)
    {
        return new SDL.FRect { X = x, Y = y, W = w, H = h };
    }
    public const nint NULL = 0;
    public nint renderer;
    nint window;
    nint windowSurface;
    SDL.FRect textRect;
    public Point windowSize;
    Dictionary<int, UIElement> ui = new Dictionary<int, UIElement>();
    public static SDL.FPoint cursor;
    public double deltaTime = 0;
    public static int? selected = null;
    private bool acceptingInput = false;
    public int lastkeyp = 0;
    public const int tileSize = 160;
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
    public unsafe void writeText(string c, float x, float y, string font, SDL.Color fg, int[]? alignment=null, SDL.FRect? src=null) {
        if (c.Length == 0) return;
        if (alignment == null)
        {
            alignment = [0, 0];
        }
        nint surface = TTF.RenderTextBlended(fonts[font], c, (uint)c.Length, fg);
        if (surface == NULL)
        {
            SDL.LogError(SDL.LogCategory.System, String.Format("Font Surface could not display: {0}", SDL.GetError()));
            return;
        }
        SDL.Surface surf = *(SDL.Surface*)surface.ToPointer();
        nint texture = SDL.CreateTextureFromSurface(renderer, surface);
        textRect.W = surf.Width;
        textRect.H = surf.Height;
        if (src != null)
        {
            textRect.W = MathF.Min(textRect.W, ((SDL.FRect)src).W);
        }
        textRect.X = align(alignment[0], x, surf.Width);
        textRect.Y = align(alignment[1], y, surf.Height);
        SDL.DestroySurface(surface);
        if (src == null)
        {
            SDL.RenderTexture(renderer, texture, NULL, textRect);
        } else
        {
            SDL.FRect rsrc = (SDL.FRect)src;
            SDL.RenderTexture(renderer, texture, new SDL.FRect
            {
                X = rsrc.X, W = MathF.Min(rsrc.W, textRect.W),
                Y = 0, H = textRect.H
            }, textRect);
        }
        SDL.DestroyTexture(texture);
    }
    public static SDL.Color black = createColor(0, 0, 0);
    public static SDL.Color white = createColor(255, 255, 255);
    [STAThread]
    public void Loop()
    {
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
        windowSize = getWindowSize(window);
        windowSurface = SDL.GetWindowSurface(window);
        initFont("consbold", "consbold.ttf", 30); // consbold_30
        initFonts("sans", "opensans.ttf", [20, 8, 15]); // opensans_ 20,8,15
        SDL.Color titleColor = createColor(255, 128, 0);
        SDL.Color grey = createColor(205);
        SDL.Color darkergrey = createColor(150);
        SDL.Color darkgrey = createColor(180);

        // Text alignments
        int[] leftlower = SDLTools.Get(TextA.LEFT, TextA.LOWER);
        int[] leftcenter = SDLTools.Get(TextA.LEFT, TextA.CENTER);
        int[] rightcenter = SDLTools.Get(TextA.RIGHT, TextA.CENTER);
        // Text alignments end
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
        ulong lastTick;
        int nsDelay = 1000/30;
        int nearestSleep = 0;
        bool loop = true;

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
                    case SDL.EventType.MouseButtonDown:
                        if (e.Button.Button == 1)
                        {
                            clicked = true;
                            selected = null;
                            inpacc = false;
                        }
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
                                }
                                break;
                            case SDL.Scancode.Left:
                                if (selected != null)
                                {
                                    int seld = (int)selected;
                                    ui[seld].cursorpos--;
                                    ui[seld].cursorpos = Math.Max(0, ui[seld].cursorpos);
                                }
                                break;
                            case SDL.Scancode.Right:
                                if (selected != null)
                                {
                                    int seld = (int)selected;
                                    ui[seld].cursorpos++;
                                    ui[seld].cursorpos = Math.Min(ui[seld].contents.Length, ui[seld].cursorpos);
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case SDL.EventType.TextInput:
                        if (acceptingInput && selected != null)
                        {
                            char yes = SDLTools.Get(e.Edit.Text);
                            ui[(int)selected].contents = ui[(int)selected].contents.Insert(ui[(int)selected].cursorpos, yes.ToString());
                            ui[(int)selected].cursorpos++;
                        }
                        break;
                    default:
                        break;
                }
            }
            cursor = getCursorPoint();
            SDL.SetRenderDrawColor(renderer, 255, 255, 255, 0);
            SDL.RenderClear(renderer);
            writeText(nearestSleep.ToString(), 0, 0, "sans_8", black);
            writeText(lastkeyp.ToString(), 0, 8, "sans_8", black);
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
            /*switch (tc.mode)
            {
                case "prompt":
                    if (tc.misctext.ContainsKey("vers"))
                    {
                        writeText(tc.misctext["vers"], 10, windowSize.y-10, "sans_20", black, leftlower);
                    }
                    if (tc.misctext.ContainsKey("name"))
                    {
                        string[] spln = tc.misctext["name"].Split("|");
                        writeText(spln[0], (windowSize.x/2)-10, 100, "consbold_30", titleColor, rightcenter);
                        writeText(spln[1], (windowSize.x/2)+10, 100, "consbold_30", SDLTools.Invert(titleColor), leftcenter);
                    }
                    break;
            } use scene switch instead/if statments*/
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