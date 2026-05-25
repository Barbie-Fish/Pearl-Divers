using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.DirectWrite;
using D2D1 = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;
using System.Windows;
using SystemDrawing = System.Drawing;
using SystemDrawingImaging = System.Drawing.Imaging;
using PearlDivers.Models;

namespace PearlDivers.Engine
{
    public class GraphicsHandler : IDisposable
    {
        private WindowRenderTarget renderTarget;
        private D2D1.Factory factory2D;
        private Dictionary<string, D2D1.Bitmap> textures = new Dictionary<string, D2D1.Bitmap>();
        private Dictionary<string, D2D1.SolidColorBrush> _brushes = new Dictionary<string, D2D1.SolidColorBrush>();
        private Dictionary<string, float> _aspectRatios = new Dictionary<string, float>();
        private SharpDX.DirectWrite.Factory _dwriteFactory;
        private TextFormat _textFormatLeft;
        private TextFormat _textFormatRight;

        // Добавлены поля для хранения размеров экрана
        private int screenWidth;
        private int screenHeight;

        public void Initialize(IntPtr hwnd, int width, int height)
        {
            screenWidth = width;
            screenHeight = height;

            factory2D = new D2D1.Factory();
            var pixelFormat = new D2D1.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D1.AlphaMode.Premultiplied);
            var properties = new RenderTargetProperties(pixelFormat);
            var winProperties = new HwndRenderTargetProperties()
            {
                Hwnd = hwnd,
                PixelSize = new Size2(width, height),
                PresentOptions = PresentOptions.None
            };
            renderTarget = new WindowRenderTarget(factory2D, properties, winProperties);

            _dwriteFactory = new SharpDX.DirectWrite.Factory();
            _textFormatLeft = new TextFormat(_dwriteFactory, "Consolas", 11f) { TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading };
            _textFormatRight = new TextFormat(_dwriteFactory, "Consolas", 12f) { TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing };
        }

        public void LoadTexture(string name, string path)
        {
            var uri = new Uri($"pack://application:,,,/{path}", UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            using (var drawingBitmap = (SystemDrawing.Bitmap)SystemDrawing.Image.FromStream(info.Stream))
            {
                textures[name] = LoadBitmap(drawingBitmap);
                _aspectRatios[name] = (float)drawingBitmap.Width / drawingBitmap.Height;
            }
        }

        private D2D1.Bitmap LoadBitmap(SystemDrawing.Bitmap drawingBitmap)
        {
            var drawingLock = drawingBitmap.LockBits(
                new SystemDrawing.Rectangle(0, 0, drawingBitmap.Width, drawingBitmap.Height),
                SystemDrawingImaging.ImageLockMode.ReadOnly,
                SystemDrawingImaging.PixelFormat.Format32bppPArgb);
            try
            {
                var dataStream = new DataStream(drawingLock.Scan0, drawingLock.Stride * drawingLock.Height, true, false);
                var properties = new BitmapProperties(new D2D1.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D1.AlphaMode.Premultiplied));
                var bitmap = new D2D1.Bitmap(renderTarget, new Size2(drawingBitmap.Width, drawingBitmap.Height), dataStream, drawingLock.Stride, properties);
                dataStream.Dispose();
                return bitmap;
            }
            finally { drawingBitmap.UnlockBits(drawingLock); }
        }

        private D2D1.SolidColorBrush GetBrush(RawColor4 color, string key)
        {
            if (!_brushes.ContainsKey(key)) _brushes[key] = new D2D1.SolidColorBrush(renderTarget, color);
            return _brushes[key];
        }

        public void DrawRect(float x, float y, float w, float h, RawColor4 color, string colorKey)
        {
            var brush = GetBrush(color, colorKey);
            renderTarget.FillRectangle(new RawRectangleF(x, y, x + w, y + h), brush);
        }

        public void DrawRectOutline(float x, float y, float w, float h, RawColor4 color, string colorKey, float strokeWidth = 2f)
        {
            var brush = GetBrush(color, colorKey);
            renderTarget.DrawRectangle(new RawRectangleF(x, y, x + w, y + h), brush, strokeWidth);
        }

