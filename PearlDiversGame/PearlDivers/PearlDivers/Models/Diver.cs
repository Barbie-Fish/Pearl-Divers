namespace PearlDivers.Models
{
    public class Diver
    {
        // Размер спрайта
        public const float SpriteW = 160f;
        public const float SpriteH = 180f;

        // Коллайдер — уменьшен со всех сторон относительно спрайта
        private const float ColliderInsetX = 30f;
        private const float ColliderInsetY = 20f;
        public const float ColliderW = SpriteW - ColliderInsetX * 2f; // 100
        public const float ColliderH = SpriteH - ColliderInsetY * 2f; // 140

        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; } = 300.0f; // pixels per second
        public bool IsMoving { get; private set; }
        public bool FacingLeft { get; private set; }
        public int PlayerNumber { get; private set; }
        public int CurrentFrame { get; private set; } = 1;

        private float _animTimer = 0f;
        private const float FrameDuration = 0.2f;

        // Границы экрана — задаются снаружи
        private float _screenW = 1280f;
        private float _screenH = 720f;

        // Коллайдер в мировых координатах
        public RectF Collider => new RectF(
            X + ColliderInsetX,
            Y + ColliderInsetY,
            ColliderW,
            ColliderH);

        public Diver(int playerNumber, float startX, float startY)
        {
            PlayerNumber = playerNumber;
            X = startX;
            Y = startY;
        }

        public void SetScreenBounds(float w, float h)
        {
            _screenW = w;
            _screenH = h;
        }

        public void Move(float dx, float dy, float dt)
        {
            if (dx != 0 && dy != 0)
            {
                dx *= 0.707f;
                dy *= 0.707f;
            }

            X += dx * Speed * dt;
            Y += dy * Speed * dt;

            // Клamp по границам экрана (по спрайту)
            if (X < 0f) X = 0f;
            if (Y < 0f) Y = 0f;
            if (X + SpriteW > _screenW) X = _screenW - SpriteW;
            if (Y + SpriteH > _screenH) Y = _screenH - SpriteH;

            IsMoving = (dx != 0 || dy != 0);
            if (dx < 0) FacingLeft = true;
            else if (dx > 0) FacingLeft = false;

            UpdateAnimation(dt);
        }

        private void UpdateAnimation(float dt)
        {
            if (!IsMoving) { _animTimer = 0f; return; }

            _animTimer += dt;
            if (_animTimer >= FrameDuration)
            {
                CurrentFrame = (CurrentFrame == 1) ? 2 : 1;
                _animTimer -= FrameDuration;
            }
        }
    }
}