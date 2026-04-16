using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;

// --- DATA MODELS ---

class Player
{
    public int X { get; set; } = 25;
    public int Y { get; } = 15;
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;

    public void Move(int deltaX, int roadLeft, int roadRight)
    {
        int newX = X + deltaX;
        // Collision padding for 3-wide car
        if (newX > roadLeft + 2 && newX < roadRight - 2) X = newX;
    }

    public string GetHearts() => new string('♥', Math.Max(0, Lives));
}

abstract class Entity
{
    public int X { get; set; }
    public int Y { get; set; }
    public abstract string[] Sprite { get; }
    public abstract string Color { get; }

    public Entity(int x, int y) { X = x; Y = y; }
    public void Update() => Y++;

    public bool CheckCollision(Player p)
    {
        // Box collision for 3x3 sprites
        return Math.Abs(Y - p.Y) < 2 && Math.Abs(X - p.X) < 2;
    }
}

class Obstacle : Entity
{
    // Detailed car: o=o (wheels/axle), |H| (body), o=o (rear wheels)
    public override string[] Sprite => new string[] { "o=o", "|X|", "o=o" };
    public override string Color => "\x1b[31m";
    public Obstacle(int x, int y) : base(x, y) { }
}

class RepairKit : Entity
{
    public override string[] Sprite => new string[] { " + ", "(🔧)", " + " };
    public override string Color => "\x1b[32m";
    public RepairKit(int x, int y) : base(x, y) { }
}

// --- MAIN ENGINE ---

class TurboRacerGame
{
    const string Reset = "\x1b[0m", Red = "\x1b[31m", Green = "\x1b[32m",
                 Yellow = "\x1b[33m", Blue = "\x1b[34m", Cyan = "\x1b[36m",
                 White = "\x1b[37m", Bold = "\x1b[1m", Gray = "\x1b[90m";

    enum State { Menu, Settings, Playing, GameOver }
    State currentState = State.Menu;

    Player player = new Player();
    List<Entity> entities = new List<Entity>();
    List<int> scoreHistory = new List<int>();
    Random rng = new Random();

    int gameSpeed = 80;
    string difficultyName = "Easy";
    bool isRunning = true;
    int roadOffset = 0;
    int lastRepairScore = 0;

    const int RoadLeft = 5;
    const int RoadRight = 35; // Compacted road for better scale
    const int RoadHeight = 18;

    public void Start()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try { Console.CursorVisible = false; } catch { }

