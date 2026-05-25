namespace PearlDivers.Models
{
    public abstract class DiverDecorator
    {
        protected IDiver _diver;
        public bool IsExpired { get; protected set; }

        public DiverDecorator(IDiver diver)
        {
            _diver = diver;
            IsExpired = false;
        }

        public virtual void Update(float dt) { }
        public virtual void BeforeMove(ref float dx, ref float dy) { }
        public virtual bool OnTakeDamage(ref float amount) { return false; }
    }
}