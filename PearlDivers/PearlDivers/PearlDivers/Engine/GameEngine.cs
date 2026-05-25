using PearlDivers.Models;
using PearlDivers.Models.Decorators;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media;
namespace PearlDivers.Engine
{
    public class GameEngine
    {
        private IDiver p1, p2;
        private GraphicsHandler graphics;
        private InputManager input;
        private bool isRunning;
        private TimeSpan _lastRenderTime = TimeSpan.Zero;
        private List<GameObject> gameObjects = new List<GameObject>();
        private List<Shellfish> shellfishes = new List<Shellfish>();
        private EnemyFactory _fishFactory = new FishFactory();
        private EnemyFactory _snakeFactory = new SnakeFactory();
        private int screenWidth;
        private int screenHeight;
        private Random random = new Random();
        private Action _onGameOver;
        private float _gameTimer = 60f;
        private bool _gameOver = false;
        private string _winnerText = "";
        private const float SNAKE_ZONE_TOP = 50f;
        private const float SNAKE_ZONE_BOTTOM = 260f;
        private const float FISH_ZONE_TOP = 250f;
        private const float FISH_ZONE_BOTTOM = 450f;
        private const int INITIAL_SHELL_COUNT = 5;
        private const int MAX_SHELLS_ON_SCREEN = 5;
        private const float SHELL_RESPAWN_DELAY_MIN = 3f;
        private const float SHELL_RESPAWN_DELAY_MAX = 6f;
        private float _shellRespawnTimer = 0f;
        private float _shellRespawnDelay = 5f;
        private float _spawnTimer = 0f;
        private const float SPAWN_INTERVAL = 4f;
        private const float ENEMY_DESPAWN_BUFFER = 300f;
        private const int MAX_BONUSES_ON_SCREEN = 2;
        private float SurfaceY => screenHeight * 0.15f;
        private float BottomY => (float)screenHeight;
        private bool debugShowHitboxes = false;
        public GameEngine(IntPtr hwnd, int width, int height, Action onGameOver)
        {
            screenWidth = width; screenHeight = height;
            _onGameOver = onGameOver;
            p1 = new Diver(1, 100, 300);
            p2 = new Diver(2, 900, 300);
            p1.SetScreenBounds(width, height);
            p2.SetScreenBounds(width, height);
            input = new InputManager();
            graphics = new GraphicsHandler();
            graphics.Initialize(hwnd, width, height);
            LoadAssets();
            SpawnInitialObjects(width, height);
            CompositionTarget.Rendering += OnRendering;
            isRunning = true;
        }
        private void LoadAssets()
        {
            graphics.LoadTexture("bg", "Assets/background.png");
            for (int i = 1; i <= 2; i++)
            {
                graphics.LoadTexture($"d{i}_idle", $"Assets/Players/Diver{i}.png");
                graphics.LoadTexture($"d{i}_swim1", $"Assets/Players/Diver{i}_Swimming1.png");
                graphics.LoadTexture($"d{i}_swim2", $"Assets/Players/Diver{i}_Swimming2.png");
            }
            graphics.LoadTexture("shell_closed", "Assets/Shells/Shell_Closed.png");
            graphics.LoadTexture("shell_poison", "Assets/Shells/Shell_Poison.png");
            graphics.LoadTexture("shell_pearl", "Assets/Shells/Shell_Pearl.png");
            for (int i = 1; i <= 3; i++)
            {
                graphics.LoadTexture($"fish_{i}", $"Assets/Fish/Fish_{i}.png");
                graphics.LoadTexture($"snake_{i}", $"Assets/Snakes/Snake_{i}.png");
            }
            graphics.LoadTexture("bonus_heal", "Assets/Bonuses/Heal_Bonus.png");
            graphics.LoadTexture("bonus_oxygen", "Assets/Bonuses/Oxygen_Bonus.png");
            graphics.LoadTexture("bonus_shield", "Assets/Bonuses/Shield_Bonus.png");
            graphics.LoadTexture("bonus_speed", "Assets/Bonuses/Speed_Bonus.png");
        }
        private void SpawnInitialObjects(int width, int height)
        {
            SpawnShells(width, height - 50);
            SpawnInitialEnemies(width, height);
        }
        private void SpawnShells(int width, int height)
        {
            float zoneTop = height * 0.78f;
            float zoneBottom = height * 0.92f - Shellfish.CellHeight;
            for (int i = 0; i < INITIAL_SHELL_COUNT; i++)
            {
                bool found = TryFindShellPosition(width, zoneTop, zoneBottom, out float x, out float y);
                if (!found) { x = random.NextFloat(20f, width - Shellfish.CellWidth - 20f); y = random.NextFloat(zoneTop, zoneBottom); }
                ShellfishType type = (random.Next(0, 3) == 0) ? ShellfishType.Poisonous : ShellfishType.Normal;
                Shellfish shell = new Shellfish(x, y, type);
                shellfishes.Add(shell);
                gameObjects.Add(shell);
            }
        }

