namespace PearlDivers.Models.Decorators
{
    public class ShieldDecorator : DiverDecorator
    {
        public ShieldDecorator(IDiver diver) : base(diver) { }

        public override void Update(float dt)
        {
            if (!_diver.IsShielded) IsExpired = true;
        }

        public override bool OnTakeDamage(ref float amount)
        {
            if (!_diver.IsShielded) return false;
            return true;
        }
    }
}