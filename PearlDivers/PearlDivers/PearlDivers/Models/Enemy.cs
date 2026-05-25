namespace PearlDivers.Models
{
    public enum EnemyType { Fish, Snake }

    public class Enemy : GameObject
    {
        public static readonly float DefaultSpriteW = 100f;
        public static readonly float DefaultSpriteH = 80f;

        public override float SpriteW { get; } = DefaultSpriteW;
        public override float SpriteH { get; } = DefaultSpriteH;

        public EnemyType Type { get; }
        public bool FacingLeft { get; set; }
        public string SpriteName { get; }
        public float MinY { get; }
        public float MaxY { get; }

        public float CenterOffsetX { get; set; } = 8f;
        public float CenterOffsetY { get; set; } = 6f;
        public float WidthShrink { get; set; } = 16f;
        public float HeightShrink { get; set; } = 12f;

        private float _speed;
        private static readonly System.Random _random = new System.Random();

        public override RectF Collider => new RectF(
            X + CenterOffsetX,
            Y + CenterOffsetY,
            SpriteW - WidthShrink,
            SpriteH - HeightShrink);

        public override string TextureName => SpriteName;

        public Enemy(float x, float y, EnemyType type, float speed, string spriteName, float minY, float maxY)
        {
            X = x; Y = y; Type = type;
            _speed = speed;
            SpriteName = spriteName;
            MinY = minY;
            MaxY = maxY;
        }

        public override void Update(float dt)
        {
            X += FacingLeft ? -_speed * dt : _speed * dt;

            if (_random.NextDouble() < 0.02)
            {
                Y += (_random.Next(0, 2) == 0 ? -1 : 1) * 0.5f;
                if (Y < MinY) Y = MinY;
                if (Y + SpriteH > MaxY) Y = MaxY - SpriteH;
            }
        }
    }
}