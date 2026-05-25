namespace PearlDivers.Models.Decorators
{
    public class PoisonDecorator : DiverDecorator
    {
        private float _pulseTimer = 0f;
        private const float PulseInterval = 1.5f;
        private const float PulseDamage = 8f;

        public PoisonDecorator(IDiver diver) : base(diver) { }

        public override void Update(float dt)
        {
            _pulseTimer += dt;
            if (_pulseTimer >= PulseInterval)
            {
                _pulseTimer = 0f;
                if (_diver is Diver d) d.TakePoisonDamage(PulseDamage);
            }

            if (_diver.PoisonTimer <= 0f) IsExpired = true;
        }
    }
}