namespace PearlDivers.Models
{
    public enum BonusType { Heal, Oxygen, Shield, Speed, Invincibility }

    public class Bonus : GameObject
    {
        public override float SpriteW { get; } = 50f;
        public override float SpriteH { get; } = 50f;

        public BonusType Type { get; }
        public float Duration { get; } = 10f; // секунд действия
        public bool IsActive { get; set; } = true;

        private float _timer = 0f;

        public override RectF Collider => new RectF(X, Y, SpriteW, SpriteH);
        public override string TextureName => $"bonus_{Type.ToString().ToLower()}";

        public Bonus(float x, float y, BonusType type)
        {
            X = x; Y = y; Type = type;
        }

        public override void Update(float dt)
        {
            _timer += dt;
            if (_timer >= Duration)
            {
                IsActive = false;
            }
        }
    }
}