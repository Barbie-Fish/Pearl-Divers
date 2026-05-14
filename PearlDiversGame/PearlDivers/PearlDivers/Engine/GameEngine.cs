using PearlDivers.Engine;
using PearlDivers.Models;
using System;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Media;
using SharpDX.Mathematics.Interop;

namespace PearlDivers.Engine
{
    public class GameEngine
    {
        private Diver p1, p2;
        private GraphicsHandler graphics;
        private InputManager input;
        private bool isRunning;
        private TimeSpan _lastRenderTime = TimeSpan.Zero;

        // Списки объектов через базовый класс
        private List<GameObject> gameObjects = new List<GameObject>();
        private List<Shellfish> shellfishes = new List<Shellfish>();

        // Фабрики для создания врагов
        private EnemyFactory _fishFactory = new FishFactory();
        private EnemyFactory _snakeFactory = new SnakeFactory();

        // Размеры экрана
        private int screenWidth;
        private int screenHeight;
        private Random random = new Random();

        // Зоны спавна
        private const float SNAKE_ZONE_TOP = 0f;
        private const float SNAKE_ZONE_BOTTOM = 200f;
        private const float FISH_ZONE_TOP = 250f;
        private const float FISH_ZONE_BOTTOM = 450f;

        // Настройки спавна
        private const int INITIAL_SHELL_COUNT = 8;
        private const int INITIAL_ENEMY_COUNT = 7;
        private float _spawnTimer = 0f;
        private const float SPAWN_INTERVAL = 5f; // новый враг каждые 5 секунд

        private const float ENEMY_DESPAWN_BUFFER = 300f;

        public GameEngine(IntPtr hwnd, int width, int height)
        {
            screenWidth = width;
            screenHeight = height;

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

            // Загрузка спрайтов призов (на будущее)
            graphics.LoadTexture("bonus_heal", "Assets/Bonuses/Heal_Bonus.png");
            graphics.LoadTexture("bonus_oxygen", "Assets/Bonuses/Oxygen_Bonus.png");
            graphics.LoadTexture("bonus_shield", "Assets/Bonuses/Shield_Bonus.png");
        }

        private void SpawnInitialObjects(int width, int height)
        {
            SpawnShells(width, height - 50);
            SpawnInitialEnemies(width, height);
        }

        private void SpawnShells(int width, int height)
        {
            float availableWidth = width - 120f;
            float spacing = availableWidth / (INITIAL_SHELL_COUNT - 1);
            float startY = height - Shellfish.CellHeight;

            for (int i = 0; i < INITIAL_SHELL_COUNT; i++)
            {
                float x =  20 + i * spacing;
                var type = (i % 3 == 0) ? ShellfishType.Poisonous : ShellfishType.Normal;
                var shell = new Shellfish(x, startY, type);
                shellfishes.Add(shell);
                gameObjects.Add(shell);
            }
        }

        private void SpawnInitialEnemies(int width, int height)
        {
            SpawnEnemiesFromFactory(_fishFactory, 4, width, height, FISH_ZONE_TOP, FISH_ZONE_BOTTOM, 80f);
            SpawnEnemiesFromFactory(_snakeFactory, 3, width, height, SNAKE_ZONE_TOP, SNAKE_ZONE_BOTTOM, 60f);
        }

        private void SpawnEnemiesFromFactory(EnemyFactory factory, int count, int width, int height,
     float zoneTop, float zoneBottom, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                bool fromLeft = random.Next(0, 2) == 0;

                // ИСПРАВЛЕНО: используем статическую константу
                float x = fromLeft
                    ? -Enemy.DefaultSpriteW - random.Next(0, 200)
                    : width + random.Next(0, 200);

                float y = random.NextFloat(zoneTop, zoneBottom - Enemy.DefaultSpriteH);

                var enemy = factory.CreateEnemy(x, y, speed, zoneTop, zoneBottom);
                enemy.FacingLeft = !fromLeft;

                gameObjects.Add(enemy);
            }
        }

        // НОВЫЙ МЕТОД: непрерывный спавн через фабрику
        private void UpdateSpawning(float dt, int width, int height)
        {
            _spawnTimer += dt;

            if (_spawnTimer >= SPAWN_INTERVAL)
            {
                _spawnTimer = 0f;

                // Случайный выбор: рыба или змея
                if (random.Next(0, 2) == 0)
                {
                    SpawnSingleEnemy(_fishFactory, width, height, FISH_ZONE_TOP, FISH_ZONE_BOTTOM, 80f);
                }
                else
                {
                    SpawnSingleEnemy(_snakeFactory, width, height, SNAKE_ZONE_TOP, SNAKE_ZONE_BOTTOM, 60f);
                }
            }
        }

