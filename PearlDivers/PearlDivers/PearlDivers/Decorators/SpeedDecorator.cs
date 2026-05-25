namespace PearlDivers.Models.Decorators
{
    public class SpeedDecorator : DiverDecorator
    {
        public SpeedDecorator(IDiver diver) : base(diver) { }

        public override void Update(float dt)
        {
            if (_diver.SpeedTimer <= 0f) IsExpired = true;
        }

        public override void BeforeMove(ref float dx, ref float dy)
        {
            dx *= 1.1f;
            dy *= 1.1f;
        }
    }
}