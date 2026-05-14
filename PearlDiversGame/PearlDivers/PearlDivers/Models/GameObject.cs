using SharpDX.Mathematics.Interop;

namespace PearlDivers.Models
{
    // Базовый класс для всех игровых объектов — устраняет дублирование параметров
    public abstract class GameObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public virtual float SpriteW { get; } = 80f;
        public virtual float SpriteH { get; } = 80f;

        // Абстрактный коллайдер — каждый наследник реализует свою логику
        public abstract RectF Collider { get; }

        // Имя текстуры для отрисовки
        public abstract string TextureName { get; }

        // Проверка: объект полностью за пределами экрана?
        public virtual bool IsOffScreen(float screenWidth, float screenHeight, float buffer = 100f)
        {
            return X + SpriteW < -buffer || X > screenWidth + buffer ||
                   Y + SpriteH < -buffer || Y > screenHeight + buffer;
        }

        // Обновление состояния (по умолчанию пустое)
        public virtual void Update(float dt) { }
    }
}