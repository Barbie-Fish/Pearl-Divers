namespace PearlDivers.Models
{
    public enum ShellfishType { Normal, Poisonous }
    public enum ShellState { Closed, Open }

    public class Shellfish : GameObject
    {
        // Размеры ячейки и спрайта
        public const float CellWidth = 80f;
        public const float CellHeight = 60f;
        public override float SpriteW { get; } = 80f;
        public override float SpriteH { get; } = 80f;

        public ShellfishType Type { get; }
        public ShellState State { get; private set; } = ShellState.Closed;

        private float _stateTimer = 0f;
        private float _stateDuration = 3f;
        private static readonly System.Random _random = new System.Random();

        // Коллайдер центрирован в ячейке
        public override RectF Collider => new RectF(
            X + (CellWidth - SpriteW) / 2f,
            Y + (CellHeight - SpriteH) / 2f,
            SpriteW, SpriteH);

        public override string TextureName => GetSpriteName();

        public Shellfish(float x, float y, ShellfishType type)
        {
            X = x; Y = y; Type = type;
            _stateTimer = _random.NextFloat(0, _stateDuration);
        }

        public override void Update(float dt)
        {
            _stateTimer += dt;

            if (_stateTimer >= _stateDuration)
            {
                _stateTimer = 0f;
                _stateDuration = _random.NextFloat(2f, 6f);

                if (_random.NextDouble() < 0.3)
                {
                    State = (State == ShellState.Closed) ? ShellState.Open : ShellState.Closed;
                }
            }
        }

        public string GetSpriteName()
        {
            if (State == ShellState.Closed)
                return "shell_closed";
            else
                return Type == ShellfishType.Poisonous ? "shell_poison" : "shell_pearl";
        }
    }

    // Расширение для Random
    public static class RandomExtensions
    {
        public static float NextFloat(this System.Random random, float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }
    }
}