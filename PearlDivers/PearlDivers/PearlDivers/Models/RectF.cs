namespace PearlDivers.Models
{
    public struct RectF
    {
        public float X, Y, Width, Height;

        public RectF(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public float Right => X + Width;
        public float Bottom => Y + Height;

        public bool Intersects(RectF other) =>
            X < other.Right && Right > other.X &&
            Y < other.Bottom && Bottom > other.Y;
    }
}