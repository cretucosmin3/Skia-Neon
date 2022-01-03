using System;

namespace SkiaNeon
{
    public static class Number
    {
        private static readonly Random rnd = new ();

        public static int Random(int min, int max)
        {
            return rnd.Next(min, max);
        }

        public static float Random(float min, float max)
        {
            int mn = (int)(min * 100);
            int mx = (int)(max * 100);

            return rnd.Next(mn, mx) / 100f;
        }

        public static bool Chance(int chance)
        {
            return Random(0, 100) < chance;
        }
    }
}
