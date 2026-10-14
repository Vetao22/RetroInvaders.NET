using System;
using System.Collections.Generic;
using System.Threading;
using TerminalGrid.NET;

namespace RetroInvaders.NET
{
    /// <summary>
    /// Core arcade game engine for RetroInvaders.NET.
    /// Handles player movement, bullet projectiles, alien grid rendering, and game state logic.
    /// </summary>
    public sealed class GameEngine
    {
        private const int ScreenWidth = 60;
        private const int ScreenHeight = 20;

        private int _playerX;
        private readonly List<(int X, int Y)> _bullets = new List<(int, int)>();
        private readonly List<(int X, int Y, bool Alive)> _aliens = new List<(int, int, bool)>();
        
        private int _score = 0;
        private int _alienDirection = 1;
        private int _alienMoveCounter = 0;
        private bool _isPlaying = true;

        public void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            InitializeGame();

            while (_isPlaying)
            {
                // 1. Handle user keyboard input non-blockingly
                ProcessInput();

                // 2. Update game physics and positions
                UpdateGameStates();

                // 3. Render frame to console buffer
                RenderFrame();

                // 4. Frame rate throttling (~30 FPS)
                Thread.Sleep(33);
            }

            // Game Over sequence: Display sarcastic High Scores via TerminalGrid.NET
            RenderGameOverScores();
        }

        private void InitializeGame()
        {
            _playerX = ScreenWidth / 2;
            _score = 0;
            _bullets.Clear();
            _aliens.Clear();

            // Populate alien formation grid (4 rows x 8 columns)
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    _aliens.Add((X: 10 + (c * 5), Y: 3 + (r * 2), Alive: true));
                }
            }
        }

        private void ProcessInput()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.LeftArrow && _playerX > 1)
                {
                    _playerX--;
                }
                else if (key == ConsoleKey.RightArrow && _playerX < ScreenWidth - 2)
                {
                    _playerX++;
                }
                else if (key == ConsoleKey.Spacebar)
                {
                    // Spawn bullet from player position
                    _bullets.Add((X: _playerX, Y: ScreenHeight - 2));
                }
                else if (key == ConsoleKey.Escape)
                {
                    _isPlaying = false;
                }
            }
        }

        private void UpdateGameStates()
        {
            // Update bullets
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];
                if (b.Y > 1)
                {
                    _bullets[i] = (b.X, b.Y - 1);
                }
                else
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                // Check collision with aliens
                for (int a = 0; a < _aliens.Count; a++)
                {
                    var alien = _aliens[a];
                    if (alien.Alive && b.X == alien.X && (b.Y == alien.Y || b.Y == alien.Y + 1))
                    {
                        _aliens[a] = (alien.X, alien.Y, false); // Kill alien
                        _bullets.RemoveAt(i);
                        _score += 100;
                        break;
                    }
                }
            }

            // Update aliens movement cycle
            _alienMoveCounter++;
            if (_alienMoveCounter >= 15)
            {
                _alienMoveCounter = 0;
                bool hitWall = false;

                foreach (var alien in _aliens)
                {
                    if (!alien.Alive) continue;
                    if ((alien.X + _alienDirection >= ScreenWidth - 2) || (alien.X + _alienDirection <= 1))
                    {
                        hitWall = true;
                        break;
                    }
                }

                if (hitWall)
                {
                    _alienDirection *= -1;
                    for (int i = 0; i < _aliens.Count; i++)
                    {
                        var al = _aliens[i];
                        _aliens[i] = (al.X, al.Y + 1, al.Alive); // Move down when hitting wall

                        // If aliens reach player height, game over!
                        if (al.Y >= ScreenHeight - 3 && al.Alive)
                        {
                            _isPlaying = false;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _aliens.Count; i++)
                    {
                        var al = _aliens[i];
                        _aliens[i] = (al.X + _alienDirection, al.Y, al.Alive);
                    }
                }
            }
        }

        private void RenderFrame()
        {
            var buffer = new char[ScreenHeight, ScreenWidth];

            // Fill canvas with empty space
            for (int y = 0; y < ScreenHeight; y++)
                for (int x = 0; x < ScreenWidth; x++)
                    buffer[y, x] = ' ';

            // Draw boundaries
            for (int x = 0; x < ScreenWidth; x++)
            {
                buffer[0, x] = '═';
                buffer[ScreenHeight - 1, x] = '═';
            }

            // Draw player ship
            buffer[ScreenHeight - 2, _playerX] = '▲';

            // Draw bullets
            foreach (var b in _bullets)
            {
                if (b.Y > 0 && b.Y < ScreenHeight - 1)
                    buffer[b.Y, b.X] = '│';
            }

            // Draw aliens
            foreach (var alien in _aliens)
            {
                if (alien.Alive && alien.Y > 0 && alien.Y < ScreenHeight - 1)
                {
                    buffer[alien.Y, alien.X] = 'M';
                }
            }

            // Output to console at origin (0,0) to prevent screen flicker
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($" SCORE: {_score} | CONTROLS: Arrow Keys to Move, SPACE to Shoot, ESC to Exit");
            
            for (int y = 0; y < ScreenHeight; y++)
            {
                for (int x = 0; x < ScreenWidth; x++)
                {
                    sb.Append(buffer[y, x]);
                }
                sb.AppendLine();
            }

            Console.SetCursorPosition(0, 0);
            Console.Write(sb.ToString());
        }

        private void RenderGameOverScores()
        {
            Console.Clear();
            Console.WriteLine("\n  *** GAME OVER! ALIENS TOOK OVER YOUR TERMINAL! ***\n");

            // Render fake, sarcastic high scores table using TerminalGrid.NET
            var scoreTable = new GridTable(BorderPreset.Rounded)
                .AddHeaders("Rank", "Player Tag", "Score", "Excuse / Achievement")
                .SetColumnAlignment(0, TextAlignment.Center)
                .SetColumnAlignment(2, TextAlignment.Right);

            scoreTable.AddRow(1, AnsiStyle.Colorize("NoobMaster99", ConsoleColor16.BrightRed), "12,400", "Cried when the first alien moved down.");
            scoreTable.AddRow(2, AnsiStyle.Colorize("KeyboardWarrior", ConsoleColor16.BrightYellow), "8,150", "Blamed keyboard lag for failing.");
            scoreTable.AddRow(3, AnsiStyle.Colorize("You (The Loser)", ConsoleColor16.BrightMagenta), _score.ToString(), "Total disaster. Absolutely tragic performance.");
            scoreTable.AddRow(4, AnsiStyle.Colorize("AI Bot #04", ConsoleColor16.BrightGreen), "1", "At least tried to shoot once.");

            Console.WriteLine(scoreTable.Render());
            Console.WriteLine("\nPress any key to close RetroInvaders.NET...");
            Console.ReadKey(true);
            Console.CursorVisible = true;
            Console.Clear();
        }
    }
}