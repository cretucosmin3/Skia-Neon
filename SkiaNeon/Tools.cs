using SkiaSharp;
using System;

namespace SkiaNeon
{
    public static class Tools
    {
        public static SKPoint MovePointTowards(SKPoint a, SKPoint b, float distance)
        {
            SKPoint vector = new SKPoint(b.X - a.X, b.Y - a.Y);
            float length = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            var unitVector = new SKPoint(vector.X / length, vector.Y / length);
            return new SKPoint(a.X + unitVector.X * distance, a.Y + unitVector.Y * distance);
        }

        public static float smoothLerp(float from, float to, float progress)
        {
            if (progress > 1f) progress = 1f;
            return from + (to - from) * (progress * progress * (3 - 2 * progress));
        }

        public static SKPoint RandomWindowPoint()
        {
            Random randomizer = new Random();
            var xx = randomizer.Next(0, Program.window.Size.X);
            var yy = randomizer.Next(0, Program.window.Size.Y);

            return new SKPoint(xx, yy);
        }

        public static SKColor LerpColor(SKColor a, SKColor b, float f)
        {
            if (f > 1f) f = 1f;

            return new SKColor(
                (byte)smoothLerp(a.Red, b.Red, f),
                (byte)smoothLerp(a.Green, b.Green, f),
                (byte)smoothLerp(a.Blue, b.Blue, f)
            );
        }

        public static float Lerp(float a, float b, float f)
        {
            if (f > 1f) f = 1f;
            return a + f * (b - a);
        }
    }
}
