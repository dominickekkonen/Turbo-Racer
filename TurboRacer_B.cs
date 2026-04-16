using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

// --- DATA MODELS ---

class Player
{
    private readonly int[] validPositions = { 12, 20, 27, 35, 42, 50, 57 };
    private int positionIndex = 3;

    public int X => validPositions[positionIndex];
    public int Y { get; } = 18;
    public int Lives { get; set; } = 3;
    public int Score { get; set; } = 0;
    public string CarColor { get; set; } = "\x1b[36m";

    public void MoveLeft() { if (positionIndex > 0) positionIndex--; }
    public void MoveRight() { if (positionIndex < validPositions.Length - 1) positionIndex++; }

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
    public bool CheckCollision(Player p) => Math.Abs(Y - p.Y) < 2 && Math.Abs(X - p.X) < 3;
}

class Obstacle : Entity
{
    public override string[] Sprite => new string[] { "o---o", "| X |", "o---o" };
    public override string Color => "\x1b[31m";
    public Obstacle(int x, int y) : base(x, y) { }
}

class RepairKit : Entity
{
    public override string[] Sprite => new string[] { " +++ ", "(🔧)", " +++ " };
    public override string Color => "\x1b[32m";
    public RepairKit(int x, int y) : base(x, y) { }
}

// --- MAIN ENGINE ---

class TurboRacerGame
{
    const string Reset = "\x1b[0m", Red = "\x1b[31m", Green = "\x1b[32m",
                 Yellow = "\x1b[33m", Blue = "\x1b[34m", Cyan = "\x1b[36m",
                 White = "\x1b[37m", Bold = "\x1b[1m", Gray = "\x1b[90m", Magenta = "\x1b[35m";

    const string ScoreFile = "highscores.txt";

    enum State { Menu, Settings, Playing, GameOver, Database }
    State currentState = State.Menu;

    Player player = new Player();
    List<Entity> entities = new List<Entity>();
    List<int> scoreHistory = new List<int>();
    Random rng = new Random();

    int gameSpeed = 45;
    string difficultyName = "Easy";
    int healthLimit = 5;
    int maxSpawnCount = 1;

    bool isRunning = true;
    int roadOffset = 0;

    const int RoadLeft = 5, RoadRight = 65, RoadHeight = 22;
    readonly int[] enemyLanes = { 12, 27, 42, 57 };
    readonly string[] playerColors = { Cyan, Green, Yellow, Magenta, White, Bold + Blue };

    public void Start()
    {
        Console.OutputEncoding = Encoding.UTF8;
        LoadScoresFromFile();
        try { Console.CursorVisible = false; } catch { }

        while (isRunning)
        {
            switch (currentState)
            {
                case State.Menu: DrawMenu(); break;
                case State.Settings: DrawSettings(); break;
                case State.Database: DrawDatabase(); break;
                case State.Playing: UpdateGame(); break;
                case State.GameOver: DrawGameOver(); break;
            }
        }
    }

    void SaveScoreToFile(int score)
    {
        try { File.AppendAllText(ScoreFile, score.ToString() + Environment.NewLine); scoreHistory.Add(score); } catch { }
    }

