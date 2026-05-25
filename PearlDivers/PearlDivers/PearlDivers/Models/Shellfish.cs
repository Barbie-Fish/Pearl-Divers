namespace PearlDivers.Models
{
    public enum ShellfishType { Normal, Poisonous }
    public enum ShellState { Closed, Open }

    public class Shellfish : GameObject
    {
        public const float CellWidth = 56f;
        public const float CellHeight = 44f;
        public override float SpriteW { get; } = 56f;
        public override float SpriteH { get; } = 56f;
        public ShellfishType Type { get; private set; }
        public ShellState State { get; private set; } = ShellState.Closed;
        private float _stateTimer = 0f;
        private float _stateDuration = 3f;
        private float _openTimer = 0f;
        private const float OPEN_TIMEOUT = 4f;
        private static readonly System.Random _random = new System.Random();
        public bool IsRemoved { get; private set; } = false;
        public bool JustCollected { get; private set; } = false;

        public override RectF Collider => new RectF(
            X + (CellWidth - SpriteW) / 2f + 5f,
            Y + (CellHeight - SpriteH) / 2f + 15f,
            SpriteW - 20f, SpriteH - 20f);

        public override string TextureName => State == ShellState.Closed ? "shell_closed" : (Type == ShellfishType.Poisonous ? "shell_poison" : "shell_pearl");

        public Shellfish(float x, float y, ShellfishType type)
        {
            X = x; Y = y; Type = type;
            _stateTimer = _random.NextFloat(0, _stateDuration);
        }

        public override void Update(float dt)
        {
            if (JustCollected) JustCollected = false;

            if (State == ShellState.Open)
            {
                _openTimer += dt;
                if (_openTimer >= OPEN_TIMEOUT)
                {
                    IsRemoved = true;
                    return;
                }
            }

            _stateTimer += dt;
            if (_stateTimer >= _stateDuration)
            {
                _stateTimer = 0f;
                _stateDuration = _random.NextFloat(2f, 3f);
                if (_random.NextDouble() < 0.75)
                {
                    if (State == ShellState.Closed)
                    {
                        State = ShellState.Open;
                        _openTimer = 0f;
                    }
                    else
                    {
                        State = ShellState.Closed;
                    }
                }
            }
        }

        public void Collect()
        {
            IsRemoved = true;
            JustCollected = true;
        }

        public void Respawn(float x, float y)
        {
            X = x; Y = y;
            Type = (_random.NextDouble() < 0.35) ? ShellfishType.Poisonous : ShellfishType.Normal;
            State = ShellState.Closed;
            IsRemoved = false;
            JustCollected = false;
            _stateTimer = _random.NextFloat(0f, 2f);
            _stateDuration = _random.NextFloat(2f, 5f);
            _openTimer = 0f;
        }
    }

    public static class RandomExtensions
    {
        public static float NextFloat(this System.Random random, float min, float max) => (float)(random.NextDouble() * (max - min) + min);
    }
}