        while (isRunning)
        {
            switch (currentState)
            {
                case State.Menu: DrawMenu(); break;
                case State.Settings: DrawSettings(); break;
                case State.Playing: UpdateGame(); break;
                case State.GameOver: DrawGameOver(); break;
            }
        }
    }

    void DrawCentered(string text)
    {
        int width = Console.WindowWidth;
        string cleanText = Regex.Replace(text, @"\x1b\[[0-9;]*m", "");
        int spaces = Math.Max(0, (width / 2) - (cleanText.Length / 2));
        Console.WriteLine(new string(' ', spaces) + text);
    }

    void DrawMenu()
    {
        Console.Clear();
        Console.WriteLine("\n\n\n");
        DrawCentered($"{Blue}{Bold}╔══════════════════════════════════════════╗{Reset}");
        DrawCentered($"{Blue}{Bold}║             🏎️  {Yellow}TURBO RACER{Blue}              ║{Reset}");
        DrawCentered($"{Blue}{Bold}╚══════════════════════════════════════════╝{Reset}");

        if (scoreHistory.Any())
            DrawCentered($"{Green}{Bold}★ BEST RECORD: {scoreHistory.Max()} ★{Reset}");

        Console.WriteLine("\n");
        DrawCentered($"{White}[1] {Green}START GAME{Reset}");
        DrawCentered($"{White}[2] {Cyan}SETTINGS{Reset}");
        DrawCentered($"{White}[3] {Red}EXIT{Reset}");
        Console.WriteLine("\n");
        DrawCentered($"{Gray}System Status: Online | Difficulty: {Yellow}{difficultyName}{Reset}");

        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) { ResetGame(); currentState = State.Playing; }
        else if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2) currentState = State.Settings;
        else if (key == ConsoleKey.D3 || key == ConsoleKey.NumPad3) isRunning = false;
    }

    void DrawSettings()
    {
        Console.Clear();
        Console.WriteLine("\n\n");
        DrawCentered($"{Cyan}🔧 GARAGE TUNING{Reset}");
        DrawCentered($"{White}Current: {Yellow}{difficultyName}{Reset}");
        Console.WriteLine("\n");
        DrawCentered($"{White}[1] EASY (Safe Handling){Reset}");
        DrawCentered($"{White}[2] HARD (Pro Racer){Reset}");
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1) { gameSpeed = 80; difficultyName = "Easy"; }
        if (key == ConsoleKey.D2) { gameSpeed = 50; difficultyName = "Hard"; }
        currentState = State.Menu;
    }

    void ResetGame() { player = new Player(); entities.Clear(); lastRepairScore = 0; }

    void UpdateGame()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.LeftArrow) player.Move(-3, RoadLeft, RoadRight);
            if (key == ConsoleKey.RightArrow) player.Move(3, RoadLeft, RoadRight);
            if (key == ConsoleKey.Escape) currentState = State.Menu;
        }

        roadOffset = (roadOffset + 1) % 4;

        for (int i = entities.Count - 1; i >= 0; i--)
        {
            entities[i].Update();
            if (entities[i].CheckCollision(player))
            {
                if (entities[i] is Obstacle)
                {
                    player.Lives--;
                    if (player.Lives <= 0) { scoreHistory.Add(player.Score); currentState = State.GameOver; return; }
                }
                else if (entities[i] is RepairKit) { if (player.Lives < 5) player.Lives++; }
                entities.RemoveAt(i);
            }
            else if (entities[i].Y > RoadHeight + 2) entities.RemoveAt(i);
        }

        if (rng.Next(0, 10) > 7) entities.Add(new Obstacle(rng.Next(RoadLeft + 3, RoadRight - 3), 0));
        if (player.Score > 0 && player.Score % 300 == 0 && player.Score != lastRepairScore)
        {
            entities.Add(new RepairKit(rng.Next(RoadLeft + 3, RoadRight - 3), 0));
            lastRepairScore = player.Score;
        }

        player.Score += (difficultyName == "Hard" ? 5 : 2);
        DrawFrame();
        Thread.Sleep(gameSpeed);
    }

    void DrawFrame()
    {
        StringBuilder canvas = new StringBuilder();
        int winWidth = Console.WindowWidth;
        // Padding for the central game road
        string padding = new string(' ', Math.Max(0, (winWidth / 2) - 30));

        canvas.Append("\n" + padding + $"{Cyan}═══ TURBO DRIVE ═══{Reset}\n");

        for (int y = 0; y < RoadHeight; y++)
        {
            // Left Border
            canvas.Append(padding + $"{Blue}║{Reset}");

            // Draw Road Content
            for (int x = RoadLeft; x <= RoadRight; x++)
            {
                if (y >= player.Y - 1 && y <= player.Y + 1 && x >= player.X - 1 && x <= player.X + 1)
                {
                    string[] pSprite = { "o=o", "|A|", "o=o" };
                    canvas.Append($"{Cyan}{pSprite[y - (player.Y - 1)][x - (player.X - 1)]}{Reset}");
                }
                else
                {
                    var ent = entities.FirstOrDefault(e => y >= e.Y - 1 && y <= e.Y + 1 && x >= e.X - 1 && x <= e.X + 1);
                    if (ent != null)
                        canvas.Append($"{ent.Color}{ent.Sprite[y - (ent.Y - 1)][x - (ent.X - 1)]}{Reset}");
                    else if (x == RoadLeft || x == RoadRight) canvas.Append($"{Gray}┃{Reset}");
                    else if (x == (RoadLeft + RoadRight) / 2 && (y + roadOffset) % 4 == 0) canvas.Append($"{White}¦{Reset}");
                    else canvas.Append(" ");
                }
            }

            // RIGHT BORDER + LARGE GAP
            canvas.Append($"{Blue}║{Reset}          "); // 10 spaces of separation

            // SEPARATED HUD PANEL
            if (y == 2) canvas.Append($"{Blue}╔══════════════════╗{Reset}");
            if (y == 3) canvas.Append($"{Blue}║{Reset} {Yellow}SCORE: {player.Score:D6} {Blue}║{Reset}");
            if (y == 4) canvas.Append($"{Blue}║{Reset} {Red}LIVES: {player.GetHearts(),-7} {Blue}║{Reset}");
            if (y == 5) canvas.Append($"{Blue}╚══════════════════╝{Reset}");

            if (y == 8) canvas.Append($"{Gray}┌── DASHBOARD ──┐{Reset}");
            if (y == 9) canvas.Append($"{Gray}│ MODE: {difficultyName,-7} │{Reset}");
            if (y == 10) canvas.Append($"{Gray}└───────────────┘{Reset}");

            if (y == 14) canvas.Append($"{Gray}  [ESC] MENU    {Reset}");

            canvas.Append("\n");
        }
        canvas.Append(padding + $"{Blue}╚" + new string('═', RoadRight - RoadLeft + 1) + "╝{Reset}\n");

        try { Console.SetCursorPosition(0, 0); } catch { Console.Clear(); }
        Console.Write(canvas.ToString());
    }

    void DrawGameOver()
    {
        Console.Clear();
        Console.WriteLine("\n\n\n");
        DrawCentered($"{Red}{Bold}💥 CRASH! ENGINE FAILURE 💥{Reset}");
        DrawCentered($"{White}──────────────────────────────{Reset}");
        DrawCentered($"{White}TOTAL SCORE: {Yellow}{player.Score}{Reset}");

        if (scoreHistory.Count > 0)
        {
            Console.WriteLine();
            DrawCentered($"{Cyan}HISTORY:{Reset}");
            foreach (var s in scoreHistory.AsEnumerable().Reverse().Take(3))
                DrawCentered($"{Gray}• Score: {s}{Reset}");
        }

        Console.WriteLine("\n");
        DrawCentered($"{Green}Press any key to Restart{Reset}");
        Console.ReadKey(true);
        currentState = State.Menu;
    }
}

class Program { static void Main() => new TurboRacerGame().Start(); }
