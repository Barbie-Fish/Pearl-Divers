using System;
using System.Collections.Generic;
using PearlDivers.Models;

namespace PearlDivers.Models
{
    public class Diver : IDiver
    {
        public const float SpriteW = 160f;
        public const float SpriteH = 180f;

        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; } = 300.0f;
        public bool IsMoving { get; private set; }
        public bool FacingLeft { get; private set; }
        public int PlayerNumber { get; private set; }
        public int CurrentFrame { get; private set; } = 1;
        public bool BlinkVisible { get; private set; } = true;

        public float HP { get; private set; } = 100f;
        public float MaxHP { get; } = 100f;
        public float Oxygen { get; private set; } = 100f;
        public float MaxOxygen { get; } = 100f;
        public int Score { get; private set; } = 0;

        public bool IsPoisoned => _poisonTimer > 0f;
        public bool IsShielded => _shieldTimer > 0f;
        public bool IsSpeedBoosted => _speedTimer > 0f;
        public float PoisonTimer => _poisonTimer;
        public float ShieldTimer => _shieldTimer;
        public float SpeedTimer => _speedTimer;

        private float _invincibilityTimer = 0f;
        private const float InvincibilityDuration = 1.5f;
        private float _blinkTimer = 0f;

        private float _poisonTimer = 0f;
        private const float PoisonDuration = 6f;
        private float _poisonFlashTimer = 0f;
        private const float PoisonFlashDuration = 0.15f;
        public bool IsPoisonFlashing => _poisonFlashTimer > 0f;

        private float _shieldTimer = 0f;
        private const float ShieldDuration = 5f;

        private float _speedTimer = 0f;
        private const float SpeedDuration = 5f;

        private readonly List<DiverDecorator> _decorators = new List<DiverDecorator>();

        private float _animTimer = 0f;
        private const float FrameDuration = 0.2f;
        private float _screenW = 1280f;
        private float _screenH = 720f;

        public RectF Collider
        {
            get
            {
                float w = IsMoving ? 80f : 50f;
                float h = IsMoving ? 50f : 90f;
                float offsetX = (SpriteW - w) / 2f;
                float offsetY = (SpriteH - h) / 2f;
                return new RectF(X + offsetX, Y + offsetY, w, h);
            }
        }

        public Diver(int playerNumber, float startX, float startY)
        {
            PlayerNumber = playerNumber;
            X = startX; Y = startY;
        }

        public void SetScreenBounds(float w, float h) { _screenW = w; _screenH = h; }
        public void AddDecorator(DiverDecorator decorator) { _decorators.Add(decorator); }

        public void Move(float dx, float dy, float dt)
        {
            foreach (var d in _decorators) d.BeforeMove(ref dx, ref dy);
            if (dx != 0 && dy != 0) { dx *= 0.707f; dy *= 0.707f; }
            X += dx * Speed * dt; Y += dy * Speed * dt;
            if (X < 0f) X = 0f; if (Y < 0f) Y = 0f;
            if (X + SpriteW > _screenW) X = _screenW - SpriteW;
            if (Y + SpriteH > _screenH) Y = _screenH - SpriteH;
            IsMoving = (dx != 0 || dy != 0);
            if (dx < 0) FacingLeft = true;
            else if (dx > 0) FacingLeft = false;
            UpdateAnimation(dt);
        }

        public void UpdateStatus(float dt, float surfaceY, float bottomY)
        {
            foreach (var d in _decorators) d.Update(dt);
            _decorators.RemoveAll(d => d.IsExpired);

            if (_invincibilityTimer > 0f)
            {
                _invincibilityTimer -= dt;
                _blinkTimer += dt;
                if (_blinkTimer >= 0.1f) { _blinkTimer -= 0.1f; BlinkVisible = !BlinkVisible; }
                if (_invincibilityTimer <= 0f) { _invincibilityTimer = 0f; BlinkVisible = true; }
            }

            if (_poisonTimer > 0f)
            {
                _poisonTimer -= dt;
                if (_poisonTimer <= 0f) _poisonTimer = 0f;
            }
            if (_shieldTimer > 0f) _shieldTimer -= dt;
            if (_speedTimer > 0f)
            {
                _speedTimer -= dt;
                if (_speedTimer <= 0f) Speed = 300f;
            }
            if (_poisonFlashTimer > 0f) _poisonFlashTimer -= dt;

            float centerY = Y + SpriteH / 2f;
            if (centerY <= surfaceY)
            {
                Oxygen += 20f * dt;
                if (Oxygen > MaxOxygen) Oxygen = MaxOxygen;
            }
            else
            {
                float depth = (centerY - surfaceY) / (bottomY - surfaceY);
                Oxygen -= depth * 8f * dt;
                if (Oxygen < 0f) { Oxygen = 0f; HP -= 5f * dt; if (HP < 0f) HP = 0f; }
            }
        }

        public void TakeDamage(float amount)
        {
            if (_invincibilityTimer > 0f) return;
            bool blocked = false;
            foreach (var d in _decorators) if (d.OnTakeDamage(ref amount)) blocked = true;

            if (!blocked)
            {
                HP -= amount; if (HP < 0f) HP = 0f;
                _invincibilityTimer = InvincibilityDuration;
                _blinkTimer = 0f; BlinkVisible = true;
            }
        }

        public void TakePoisonDamage(float amount)
        {
            if (IsShielded) return;
            HP -= amount; if (HP < 0f) HP = 0f;
            _poisonFlashTimer = PoisonFlashDuration;
        }

        public void ApplyPoison() { _poisonTimer = PoisonDuration; }
        public void CurePoison() { _poisonTimer = 0f; _poisonFlashTimer = 0f; }
        public void ActivateShield() { _shieldTimer = ShieldDuration; }
        public void ActivateSpeed() { Speed = 500f; _speedTimer = SpeedDuration; }
        public void Heal(float amount) { HP = Math.Min(HP + amount, MaxHP); }
        public void RefillOxygen(float amount) { Oxygen = Math.Min(Oxygen + amount, MaxOxygen); }
        public void AddScore(int points = 1) { Score += points; }

        private void UpdateAnimation(float dt)
        {
            if (!IsMoving) { _animTimer = 0f; return; }
            _animTimer += dt;
            if (_animTimer >= FrameDuration) { CurrentFrame = (CurrentFrame == 1) ? 2 : 1; _animTimer -= FrameDuration; }
        }
    }
}