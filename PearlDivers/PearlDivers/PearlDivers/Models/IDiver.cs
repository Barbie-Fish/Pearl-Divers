using SharpDX.Mathematics.Interop;

namespace PearlDivers.Models
{
    public interface IDiver
    {
        float X { get; set; }
        float Y { get; set; }
        float Speed { get; set; }
        bool IsMoving { get; }
        bool FacingLeft { get; }
        int PlayerNumber { get; }
        int CurrentFrame { get; }
        bool BlinkVisible { get; }

        float HP { get; }
        float MaxHP { get; }
        float Oxygen { get; }
        float MaxOxygen { get; }
        int Score { get; }

        bool IsPoisoned { get; }
        bool IsShielded { get; }
        bool IsSpeedBoosted { get; }
        float PoisonTimer { get; }
        float ShieldTimer { get; }
        float SpeedTimer { get; }

        RectF Collider { get; }

        void Move(float dx, float dy, float dt);
        void UpdateStatus(float dt, float surfaceY, float bottomY);
        void TakeDamage(float amount);
        void ApplyPoison();
        void ActivateShield();
        void ActivateSpeed();
        void AddScore(int points);
        void SetScreenBounds(float w, float h);
        void AddDecorator(DiverDecorator decorator);
    }
}