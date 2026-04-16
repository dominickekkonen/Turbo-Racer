using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

// --- DATA MODELS ---

class Player
{
    public int X { get; set; } = 15;
    public int Y { get; } = 14;
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;

    public void Move(int deltaX)
    {
        int newX = X + deltaX;
        if (newX > 6 && newX < 24) X = newX;
    }

    public string GetHearts() => new string('♥', Math.Max(0, Lives));
}

abstract class Entity
{
    public int X { get; set; }
    public int Y { get; set; }
    public abstract string Symbol { get; } 
    public abstract string Color { get; }

    public Entity(int x, int y) { X = x; Y = y; }
    public void Update() => Y++;
    public bool CheckCollision(Player p) => Y == p.Y && Math.Abs(X - p.X) <= 1;
}

class Obstacle : Entity
{
    public override string Symbol => "█";
    public override string Color => "\x1b[31m"; 
    public Obstacle(int x, int y) : base(x, y) { }
}

class RepairKit : Entity
{
    public override string Symbol => "🔧";
    public override string Color => "\x1b[32m"; 
    public RepairKit(int x, int y) : base(x, y) { }
}

// --- MAIN ENGINE ---

class TurboRacerGame
{
    const string Reset = "\x1b[0m", Red = "\x1b[31m", Green = "\x1b[32m", 
                 Yellow = "\x1b[33m", Blue = "\x1b[34m", Cyan = "\x1b[36m", 
                 White = "\x1b[37m", Bold = "\x1b[1m";

    enum State { Menu, Settings, Playing, GameOver }
    State currentState = State.Menu;

    Player player = new Player();
    List<Entity> entities = new List<Entity>();
    List<int> scoreHistory = new List<int>();
    Random rng = new Random();

    int gameSpeed = 100;
    string difficultyName = "Easy";
    bool isRunning = true;
    int roadOffset = 0;
    int lastRepairScore = 0;

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

    void DrawMenu()
    {
        Console.Clear();
        Console.WriteLine("\n");
        Console.WriteLine($"{Blue}{Bold}      ╔══════════════════════════════╗{Reset}");
        Console.WriteLine($"{Blue}{Bold}      ║       {Yellow}🏎️  TURBO RACER{Blue}         ║{Reset}");
        Console.WriteLine($"{Blue}{Bold}      ╚══════════════════════════════╝{Reset}");
        
        if (scoreHistory.Any())
            Console.WriteLine($"\n          {Green}BEST RECORD: {scoreHistory.Max()}{Reset}");

        Console.WriteLine($"\n         {White}[1] {Green}START GAME{Reset}");
        Console.WriteLine($"         {White}[2] {Cyan}SETTINGS{Reset}");
        Console.WriteLine($"         {White}[3] {Red}EXIT{Reset}");
        Console.WriteLine($"\n      {White}Difficulty: {Yellow}{difficultyName}{Reset}");
        
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) { ResetGame(); currentState = State.Playing; }
        else if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2) currentState = State.Settings;
        else if (key == ConsoleKey.D3 || key == ConsoleKey.NumPad3) isRunning = false;
    }

    void DrawSettings()
    {
        Console.Clear();
        Console.WriteLine($"\n\n      {Cyan}─── {Bold}GAME SETTINGS{Reset}{Cyan} ───{Reset}");
        Console.WriteLine($"\n      Active Mode: {Yellow}{difficultyName}{Reset}");
        Console.WriteLine($"\n      {White}[1] {Green}EASY {White}(100ms){Reset}");
        Console.WriteLine($"      {White}[2] {Red}HARD {White}(60ms){Reset}");
        Console.WriteLine($"\n      {Cyan}[B] BACK TO MENU{Reset}");

        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1) { gameSpeed = 100; difficultyName = "Easy"; }
        else if (key == ConsoleKey.D2) { gameSpeed = 60; difficultyName = "Hard"; }
        else if (key == ConsoleKey.B) currentState = State.Menu;
    }

    void ResetGame()
    {
        player = new Player();
        entities.Clear();
        lastRepairScore = 0;
    }

    void UpdateGame()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.LeftArrow) player.Move(-2);
            if (key == ConsoleKey.RightArrow) player.Move(2);
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
                    if (player.Lives <= 0) 
                    { 
                        scoreHistory.Add(player.Score); 
                        currentState = State.GameOver; 
                        return; 
                    }
                }
                else if (entities[i] is RepairKit)
                {
                    if (player.Lives < 5) player.Lives++;
                }
                entities.RemoveAt(i);
            }
            else if (entities[i].Y > 16) entities.RemoveAt(i);
        }

        if (rng.Next(0, 10) > 7) entities.Add(new Obstacle(rng.Next(7, 24), 0));

        if (player.Score > 0 && player.Score % 200 == 0 && player.Score != lastRepairScore)
        {
            entities.Add(new RepairKit(rng.Next(7, 24), 0));
            lastRepairScore = player.Score;
        }

        player.Score += (difficultyName == "Hard" ? 2 : 1);
        DrawFrame();
        Thread.Sleep(gameSpeed);
    }

    void DrawFrame()
    {
        StringBuilder canvas = new StringBuilder();
        canvas.Append($"{Cyan}{Bold}── {difficultyName} ── {Red}{player.GetHearts()} {Reset} {White}SCORE: {Yellow}{player.Score:D5} {Cyan}──{Reset}\n");

        for (int y = 0; y < 18; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                var ent = entities.FirstOrDefault(e => e.X == x && e.Y == y);
                if (x == 5 || x == 25) canvas.Append($"{Blue}║{Reset}");
                else if (x == 15 && (y + roadOffset) % 4 == 0) canvas.Append($"{White}¦{Reset}");
                else if (y == player.Y && x == player.X) canvas.Append($"{Cyan}H{Reset}");
                else if (y == player.Y && (x == player.X - 1)) canvas.Append($"{Cyan}[{Reset}");
                else if (y == player.Y && (x == player.X + 1)) canvas.Append($"{Cyan}]{Reset}");
                else if (ent != null) canvas.Append($"{ent.Color}{ent.Symbol}{Reset}");
                else canvas.Append(" ");
            }
            canvas.Append("\n");
        }
        try { Console.SetCursorPosition(0, 0); } catch { Console.Clear(); }
        Console.Write(canvas.ToString());
    }

    void DrawGameOver()
    {
        Console.Clear();
        Console.WriteLine($"\n\n      {Red}{Bold}💥 KABOOM! 💥{Reset}");
        Console.WriteLine($"{White}      ────────────────────────────{Reset}");
        
        bool isNewRecord = scoreHistory.Count > 1 && player.Score == scoreHistory.Max();
        string runText = isNewRecord ? $"{Green}{Bold}NEW RECORD!{Reset}" : "THIS RUN:";
        
        Console.WriteLine($"      {White}{runText} {Yellow}{player.Score}{Reset}");
        
        Console.WriteLine($"\n      {Cyan}--- RECENT ATTEMPTS ---{Reset}");
        var recent = scoreHistory.AsEnumerable().Reverse().Take(5);
        int i = 1;
        foreach (var s in recent) 
        {
            Console.WriteLine($"      {White}Attempt {i++}: {s}{Reset}");
        }

        Console.WriteLine($"{White}      ────────────────────────────{Reset}");
        Console.WriteLine($"\n      {Green}Press any key for Menu{Reset}");
        Console.ReadKey(true);
        currentState = State.Menu;
    }
}

class Program { static void Main() => new TurboRacerGame().Start(); }