        public void BeginDraw() => renderTarget.BeginDraw();
        public void EndDraw() => renderTarget.EndDraw();
        public void Clear() => renderTarget.Clear(new RawColor4(0.39f, 0.58f, 0.93f, 1.0f));

        public void DrawSprite(string textureName, float x, float y, float targetWidth, float targetHeight, bool flipX)
        {
            DrawSprite(textureName, x, y, targetWidth, targetHeight, flipX, maintainAspectRatio: false);
        }

        public void DrawSprite(string textureName, float x, float y, float targetWidth, float targetHeight, bool flipX, bool maintainAspectRatio)
        {
            if (!textures.ContainsKey(textureName)) return;
            var bitmap = textures[textureName];
            float drawWidth = targetWidth;
            float drawHeight = targetHeight;
            if (maintainAspectRatio && _aspectRatios.ContainsKey(textureName))
            {
                float originalRatio = _aspectRatios[textureName];
                float targetRatio = targetWidth / targetHeight;
                if (originalRatio > targetRatio) { drawWidth = targetWidth; drawHeight = targetWidth / originalRatio; }
                else { drawHeight = targetHeight; drawWidth = targetHeight * originalRatio; }
            }
            var destRect = new RawRectangleF(x, y, x + drawWidth, y + drawHeight);
            renderTarget.Transform = flipX ? new RawMatrix3x2(-1f, 0f, 0f, 1f, 2f * (x + drawWidth / 2f), 0f) : new RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
            renderTarget.DrawBitmap(bitmap, destRect, 1.0f, BitmapInterpolationMode.Linear, null);
            renderTarget.Transform = new RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
        }

        public void DrawSpriteColored(string textureName, float x, float y, float targetWidth, float targetHeight, bool flipX, float tintR, float tintG, float tintB, float tintA, bool maintainAspectRatio = false)
        {
            if (!textures.ContainsKey(textureName)) return;
            var bitmap = textures[textureName];
            float drawWidth = targetWidth;
            float drawHeight = targetHeight;
            if (maintainAspectRatio && _aspectRatios.ContainsKey(textureName))
            {
                float originalRatio = _aspectRatios[textureName];
                float targetRatio = targetWidth / targetHeight;
                if (originalRatio > targetRatio) { drawWidth = targetWidth; drawHeight = targetWidth / originalRatio; }
                else { drawHeight = targetHeight; drawWidth = targetHeight * originalRatio; }
            }
            var destRect = new RawRectangleF(x, y, x + drawWidth, y + drawHeight);
            renderTarget.Transform = flipX ? new RawMatrix3x2(-1f, 0f, 0f, 1f, 2f * (x + drawWidth / 2f), 0f) : new RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
            renderTarget.DrawBitmap(bitmap, destRect, 1.0f, BitmapInterpolationMode.Linear, null);
            var oldMode = renderTarget.AntialiasMode;
            renderTarget.AntialiasMode = AntialiasMode.Aliased;
            var tintBrush = GetBrush(new RawColor4(tintR, tintG, tintB, tintA), "tint_overlay");
            renderTarget.FillOpacityMask(bitmap, tintBrush, OpacityMaskContent.Graphics, destRect, null);
            renderTarget.AntialiasMode = oldMode;
            renderTarget.Transform = new RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
        }

        public void DrawCircleProgress(float cx, float cy, float radius, float progress, RawColor4 color, string colorKey)
        {
            if (progress <= 0f) return;
            var brush = GetBrush(color, colorKey);
            using (var pathGeometry = new PathGeometry(factory2D))
            {
                using (var sink = pathGeometry.Open())
                {
                    sink.BeginFigure(new RawVector2(cx, cy), FigureBegin.Filled);
                    float startAngle = -90f;
                    float endAngle = startAngle + (progress * 360f);
                    sink.AddLine(new RawVector2(cx, cy - radius));
                    if (progress < 1f)
                    {
                        double rad = endAngle * Math.PI / 180.0;
                        float endX = cx + radius * (float)Math.Cos(rad);
                        float endY = cy + radius * (float)Math.Sin(rad);
                        var arc = new ArcSegment
                        {
                            Point = new RawVector2(endX, endY),
                            Size = new Size2F(radius, radius),
                            RotationAngle = 0,
                            SweepDirection = SweepDirection.Clockwise,
                            ArcSize = progress > 0.5f ? ArcSize.Large : ArcSize.Small
                        };
                        sink.AddArc(arc);
                    }
                    else
                    {
                        sink.AddArc(new ArcSegment
                        {
                            Point = new RawVector2(cx, cy - radius),
                            Size = new Size2F(radius, radius),
                            RotationAngle = 0,
                            SweepDirection = SweepDirection.Clockwise,
                            ArcSize = ArcSize.Large
                        });
                    }
                    sink.AddLine(new RawVector2(cx, cy));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }
                renderTarget.FillGeometry(pathGeometry, brush);
            }
        }