    void LoadScoresFromFile()
    {
        try
        {
            if (File.Exists(ScoreFile))
            {
                string[] lines = File.ReadAllLines(ScoreFile);
                foreach (string line in lines) if (int.TryParse(line, out int s)) scoreHistory.Add(s);
            }
        }
        catch { }
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
        if (scoreHistory.Any()) DrawCentered($"{Green}{Bold}★ PERSONAL BEST: {scoreHistory.Max()} ★{Reset}");
        Console.WriteLine("\n");
        DrawCentered($"{White}[1] {Green}START GAME{Reset}");
        DrawCentered($"{White}[2] {Cyan}DIFFICULTY{Reset}");
        DrawCentered($"{White}[3] {Yellow}RECORDS (DB){Reset}");
        DrawCentered($"{White}[4] {Red}EXIT{Reset}");
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) { ResetGame(); currentState = State.Playing; }
        else if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2) currentState = State.Settings;
        else if (key == ConsoleKey.D3 || key == ConsoleKey.NumPad3) currentState = State.Database;
        else if (key == ConsoleKey.D4 || key == ConsoleKey.NumPad4) isRunning = false;
    }

    void DrawSettings()
    {
        Console.Clear();
        Console.WriteLine("\n\n");
        DrawCentered($"{Bold}{White}─── {Cyan}SELECT YOUR INTENSITY{White} ───{Reset}");
        Console.WriteLine("\n");

        // EASY OPTION
        DrawCentered($"{Green}┌──────────────────────────────────────────┐{Reset}");
        DrawCentered($"{Green}│ [1] {Bold}EASY MODE{Reset}{Green}                            │{Reset}");
        DrawCentered($"{Green}│     DANGER: █░░░░░░░░░  (45ms / 5 HP)    │{Reset}");
        DrawCentered($"{Green}└──────────────────────────────────────────┘{Reset}");
        Console.WriteLine();

        // HARD OPTION
        DrawCentered($"{Yellow}┌──────────────────────────────────────────┐{Reset}");
        DrawCentered($"{Yellow}│ [2] {Bold}HARD MODE{Reset}{Yellow}                            │{Reset}");
        DrawCentered($"{Yellow}│     DANGER: █████░░░░░  (25ms / 3 HP)    │{Reset}");
        DrawCentered($"{Yellow}└──────────────────────────────────────────┘{Reset}");
        Console.WriteLine();

        // EXTREME OPTION
        DrawCentered($"{Red}┌──────────────────────────────────────────┐{Reset}");
        DrawCentered($"{Red}│ [3] {Bold}EXTREME MODE{Reset}{Red}                         │{Reset}");
        DrawCentered($"{Red}│     DANGER: ██████████  (20ms / 2 HP)    │{Reset}");
        DrawCentered($"{Red}└──────────────────────────────────────────┘{Reset}");

        Console.WriteLine("\n");
        DrawCentered($"{Gray}Select number to confirm or [ESC] for Menu{Reset}");

        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1) { gameSpeed = 45; difficultyName = "Easy"; healthLimit = 5; maxSpawnCount = 1; currentState = State.Menu; }
        if (key == ConsoleKey.D2) { gameSpeed = 25; difficultyName = "Hard"; healthLimit = 3; maxSpawnCount = 2; currentState = State.Menu; }
        if (key == ConsoleKey.D3) { gameSpeed = 20; difficultyName = "Extreme"; healthLimit = 2; maxSpawnCount = 3; currentState = State.Menu; }
        if (key == ConsoleKey.Escape) currentState = State.Menu;
    }

    void DrawDatabase()
    {
        Console.Clear();
        Console.WriteLine("\n\n");
        DrawCentered($"{Yellow}═══ MISSION HISTORY DATABASE ═══{Reset}");
        if (!scoreHistory.Any()) DrawCentered($"{Gray}No records found.{Reset}");
        else
        {
            DrawCentered($"{Green}ALL-TIME HIGH: {scoreHistory.Max()}{Reset}");
            foreach (var s in scoreHistory.AsEnumerable().Reverse().Take(8))
                DrawCentered($"{White}Entry: {s:D6}{Reset}");
        }
        Console.WriteLine("\n\n" + Gray + "Press any key to go back" + Reset);
        Console.ReadKey(true);
        currentState = State.Menu;
    }

    void ResetGame() { player = new Player(); player.Lives = healthLimit; player.CarColor = playerColors[rng.Next(playerColors.Length)]; entities.Clear(); }

    void UpdateGame()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.LeftArrow) player.MoveLeft();
            if (key == ConsoleKey.RightArrow) player.MoveRight();
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
                    if (player.Lives <= 0) { SaveScoreToFile(player.Score); currentState = State.GameOver; return; }
                }
                else if (entities[i] is RepairKit) { if (player.Lives < healthLimit) player.Lives++; }
                entities.RemoveAt(i);
            }
            else if (entities[i].Y > RoadHeight + 2) entities.RemoveAt(i);
        }

        if (!entities.Any(e => e.Y < 5) && rng.Next(0, 10) > 5)
        {
            int spawns = rng.Next(1, maxSpawnCount + 1);
            List<int> used = new List<int>();
            for (int i = 0; i < spawns; i++)
            {
                int laneIdx = rng.Next(enemyLanes.Length);
                if (!used.Contains(laneIdx)) { entities.Add(new Obstacle(enemyLanes[laneIdx], 0)); used.Add(laneIdx); }
            }
        }

        player.Score += (difficultyName == "Extreme" ? 10 : (difficultyName == "Hard" ? 5 : 2));
        DrawFrame();
        Thread.Sleep(gameSpeed);
    }

    void DrawFrame()
    {
        StringBuilder canvas = new StringBuilder();
        string padding = new string(' ', Math.Max(0, (Console.WindowWidth / 2) - 55));
        canvas.Append("\n" + padding + $"{Cyan}═══ TURBO DRIVE XL ═══{Reset}\n");
        for (int y = 0; y < RoadHeight; y++)
        {
            canvas.Append(padding + $"{Blue}║{Reset}");
            for (int x = RoadLeft; x <= RoadRight; x++)
            {
                if (y >= player.Y - 1 && y <= player.Y + 1 && x >= player.X - 2 && x <= player.X + 2)
                {
                    string car = "o---o| A |o---o";
                    canvas.Append($"{player.CarColor}{car.Substring((y - (player.Y - 1)) * 5, 5)[x - (player.X - 2)]}{Reset}");
                }
                else
                {
                    var ent = entities.FirstOrDefault(e => y >= e.Y - 1 && y <= e.Y + 1 && x >= e.X - 2 && x <= e.X + 2);
                    if (ent != null) canvas.Append($"{ent.Color}{ent.Sprite[y - (ent.Y - 1)][x - (ent.X - 2)]}{Reset}");
                    else if (x == RoadLeft || x == RoadRight) canvas.Append($"{Gray}█{Reset}");
                    else if ((x == 20 || x == 35 || x == 50) && (y + roadOffset) % 4 == 0) canvas.Append($"{White}¦{Reset}");
                    else canvas.Append(" ");
                }
            }
            canvas.Append($"{Blue}║{Reset}     ");
            if (y == 3) canvas.Append($"{Yellow}SCORE: {player.Score:D6}{Reset}");
            if (y == 4) canvas.Append($"{Red}LIVES: {player.GetHearts()}{Reset}");
            canvas.Append("\n");
        }
        canvas.Append(padding + $"{Blue}╚" + new string('═', RoadRight - RoadLeft + 1) + "╝{Reset}\n");
        try { Console.SetCursorPosition(0, 0); } catch { }
        Console.Write(canvas.ToString());
    }

    void DrawGameOver()
    {
        Console.Clear();
        Console.WriteLine("\n\n\n");
        DrawCentered($"{Red}{Bold}╔══════════════════════════════════════════╗{Reset}");
        DrawCentered($"{Red}{Bold}║          CRITICAL SYSTEM FAILURE         ║{Reset}");
        DrawCentered($"{Red}{Bold}╚══════════════════════════════════════════╝{Reset}");
        Console.WriteLine("\n");
        DrawCentered($"{White}--- POST-CRASH DIAGNOSTIC ---{Reset}");
        DrawCentered($"{Gray}FINAL DISTANCE: {Yellow}{player.Score} Units{Reset}");
        DrawCentered($"{Gray}DIFFICULTY:     {White}{difficultyName.ToUpper()}{Reset}");
        int pb = scoreHistory.Max();
        if (player.Score >= pb) DrawCentered($"{Green}{Bold}!!! NEW RECORD DETECTED !!!{Reset}");
        else DrawCentered($"{Gray}BEST RECORD:    {pb}{Reset}");
        Console.WriteLine("\n" + Red + "VEHICLE STATUS: DESTROYED" + Reset);
        DrawCentered($"{Cyan}RECORDS LOGGED TO: {ScoreFile}{Reset}");
        Console.WriteLine("\n\n" + Green + "PRESS ANY KEY TO RETURN TO HQ" + Reset);
        Console.ReadKey(true);
        currentState = State.Menu;
    }
}

class Program { static void Main() => new TurboRacerGame().Start(); }
