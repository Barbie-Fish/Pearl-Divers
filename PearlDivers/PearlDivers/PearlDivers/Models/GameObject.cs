using SharpDX.Mathematics.Interop;

namespace PearlDivers.Models
{
    public abstract class GameObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public virtual float SpriteW { get; } = 80f;
        public virtual float SpriteH { get; } = 80f;

        public abstract RectF Collider { get; }

        public abstract string TextureName { get; }

        public virtual bool IsOffScreen(float screenWidth, float screenHeight, float buffer = 100f)
        {
            return X + SpriteW < -buffer || X > screenWidth + buffer ||
                   Y + SpriteH < -buffer || Y > screenHeight + buffer;
        }

        public virtual void Update(float dt) { }
    }
}