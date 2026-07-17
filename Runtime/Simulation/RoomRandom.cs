using System;

namespace VirtualRoom.Audience
{
    internal sealed class RoomRandom
    {
        private readonly Random _random;

        public RoomRandom(int seed)
        {
            _random = seed == 0 ? new Random() : new Random(seed);
        }

        public float Next() => (float)_random.NextDouble();

        public float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        public int RangeInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public bool Chance(float probability) => _random.NextDouble() < probability;

        public float Bell(float center = 0.5f)
        {
            float v = (Next() + Next() + Next()) / 3f;
            v += center - 0.5f;
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }
    }

    internal static class M
    {
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        public static float Damp(float current, float target, float rate, float dt)
        {
            return Lerp(current, target, 1f - (float)Math.Exp(-rate * dt));
        }
    }
}
