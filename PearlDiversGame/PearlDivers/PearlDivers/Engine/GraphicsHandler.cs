using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;
using System.Windows;
using SystemDrawing = System.Drawing;
using SystemDrawingImaging = System.Drawing.Imaging;

namespace PearlDivers.Engine
{
    public class GraphicsHandler : IDisposable
    {
        private WindowRenderTarget renderTarget;
        private D2D1.Factory factory2D;
        private Dictionary<string, D2D1.Bitmap> textures = new Dictionary<string, D2D1.Bitmap>();
        private Dictionary<string, D2D1.SolidColorBrush> _brushes = new Dictionary<string, D2D1.SolidColorBrush>();

        // Кэш пропорций спрайтов для корректного масштабирования
        private Dictionary<string, float> _aspectRatios = new Dictionary<string, float>();

        public void Initialize(IntPtr hwnd, int width, int height)
        {
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
        }

        public void LoadTexture(string name, string path)
        {
            var uri = new Uri($"pack://application:,,,/{path}", UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            using (var drawingBitmap = (SystemDrawing.Bitmap)SystemDrawing.Image.FromStream(info.Stream))
            {
                textures[name] = LoadBitmap(drawingBitmap);
                // Сохраняем пропорцию для корректного масштабирования
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
            finally
            {
                drawingBitmap.UnlockBits(drawingLock);
            }
        }

        private D2D1.SolidColorBrush GetBrush(RawColor4 color, string key)
        {
            if (!_brushes.ContainsKey(key))
                _brushes[key] = new D2D1.SolidColorBrush(renderTarget, color);
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

        // НОВАЯ МЕТОДИКА: отрисовка с сохранением пропорций ("зум" вместо "стретч")
        public void DrawSprite(string textureName, float x, float y, float targetWidth, float targetHeight, bool flipX)
        {
            DrawSprite(textureName, x, y, targetWidth, targetHeight, flipX, maintainAspectRatio: false);
        }

        public void DrawSprite(string textureName, float x, float y, float targetWidth, float targetHeight, bool flipX, bool maintainAspectRatio)
        {
            if (!textures.ContainsKey(textureName)) return;

            var bitmap = textures[textureName];

            // Если нужно сохранить пропорции — вычисляем корректный размер
            float drawWidth = targetWidth;
            float drawHeight = targetHeight;

            if (maintainAspectRatio && _aspectRatios.ContainsKey(textureName))
            {
                float originalRatio = _aspectRatios[textureName];
                float targetRatio = targetWidth / targetHeight;

                if (originalRatio > targetRatio)
                {
                    // Спрайт шире — масштабируем по ширине
                    drawWidth = targetWidth;
                    drawHeight = targetWidth / originalRatio;
                }
                else
                {
                    // Спрайт уже — масштабируем по высоте
                    drawHeight = targetHeight;
                    drawWidth = targetHeight * originalRatio;
                }
            }

            var destRect = new RawRectangleF(x, y, x + drawWidth, y + drawHeight);

            if (flipX)
            {
                float cx = x + drawWidth / 2f;
                var flip = new RawMatrix3x2(-1f, 0f, 0f, 1f, 2f * cx, 0f);
                renderTarget.Transform = flip;
            }
            else
            {
                renderTarget.Transform = new RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
            }

            renderTarget.DrawBitmap(bitmap, destRect, 1.0f, BitmapInterpolationMode.Linear, null);
            renderTarget.Transform = new RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
        }

        public void DrawUI() { }

        public void Dispose()
        {
            foreach (var tex in textures.Values) tex.Dispose();
            foreach (var br in _brushes.Values) br.Dispose();
            renderTarget?.Dispose();
            factory2D?.Dispose();
        }
    }
}