        private const float MIN_SHELL_GAP = 13f;
        private bool TryFindShellPosition(int width, float zoneTop, float zoneBottom, out float rx, out float ry, int attempts = 40)
        {
            float minX = 20f, maxX = width - Shellfish.CellWidth - 20f;
            for (int a = 0; a < attempts; a++)
            {
                float x = random.NextFloat(minX, maxX);
                float y = random.NextFloat(zoneTop, zoneBottom);
                bool overlaps = false;
                foreach (var obj in gameObjects)
                {
                    if (obj is Shellfish s && !s.IsRemoved)
                    {
                        float dx = Math.Abs(s.X - x);
                        float dy = Math.Abs(s.Y - y);
                        if (dx < Shellfish.CellWidth + MIN_SHELL_GAP && dy < Shellfish.CellHeight + MIN_SHELL_GAP)
                        { overlaps = true; break; }
                    }
                }
                if (!overlaps) { rx = x; ry = y; return true; }
            }
            rx = 0; ry = 0; return false;
        }
        private void SpawnInitialEnemies(int width, int height)
        {
            SpawnEnemiesFromFactory(_fishFactory, 4, width, height, FISH_ZONE_TOP, FISH_ZONE_BOTTOM, 80f);
            SpawnEnemiesFromFactory(_snakeFactory, 3, width, height, SNAKE_ZONE_TOP, SNAKE_ZONE_BOTTOM, 60f);
        }
        private void SpawnEnemiesFromFactory(EnemyFactory factory, int count, int width, int height, float zoneTop, float zoneBottom, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                bool fromLeft = random.Next(0, 2) == 0;
                float x = fromLeft ? -Enemy.DefaultSpriteW - random.Next(0, 200) : width + random.Next(0, 200);
                float y = random.NextFloat(zoneTop, zoneBottom - Enemy.DefaultSpriteH);
                Enemy enemy = factory.CreateEnemy(x, y, speed, zoneTop, zoneBottom);
                enemy.FacingLeft = !fromLeft;
                gameObjects.Add(enemy);
            }
        }
        private void UpdateSpawning(float dt, int width, int height)
        {
            if (_gameOver) return;

            for (int i = gameObjects.Count - 1; i >= 0; i--)
            {
                if (gameObjects[i] is Shellfish sh && sh.IsRemoved)
                    gameObjects.RemoveAt(i);
            }

            int activeShells = gameObjects.Count(o => o is Shellfish s && !s.IsRemoved);
            if (activeShells < MAX_SHELLS_ON_SCREEN)
            {
                _shellRespawnTimer += dt;
                if (_shellRespawnTimer >= _shellRespawnDelay)
                {
                    _shellRespawnTimer = 0f;
                    _shellRespawnDelay = random.NextFloat(SHELL_RESPAWN_DELAY_MIN, SHELL_RESPAWN_DELAY_MAX);
                    Shellfish toRespawn = shellfishes.FirstOrDefault(s => s.IsRemoved);
                    if (toRespawn != null)
                    {
                        float zoneTop = height * 0.78f;
                        float zoneBottom = height * 0.92f - Shellfish.CellHeight;
                        bool found = TryFindShellPosition(width, zoneTop, zoneBottom, out float x, out float y);
                        if (!found) { x = random.NextFloat(20f, width - Shellfish.CellWidth - 20f); y = random.NextFloat(zoneTop, zoneBottom); }
                        toRespawn.Respawn(x, y);
                        gameObjects.Add(toRespawn);
                    }
                }
            }
            else
            {
                _shellRespawnTimer = 0f;
            }

            _spawnTimer += dt;
            if (_spawnTimer >= SPAWN_INTERVAL)
            {
                _spawnTimer = 0f;
                if (random.Next(0, 2) == 0)
                    SpawnSingleEnemy(_fishFactory, width, height, FISH_ZONE_TOP, FISH_ZONE_BOTTOM, 80f);
                else
                    SpawnSingleEnemy(_snakeFactory, width, height, SNAKE_ZONE_TOP, SNAKE_ZONE_BOTTOM, 60f);
                int activeBonuses = gameObjects.Count(o => o is Bonus b && b.IsActive);
                if (activeBonuses < MAX_BONUSES_ON_SCREEN)
                    SpawnBonus(width, height);
            }
        }
        private void SpawnBonus(int width, int height)
        {
            float x = random.NextFloat(50f, width - 100f);
            float y = random.NextFloat(FISH_ZONE_TOP, FISH_ZONE_BOTTOM);
            BonusType type = (BonusType)random.Next(0, 4);
            Bonus bonus = new Bonus(x, y, type);
            gameObjects.Add(bonus);
        }
        private void SpawnSingleEnemy(EnemyFactory factory, int width, int height, float zoneTop, float zoneBottom, float speed)
        {
            bool fromLeft = random.Next(0, 2) == 0;
            float x = fromLeft ? -120f : width + 20f;
            float y = random.NextFloat(zoneTop, zoneBottom - 80f);
            Enemy enemy = factory.CreateEnemy(x, y, speed, zoneTop, zoneBottom);
            enemy.FacingLeft = !fromLeft;
            gameObjects.Add(enemy);
        }
        public void HandleKey(System.Windows.Input.Key key, bool isDown) => input.RegisterKey(key, isDown);
        private void OnRendering(object sender, EventArgs e)
        {
            if (!isRunning) return;
            RenderingEventArgs args = (System.Windows.Media.RenderingEventArgs)e;
            TimeSpan now = args.RenderingTime;
            if (_lastRenderTime == TimeSpan.Zero) { _lastRenderTime = now; return; }
            float dt = (float)(now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;
            if (dt > 0.1f) dt = 0.1f;
            if (_gameOver)
            {
                RenderGameOver();
                if (input.IsPressed(System.Windows.Input.Key.Escape))
                {
                    _onGameOver?.Invoke();
                }
                return;
            }
            _gameTimer -= dt;
            if (_gameTimer <= 0f)
            {
                _gameTimer = 0f;
                EndGameByTime();
            }
            var m1 = input.GetMovementP1(); p1.Move(m1.dx, m1.dy, dt);
            var m2 = input.GetMovementP2(); p2.Move(m2.dx, m2.dy, dt);
            if (p1 is Diver d1) d1.UpdateStatus(dt, SurfaceY, BottomY);
            if (p2 is Diver d2) d2.UpdateStatus(dt, SurfaceY, BottomY);
            // Обработка смерти с поддержкой ничьей
            if (p1.HP <= 0f && p2.HP <= 0f) EndGame("Ничья!");
            else if (p1.HP <= 0f) EndGame("Второй игрок победил!");
            else if (p2.HP <= 0f) EndGame("Первый игрок победил!");
            for (int i = gameObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = gameObjects[i];
                obj.Update(dt);
                if (obj is Enemy enemy && enemy.IsOffScreen(screenWidth, screenHeight, ENEMY_DESPAWN_BUFFER))
                    gameObjects.RemoveAt(i);
                else if (obj is Bonus bonus && !bonus.IsActive)
                    gameObjects.RemoveAt(i);
            }
            UpdateSpawning(dt, screenWidth, screenHeight);
            CheckCollisions();
            Render();
        }
        private void EndGame(string winnerText)
        {
            _gameOver = true;
            _winnerText = winnerText;
        }
        private void EndGameByTime()
        {
            string winner;
            if (p1.Score > p2.Score) winner = "Первый игрок победил!";
            else if (p2.Score > p1.Score) winner = "Второй игрок победил!";
            else winner = "Ничья!";
            EndGame(winner);
        }
        private void CheckCollisions()
        {
            CheckDiverCollisions(p1);
            CheckDiverCollisions(p2);
        }
        private void CheckDiverCollisions(IDiver diver)
        {
            RectF diverCollider = diver.Collider;
            foreach (GameObject obj in gameObjects)
            {
                if (obj is Enemy enemy && diverCollider.Intersects(enemy.Collider))
                {
                    diver.TakeDamage(10f);
                }
                else if (obj is Shellfish shell && shell.State == ShellState.Open && diverCollider.Intersects(shell.Collider))
                {
                    shell.Collect();
                    if (shell.Type == ShellfishType.Normal) diver.AddScore(1);
                    else
                    {
                        diver.ApplyPoison();
                        if (diver is Diver d) d.AddDecorator(new PoisonDecorator(diver));
                    }
                }
                else if (obj is Bonus bonus && bonus.IsActive && diverCollider.Intersects(bonus.Collider))
                {
                    bonus.IsActive = false;
                    if (bonus.Type == BonusType.Shield)
                    {
                        diver.ActivateShield();
                        if (diver is Diver d) d.AddDecorator(new ShieldDecorator(diver));
                    }
                    else if (bonus.Type == BonusType.Speed)
                    {
                        diver.ActivateSpeed();
                        if (diver is Diver d) d.AddDecorator(new SpeedDecorator(diver));
                    }
                    else if (bonus.Type == BonusType.Heal)
                    {
                        if (diver is Diver d)
                        {
                            d.CurePoison();
                            d.Heal(25);
                        }
                    }
                    else if (bonus.Type == BonusType.Oxygen)
                    {
                        if (diver is Diver d) d.RefillOxygen(40);
                    }
                }
            }
        }
        private void Render()
        {
            graphics.BeginDraw();
            graphics.Clear();
            graphics.DrawSprite("bg", 0, 0, screenWidth, screenHeight, false);
            foreach (var obj in gameObjects)
            {
                if (obj is Enemy enemy)
                    graphics.DrawSprite(enemy.TextureName, enemy.X, enemy.Y, enemy.SpriteW, enemy.SpriteH, enemy.FacingLeft, maintainAspectRatio: true);
                else if (obj is Shellfish shell)
                {
                    float drawX = shell.X + (Shellfish.CellWidth - shell.SpriteW) / 2f;
                    float drawY = shell.Y + (Shellfish.CellHeight - shell.SpriteH) / 2f;
                    graphics.DrawSprite(shell.TextureName, drawX, drawY, shell.SpriteW, shell.SpriteH, false, maintainAspectRatio: true);
                }
                else if (obj is Bonus bonus && bonus.IsActive)
                {
                    graphics.DrawSprite(bonus.TextureName, bonus.X, bonus.Y, bonus.SpriteW, bonus.SpriteH, false, maintainAspectRatio: true);
                }
            }
            DrawDiver(p1);
            DrawDiver(p2);
            graphics.DrawHUD(p1, p2);
            graphics.DrawTimer(_gameTimer);
            if (debugShowHitboxes) DrawDebugHitboxes();
            graphics.EndDraw();
        }
        private void RenderGameOver()
        {
            graphics.BeginDraw();
            graphics.Clear();
            graphics.DrawRect(0, 0, screenWidth, screenHeight, new RawColor4(0f, 0f, 0f, 0.7f), "overlay");
            graphics.DrawTextCentered(_winnerText, new RawColor4(1f, 1f, 1f, 1f), 64f, screenHeight / 2f - 40f);
            graphics.DrawTextCentered("Нажмите ESC чтобы выйти", new RawColor4(0.8f, 0.8f, 0.8f, 1f), 32f, screenHeight / 2f + 20f);
            graphics.EndDraw();
        }
        private void DrawDebugHitboxes()
        {
            var yellow = new RawColor4(1f, 1f, 0f, 1f);
            var red = new RawColor4(1f, 0f, 0f, 1f);
            var green = new RawColor4(0f, 1f, 0f, 1f);
            DrawBox(p1.Collider, yellow);
            DrawBox(p2.Collider, yellow);
            foreach (var obj in gameObjects)
            {
                if (obj is Enemy) DrawBox(obj.Collider, red);
                else if (obj is Shellfish) DrawBox(obj.Collider, green);
            }
        }
        private void DrawBox(RectF rect, RawColor4 color)
        {
            graphics.DrawRectOutline(rect.X, rect.Y, rect.Width, rect.Height, color, "debug", 2f);
        }
        private void DrawDiver(IDiver d)
        {
            if (!d.BlinkVisible) return;
            string state = d.IsMoving ? (d.CurrentFrame == 1 ? "swim1" : "swim2") : "idle";
            string texName = $"d{d.PlayerNumber}_{state}";
            if (d is Diver diver && diver.IsPoisonFlashing)
                graphics.DrawSpriteColored(texName, d.X, d.Y, 160, 180, d.FacingLeft, 0.2f, 1.0f, 0.2f, 0.6f, true);
            else
                graphics.DrawSprite(texName, d.X, d.Y, 160, 180, d.FacingLeft, true);
        }
    }
}