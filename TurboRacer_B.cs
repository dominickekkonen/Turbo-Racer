using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

class Program
{
    enum State { Menu, Settings, Playing, GameOver }
    static State currentState = State.Menu;

    // ANSI Color Codes
    const string Reset = "\x1b[0m";
    const string Red = "\x1b[31m";
    const string Green = "\x1b[32m";
    const string Yellow = "\x1b[33m";
    const string Blue = "\x1b[34m";
    const string Cyan = "\x1b[36m";
    const string White = "\x1b[37m";
    const string Bold = "\x1b[1m";

    // Game Settings
    static int gameSpeed = 100;
    static string difficultyName = "Easy";
    static List<int> scoreHistory = new List<int>();
    
    // Player Stats
    static int playerX = 15;
    static int score = 0;
    static int lives = 3; // NEW: Health System
    static bool isRunning = true;
    
    static List<int[]> obstacles = new List<int[]>();
    static Random rng = new Random();
    static int roadOffset = 0;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try { Console.CursorVisible = false; } catch { }

        while (isRunning)
        {
            switch (currentState)
            {
                case State.Menu: ShowMenu(); break;
                case State.Settings: ShowSettings(); break;
                case State.Playing: RunGame(); break;
                case State.GameOver: ShowGameOver(); break;
            }
        }
    }

    static void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine("\n");
        Console.WriteLine($"{Blue}{Bold}      ╔══════════════════════════════╗{Reset}");
        Console.WriteLine($"{Blue}{Bold}      ║       {Yellow}🏎️  TURBO RACER{Blue}         ║{Reset}");
        Console.WriteLine($"{Blue}{Bold}      ╚══════════════════════════════╝{Reset}");
        
        if (scoreHistory.Count > 0)
            Console.WriteLine($"\n          {Green}BEST RECORD: {scoreHistory.Max()}{Reset}");

        Console.WriteLine($"\n         {White}[1] {Green}START GAME{Reset}");
        Console.WriteLine($"         {White}[2] {Cyan}SETTINGS{Reset}");
        Console.WriteLine($"         {White}[3] {Red}EXIT{Reset}");
        
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) { ResetGame(); currentState = State.Playing; }
        else if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2) currentState = State.Settings;
        else if (key == ConsoleKey.D3 || key == ConsoleKey.NumPad3) isRunning = false;
    }

    static void ShowSettings()
    {
        Console.Clear();
        Console.WriteLine($"\n\n      {Cyan}─── {Bold}GAME SETTINGS{Reset}{Cyan} ───{Reset}");
        Console.WriteLine($"\n      [1] {Green}EASY {White}(100ms speed){Reset}");
        Console.WriteLine($"      [2] {Red}HARD {White}(60ms speed){Reset}");
        Console.WriteLine($"\n      {Cyan}[B] BACK TO MENU{Reset}");

        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1) { gameSpeed = 100; difficultyName = "Easy"; }
        else if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2) { gameSpeed = 60; difficultyName = "Hard"; }
        else if (key == ConsoleKey.B) currentState = State.Menu;
    }

    static void ResetGame()
    {
        score = 0;
        lives = 3; // Reset lives
        playerX = 15;
        obstacles.Clear();
    }

    static void RunGame()
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.LeftArrow && playerX > 7) playerX -= 2;
            if (key == ConsoleKey.RightArrow && playerX < 23) playerX += 2;
            if (key == ConsoleKey.Escape) currentState = State.Menu;
        }

        roadOffset = (roadOffset + 1) % 4;

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            obstacles[i][1]++;
            
            // Collision Detection with Health Logic
            if (obstacles[i][1] == 14 && Math.Abs(obstacles[i][0] - playerX) <= 1)
            {
                lives--; // Lose a life
                obstacles.RemoveAt(i); // Remove the obstacle we hit
                
                if (lives <= 0)
                {
                    scoreHistory.Add(score);
                    currentState = State.GameOver;
                    return;
                }
            }
            else if (obstacles[i][1] > 16) obstacles.RemoveAt(i);
        }

        if (rng.Next(0, 10) > 7) obstacles.Add(new int[] { rng.Next(7, 24), 0 });
        score += (difficultyName == "Hard" ? 2 : 1);

        DrawFrame(32, 18);
        Thread.Sleep(gameSpeed);
    }

    static void DrawFrame(int w, int h)
    {
        StringBuilder canvas = new StringBuilder();
        
        // Header with Health Display (Hearts)
        string heartDisplay = new string('♥', lives);
        canvas.Append($"{Cyan}{Bold}── {difficultyName} ── {Red}{heartDisplay} {Reset} {White}SCORE: {Yellow}{score.ToString("D5")} {Cyan}──{Reset}\n");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isObstacle = false;
                foreach(var obs in obstacles) { if(obs[0] == x && obs[1] == y) isObstacle = true; }

                if (x == 5 || x == 25) canvas.Append($"{Blue}║{Reset}");
                else if (x == 15 && (y + roadOffset) % 4 == 0) canvas.Append($"{White}¦{Reset}");
                else if (y == 14 && x == playerX) canvas.Append($"{Cyan}H{Reset}");
                else if (y == 14 && x == playerX - 1) canvas.Append($"{Cyan}[{Reset}");
                else if (y == 14 && x == playerX + 1) canvas.Append($"{Cyan}]{Reset}");
                else if (isObstacle) canvas.Append($"{Red}█{Reset}");
                else canvas.Append(" ");
            }
            canvas.Append("\n");
        }

        try { Console.SetCursorPosition(0, 0); } catch { Console.Clear(); }
        Console.Write(canvas.ToString());
    }

    static void ShowGameOver()
    {
        Console.Clear();
        Console.WriteLine($"\n\n      {Red}{Bold}💥 GAME OVER 💥{Reset}");
        Console.WriteLine($"{White}      ────────────────────────────{Reset}");
        Console.WriteLine($"      {White}FINAL SCORE: {Yellow}{score}{Reset}");
        
        Console.WriteLine($"\n      {Cyan}--- RECENT ATTEMPTS ---{Reset}");
        var recentScores = scoreHistory.AsEnumerable().Reverse().Take(3);
        foreach (var s in recentScores) Console.WriteLine($"      {White}Score: {s}{Reset}");

        Console.WriteLine($"{White}      ────────────────────────────{Reset}");
        Console.WriteLine($"\n      {Green}Press any key for Menu{Reset}");
        Console.ReadKey(true);
        currentState = State.Menu;
    }
}