        public void DrawHUD(IDiver p1, IDiver p2)
        {
            float barW = 200f, barH = 16f;
            float margin = 20f;
            float p1X = margin;
            float p2X = screenWidth - margin - barW;
            float y = margin;

            DrawRect(p1X, y, barW, barH, new RawColor4(0.1f, 0.1f, 0.1f, 1f), "ui_bg");
            DrawRect(p1X, y, barW * (p1.HP / p1.MaxHP), barH, new RawColor4(0.9f, 0.15f, 0.15f, 1f), "ui_hp1");
            y += barH + 5;
            DrawRect(p1X, y, barW, barH, new RawColor4(0.1f, 0.1f, 0.1f, 1f), "ui_bg");
            DrawRect(p1X, y, barW * (p1.Oxygen / p1.MaxOxygen), barH, new RawColor4(0.15f, 0.45f, 0.9f, 1f), "ui_o21");
            y += barH + 10;
            renderTarget.DrawText($"P1 Score: {p1.Score}", _textFormatLeft, new RawRectangleF(p1X, y, p1X + 200f, y + 24f), GetBrush(new RawColor4(1f, 1f, 1f, 1f), "ui_txt"));
            y += 30f;
            DrawEffects(p1, p1X, ref y);

            y = margin;
            DrawRect(p2X, y, barW, barH, new RawColor4(0.1f, 0.1f, 0.1f, 1f), "ui_bg");
            float hp2W = barW * (p2.HP / p2.MaxHP);
            DrawRect(p2X + barW - hp2W, y, hp2W, barH, new RawColor4(0.9f, 0.15f, 0.15f, 1f), "ui_hp2");
            y += barH + 5;
            DrawRect(p2X, y, barW, barH, new RawColor4(0.1f, 0.1f, 0.1f, 1f), "ui_bg");
            float o22W = barW * (p2.Oxygen / p2.MaxOxygen);
            DrawRect(p2X + barW - o22W, y, o22W, barH, new RawColor4(0.15f, 0.45f, 0.9f, 1f), "ui_o22");
            y += barH + 10;
            renderTarget.DrawText($"P2 Score: {p2.Score}", _textFormatRight, new RawRectangleF(p2X, y, p2X + 200f, y + 24f), GetBrush(new RawColor4(1f, 1f, 1f, 1f), "ui_txt"));
            y += 30f;
            DrawEffects(p2, p2X + barW, ref y, true);
        }

