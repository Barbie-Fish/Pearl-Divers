namespace PearlDivers.Models
{
    public enum EnemyType { Fish, Snake }

    public class Enemy : GameObject
    {
        // === СТАТИЧЕСКИЕ КОНСТАНТЫ для расчётов без экземпляра ===
        public static readonly float DefaultSpriteW = 100f;
        public static readonly float DefaultSpriteH = 80f;

        // === Переопределение для базового класса (экземпляр) ===
        public override float SpriteW { get; } = DefaultSpriteW;
        public override float SpriteH { get; } = DefaultSpriteH;

        public EnemyType Type { get; }
        public bool FacingLeft { get; set; } // теперь это направление движения, а не "смотрит"
        public string SpriteName { get; }
        public float MinY { get; }
        public float MaxY { get; }

        private float _speed;
        private static readonly System.Random _random = new System.Random();

        // Коллайдер с небольшими отступами
        public override RectF Collider => new RectF(
            X + 8f,
            Y + 6f,
            SpriteW - 16f,
            SpriteH - 12f);

        public override string TextureName => SpriteName;

        public Enemy(float x, float y, EnemyType type, float speed, string spriteName, float minY, float maxY)
        {
            X = x; Y = y; Type = type;
            _speed = speed;
            SpriteName = spriteName;
            MinY = minY;
            MaxY = maxY;
            // FacingLeft задаётся снаружи при создании
        }

        public override void Update(float dt)
        {
            // Движение строго в одном направлении (без разворотов!)
            X += FacingLeft ? -_speed * dt : _speed * dt;

            // Лёгкое дрейфование по вертикали в пределах зоны (для естественности)
            if (_random.NextDouble() < 0.02)
            {
                Y += (_random.Next(0, 2) == 0 ? -1 : 1) * 0.5f;
                if (Y < MinY) Y = MinY;
                if (Y + SpriteH > MaxY) Y = MaxY - SpriteH;
            }
        }
    }
}