        private void SpawnSingleEnemy(EnemyFactory factory, int width, int height,
            float zoneTop, float zoneBottom, float speed)
        {
            // Спавн с края, куда смотрит враг
            bool fromLeft = random.Next(0, 2) == 0;
            float x = fromLeft ? -120f : width + 20f;
            float y = random.NextFloat(zoneTop, zoneBottom - 80f);

            var enemy = factory.CreateEnemy(x, y, speed, zoneTop, zoneBottom); 
            enemy.FacingLeft = !fromLeft;

            gameObjects.Add(enemy);
        }

        public void HandleKey(System.Windows.Input.Key key, bool isDown) => input.RegisterKey(key, isDown);

        private void OnRendering(object sender, EventArgs e)
        {
            if (!isRunning) return;

            var args = (System.Windows.Media.RenderingEventArgs)e;
            var now = args.RenderingTime;

            if (_lastRenderTime == TimeSpan.Zero)
            {
                _lastRenderTime = now;
                return;
            }

            float dt = (float)(now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;
            if (dt > 0.1f) dt = 0.1f;

            // Обновление игроков
            var m1 = input.GetMovementP1();
            p1.Move(m1.dx, m1.dy, dt);
            var m2 = input.GetMovementP2();
            p2.Move(m2.dx, m2.dy, dt);

            // Обновление всех объектов
            for (int i = gameObjects.Count - 1; i >= 0; i--)
            {
                var obj = gameObjects[i];
                obj.Update(dt);

                // Удаление ушедших за экран врагов
                if (obj is Enemy enemy && enemy.IsOffScreen(screenWidth, screenHeight, ENEMY_DESPAWN_BUFFER))
                {
                    gameObjects.RemoveAt(i);
                }
                // Удаление неактивных призов
                else if (obj is Bonus bonus && !bonus.IsActive)
                {
                    gameObjects.RemoveAt(i);
                }
            }

            // Непрерывный спавн новых врагов
            UpdateSpawning(dt, screenWidth, screenHeight);

            CheckCollisions();
            Render();
        }

        private void CheckCollisions()
        {
            CheckDiverCollisions(p1);
            CheckDiverCollisions(p2);
        }

        private void CheckDiverCollisions(Diver diver)
        {
            var diverCollider = diver.Collider;

            foreach (var obj in gameObjects)
            {
                if (obj is Enemy enemy && diverCollider.Intersects(enemy.Collider))
                {
                    Console.WriteLine($"Player {diver.PlayerNumber} hit {enemy.Type}!");
                    // Позже: diver.TakeDamage(10);
                }
                else if (obj is Shellfish shell && shell.State == ShellState.Open
                    && diverCollider.Intersects(shell.Collider))
                {
                    Console.WriteLine($"Player {diver.PlayerNumber} collected from shell!");
                    // Позже: если shell.Type == Poisonous — урон, иначе +жемчуг
                }
                else if (obj is Bonus bonus && bonus.IsActive
                    && diverCollider.Intersects(bonus.Collider))
                {
                    Console.WriteLine($"Player {diver.PlayerNumber} got {bonus.Type} bonus!");
                    // Позже: применить бафф через паттерн Декоратор
                    bonus.IsActive = false;
                }
            }
        }

        private void Render()
        {
            graphics.BeginDraw();
            graphics.Clear();
            graphics.DrawSprite("bg", 0, 0, screenWidth, screenHeight, false);

            // Отрисовка ракушек
            foreach (var shell in shellfishes)
            {
                float drawX = shell.X + (Shellfish.CellWidth - shell.SpriteW) / 2f; 
                float drawY = shell.Y + (Shellfish.CellHeight - shell.SpriteH) / 2f;
                graphics.DrawSprite(shell.TextureName, drawX, drawY,
                    shell.SpriteW, shell.SpriteH, false, maintainAspectRatio: true);
            }

            // Отрисовка всех динамических объектов
            foreach (var obj in gameObjects)
            {
                if (obj is Enemy enemy)
                {
                    graphics.DrawSprite(enemy.TextureName, enemy.X, enemy.Y,
                        enemy.SpriteW, enemy.SpriteH, enemy.FacingLeft, maintainAspectRatio: true);
                }
                else if (obj is Bonus bonus && bonus.IsActive)
                {
                    graphics.DrawSprite(bonus.TextureName, bonus.X, bonus.Y,
                        bonus.SpriteW, bonus.SpriteH, false, maintainAspectRatio: true);
                }
            }

            DrawDiver(p1);
            DrawDiver(p2);
            graphics.DrawUI();
            graphics.EndDraw();
        }

        private void DrawDiver(Diver d)
        {
            string state = d.IsMoving ? (d.CurrentFrame == 1 ? "swim1" : "swim2") : "idle";
            graphics.DrawSprite($"d{d.PlayerNumber}_{state}", d.X, d.Y, 160, 180, d.FacingLeft);
        }
    }
}