        private void DrawEffects(IDiver d, float x, ref float y, bool rightAlign = false)
        {
            float iconSize = 22f;
            float rowH = iconSize + 8f;
            float progressRadius = iconSize / 2f;
            float labelW = 70f;
            float padX = 4f, padY = 3f;
            float rowTotalW = iconSize + 6f + labelW;

            if (d.IsShielded)
            {
                float rowX = rightAlign ? x - rowTotalW : x;
                float bgX = rowX - padX;
                float bgY = y - padY;
                DrawRect(bgX, bgY, rowTotalW + padX * 2f, rowH + padY * 2f, new RawColor4(0f, 0f, 0f, 0.55f), "ui_eff_bg");
                float cx = rowX + progressRadius;
                float cy = y + iconSize / 2f;
                DrawCircleProgress(cx, cy, progressRadius, d.ShieldTimer / 5f, new RawColor4(0.2f, 0.8f, 1.0f, 1f), "ui_shield");
                var rect = new RawRectangleF(rowX + iconSize + 6f, y + 1f, rowX + iconSize + 6f + labelW, y + iconSize);
                renderTarget.DrawText("Щит!", _textFormatLeft, rect, GetBrush(new RawColor4(0.2f, 0.8f, 1.0f, 1f), "ui_shield_txt"));
                y += rowH + 4f;
            }

            if (d.IsPoisoned)
            {
                float rowX = rightAlign ? x - rowTotalW : x;
                float bgX = rowX - padX;
                float bgY = y - padY;
                DrawRect(bgX, bgY, rowTotalW + padX * 2f, rowH + padY * 2f, new RawColor4(0f, 0f, 0f, 0.55f), "ui_eff_bg");
                float cx = rowX + progressRadius;
                float cy = y + iconSize / 2f;
                DrawCircleProgress(cx, cy, progressRadius, d.PoisonTimer / 6f, new RawColor4(0.2f, 1.0f, 0.2f, 1f), "ui_poison");
                var rect = new RawRectangleF(rowX + iconSize + 6f, y + 1f, rowX + iconSize + 6f + labelW, y + iconSize);
                renderTarget.DrawText("Отравление!", _textFormatLeft, rect, GetBrush(new RawColor4(0.2f, 1.0f, 0.2f, 1f), "ui_poison_txt"));
                y += rowH + 4f;
            }

            if (d.IsSpeedBoosted)
            {
                float rowX = rightAlign ? x - rowTotalW : x;
                float bgX = rowX - padX;
                float bgY = y - padY;
                DrawRect(bgX, bgY, rowTotalW + padX * 2f, rowH + padY * 2f, new RawColor4(0f, 0f, 0f, 0.55f), "ui_eff_bg");
                float cx = rowX + progressRadius;
                float cy = y + iconSize / 2f;
                DrawCircleProgress(cx, cy, progressRadius, d.SpeedTimer / 5f, new RawColor4(1.0f, 0.8f, 0.2f, 1f), "ui_speed");
                var rect = new RawRectangleF(rowX + iconSize + 6f, y + 1f, rowX + iconSize + 6f + labelW, y + iconSize);
                renderTarget.DrawText("Ускорение!", _textFormatLeft, rect, GetBrush(new RawColor4(1.0f, 0.8f, 0.2f, 1f), "ui_speed_txt"));
                y += rowH + 4f;
            }
        }

        // Методы для таймера и экрана победы
        public void DrawTimer(float timeLeft)
        {
            int minutes = (int)(timeLeft / 60f);
            int seconds = (int)(timeLeft % 60f);
            string text = $"{minutes:00}:{seconds:00}";
            DrawTextCentered(text, new RawColor4(1f, 1f, 1f, 1f), 48f, 20f);
        }

        public void DrawTextCentered(string text, RawColor4 color, float fontSize, float offsetY)
        {
            using (var format = new TextFormat(_dwriteFactory, "Consolas", fontSize) { TextAlignment = SharpDX.DirectWrite.TextAlignment.Center })
            {
                var brush = GetBrush(color, "centered_txt");
                var rect = new RawRectangleF(0, offsetY, screenWidth, offsetY + fontSize * 1.5f);
                renderTarget.DrawText(text, format, rect, brush);
            }
        }

        public void DrawGameOver(string winnerText)
        {
            DrawRect(0, 0, screenWidth, screenHeight, new RawColor4(0f, 0f, 0f, 0.7f), "overlay");
            DrawTextCentered(winnerText, new RawColor4(1f, 1f, 1f, 1f), 64f, screenHeight / 2f - 50f);
            DrawTextCentered("Нажмите ESC чтобы выйти", new RawColor4(0.8f, 0.8f, 0.8f, 1f), 32f, screenHeight / 2f + 20f);
        }

        public void Dispose()
        {
            foreach (var tex in textures.Values) tex.Dispose();
            foreach (var br in _brushes.Values) br.Dispose();
            _textFormatLeft?.Dispose();
            _textFormatRight?.Dispose();
            _dwriteFactory?.Dispose();
            renderTarget?.Dispose();
            factory2D?.Dispose();
        }
    }
}